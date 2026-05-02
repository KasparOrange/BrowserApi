# Const-equivalent CSS-in-C# performance — measure first, then optimize

**Parent:** [browser-api/css-in-csharp.md §2](browser-api/css-in-csharp.md)

Spec §2 says: *"Class name references in Razor (`@Card`) are pre-computed string reads — equivalent to a `const`."* Today they're not. There's a small but non-zero hot path on every Razor `class` attribute render. This plan describes how to **measure each cost first**, **try each optimization in isolation**, and **prove the delta is real** before merging. The goal is not to chase nanoseconds for their own sake — it's to close the spec-promise gap with confidence, and to document concrete numbers for the next person who asks.

---

## 0. Definitions

**"Const-equivalent"** for a property access ≈ what the JIT generates for `string s = "mw-card";`:
- One IL instruction (`ldstr`).
- Roughly 1–2 ns wall-clock after warm-up.
- Zero heap allocation.

**"In budget"** is whatever we decide is good enough — not necessarily const-equivalent. The targets in §3 are starting points; the measurements decide if they're achievable.

**Hot paths we care about:**
1. `class="@DnD.Card"` — single `Class` → `string` per attribute per render.
2. `class="@(Btn + Active)"` — `Class + Class` → `ClassList` → `string`, two-class case.
3. `class="@(Btn + Btn.Variant(slug))"` — same as above plus a `.Variant(runtimeSlug)` allocation.
4. `class="@AppStyles.Card.Selector.Css"` — `Class.Selector.Css` for drag-controller selector params.

---

## 1. Current cost map (theory)

What each call site does today, derived from reading the code:

| Call site | Steps |
|---|---|
| `string s = MyStyles.Card;` | static field load → implicit operator (static method) → null check → `string.IsNullOrEmpty(c.Name)` length check → `CssRegistry.EnsureScanned()` (volatile bool read on hot path) → property getter on `Class.Name` → return |
| `string s = "mw-card";` | `ldstr` |
| `var list = A + B;` (both `Class`) | `Class + Class` operator → `new ClassList()` (struct) → `Add(a)` (slot store + counter increment) → `Add(b)` (same) → returns struct |
| `(string)(A + B)` | above + `ClassList.ToString()` → `new StringBuilder(...)` (heap) → 4 nullable slot appends → final `sb.ToString()` (heap) |
| `Card.Variant("active")` | null/empty check on `Name` → `EnsureScanned` (cached) → `new Class { Name = $"{Name}--active" }` (heap object + interpolated string) |
| `Card.Selector.Css` | `Selector` getter (null/empty check, `EnsureScanned`, allocates `new Selector($".{Name}")`) → field read |

Compared to const literal: each of the above is more work. **How much more, in actual nanoseconds and bytes, is the question this plan answers.**

---

## 2. Benchmark infrastructure

### 2.1 Project location

Add a new BenchmarkDotNet project at `tests/BrowserApi.Benchmarks/`. Standalone, not part of `dotnet test` (BenchmarkDotNet hijacks the process and runs subprocesses). Add it to the existing solution but flag with `<IsPackable>false</IsPackable>`.

### 2.2 Why BenchmarkDotNet specifically

- Standard for .NET microbenchmarks; well-understood by .NET reviewers.
- `[MemoryDiagnoser]` gives byte-accurate alloc counts, not just GC pressure proxies.
- `[BenchmarkCategory]` lets us group baselines and candidates next to each other in the output.
- Built-in baseline-vs-candidate ratio reporting — exactly the A/B view we want.

### 2.3 csproj sketch

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
    <ServerGarbageCollection>true</ServerGarbageCollection>
    <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
    <TieredCompilation>true</TieredCompilation>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.*" />
    <ProjectReference Include="..\..\src\BrowserApi\BrowserApi.csproj" />
    <ProjectReference Include="..\..\src\BrowserApi.Css.SourceGen\BrowserApi.Css.SourceGen.csproj"
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

ServerGC + Tiered Compilation reflect the typical Blazor Server runtime so numbers translate to the real workload.

### 2.4 Run protocol

```bash
dotnet run -c Release --project tests/BrowserApi.Benchmarks -- --filter "*"
```

- **Always Release mode.** Debug runs are useless — JIT does no inlining.
- **Run on a quiet machine.** Close every other app. Plug into mains; battery thermal throttling distorts numbers.
- **Do warm-up runs** before recording. BenchmarkDotNet does this automatically; don't disable.
- **Record platform and runtime** in the commit message: macOS 25.2 / Apple Silicon / .NET 10.0.x. Numbers from x86 hardware will differ in absolute terms (volatile reads cost more on weakly-ordered ARM).

### 2.5 Output format

Each benchmark run produces a markdown table. Append the table verbatim to a `benchmarks/` subdirectory in this plan, named `YYYY-MM-DD-{topic}.md`. We end up with a chronological log of "before A," "after A," "before B," etc. that anyone can scan to see what each optimization actually delivered. **Don't paraphrase the numbers. Paste the raw table.**

---

## 3. Baseline measurement: what does each cost today?

Before any optimization, we need numbers for the current implementation. One benchmark per cost in the §1 map:

### 3.1 Single-class implicit conversion

```csharp
[MemoryDiagnoser]
public class ClassToStringBenchmarks {
    private static readonly string Literal = "mw-card";
    private static readonly Class FromAuthoring = new() { Name = "mw-card" };

    [Benchmark(Baseline = true)]
    public string ConstLiteral() => Literal;

    [Benchmark]
    public string ClassImplicit() => FromAuthoring;  // calls implicit op
}
```

**Measures:** the per-attribute cost we ship today for `class="@MyStyles.Card"`.
**Targets:**
- Allocation: 0 B (it should already be — no string is allocated, the `Name` field is reused).
- Time: within 2× the const literal. If we're at 5 ns and const is 1 ns, that's 5×; if we're at 8 ns we have a problem.

### 3.2 ClassList composition (no string materialization yet)

```csharp
private static readonly Class A = new() { Name = "a" };
private static readonly Class B = new() { Name = "b" };

[Benchmark(Baseline = true)]
public string ConstConcat() => "a b";

[Benchmark]
public string TwoClassPlus() => A + B;
```

The `+` operator returns `ClassList`, which then implicit-converts to `string`. So this benchmark covers the StringBuilder allocation in `ClassList.ToString()`.

**Measures:** typical `class="@(Card + Active)"` markup.
**Targets:**
- Time: probably 5–10× the literal — we're paying for a StringBuilder.
- Allocation: BenchmarkDotNet reports the byte count. Anything > 0 B is the StringBuilder + final string allocation. The literal is 0 B (it's interned).

### 3.3 Variant() with runtime slug

```csharp
private string[] _slugs = ["todo", "progress", "done", "blocked", "review"];
private int _idx;

[Benchmark(Baseline = true)]
public string ConstSwitch() => _slugs[_idx++ % _slugs.Length] switch {
    "todo" => "kh kh--todo",
    "progress" => "kh kh--progress",
    "done" => "kh kh--done",
    "blocked" => "kh kh--blocked",
    _ => "kh kh--review",
};

[Benchmark]
public string Variant() {
    var slug = _slugs[_idx++ % _slugs.Length];
    return KanbanHeader + KanbanHeader.Variant(slug);
}
```

**Measures:** the realistic kanban-header path — the one the user just saw in MitWare.
**Targets:**
- Time: probably the worst of the three, since `.Variant()` allocates a new `Class`.
- Allocation: the `new Class` (~32 B) + the interpolated `$"{Name}--{slug}"` string (variable, depends on lengths). Const switch is 0 B (interned).

### 3.4 EnsureScanned overhead in isolation

```csharp
[Benchmark]
public bool EnsureScannedHotPath() {
    CssRegistry.EnsureScanned();
    return true;
}
```

**Measures:** the worst case where we keep the lazy-scan check but the scan has already completed. Lower bound on what the volatile-read costs us per access.

### 3.5 Selector.Css (the drag-controller path)

```csharp
[Benchmark(Baseline = true)]
public string LiteralSelector() => ".mw-card";

[Benchmark]
public string ClassDotSelector() => Btn.Selector.Css;
```

**Measures:** the path in `DnDTestPage.razor` where `Sources="@DnD.ListItem.Selector.Css"` runs once per drag-controller render.

### 3.6 Realistic Blazor render

A separate benchmark that actually walks a `RenderTreeBuilder` for a realistic component (e.g. the kanban column from `DnDTestPage.razor`) and measures end-to-end. This is what we ultimately care about. The microbenchmarks above isolate individual costs; this one tells us whether a 5-ns saving on `Class → string` is even visible against the rest of the render.

```csharp
[Benchmark]
public void RenderKanbanColumn() {
    using var builder = new RenderTreeBuilder();
    KanbanColumnComponent.RenderStatic(builder, columnData);  // helper that bypasses DI
}
```

If a microbenchmark improvement of 5 ns shows up as 0% on this benchmark, we know the optimization isn't worth the complexity. If it shows up as 1–2%, it's marginal but real.

---

## 4. Optimization candidates — one A/B at a time

Each candidate gets its own branch, its own PR, its own benchmark run. **No bundling.** The discipline is: change one thing, measure, decide.

### 4.1 [A] Skip `EnsureScanned` on the hot path

**Change:**
```csharp
public static implicit operator string(Class c) {
    if (c is null) return string.Empty;
    return c.Name;       // was: if (string.IsNullOrEmpty(c.Name)) CssRegistry.EnsureScanned(); return c.Name;
}
```

**Hypothesis:** The source generator's `[ModuleInitializer]` populates `Name` for every compile-time-visible `Class` field before user code runs. Removing the `IsNullOrEmpty + EnsureScanned` saves a string-length check + a volatile bool read.

**Risk:** Types loaded *after* module init (test fixtures, dynamically-loaded plug-in assemblies) bypass the source generator. Their `Class` fields show empty `Name`. Need a different fallback path — probably a one-time `Module.Init()`-from-the-runtime-side that does the AppDomain scan if `_scanned` is still false at first runtime access. The lazy-scan call moves from "every single attribute access" to "first access ever."

**Expected delta:** ~2–5 ns per access. Allocation unchanged (still 0 B).

**Decision criteria:** Merge if (a) the microbenchmark shows ≥10% improvement AND (b) the realistic Blazor render benchmark shows any non-noise improvement AND (c) the test suite still passes (lazy fallback intact for dynamic types).

### 4.2 [B] `[MethodImpl(AggressiveInlining)]` on the implicit operator

**Change:** add `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to the `Class → string` operator (and potentially `ClassList.ToString`).

**Hypothesis:** The implicit conversion is short enough that the JIT *should* inline it, but the `EnsureScanned` call may be inhibiting that. If we already did [A], this might pile on a marginal gain by guaranteeing the JIT folds the call into the caller.

**Risk:** Aggressive inlining bloats code size at every call site. For something used in every Razor render this is what we want, but worth verifying the binary doesn't grow disproportionately.

**Expected delta:** ~1 ns per access if [A] hasn't already enabled inlining.

**Decision criteria:** Same shape as [A]. Skip if [A] already produces inlined code (check disassembly via `BenchmarkDotNet.Disassembler` or `dotnet-jit-codegen` for the relevant method).

### 4.3 [C] Convert `Class` to a `readonly struct` carrying just a string

**Change:** `Class` becomes a `readonly struct`. The reference identity (used by the registry's `_rendered` `ConcurrentDictionary<Type, string>`) is replaced by the field type (`Type`) — already keyed by stylesheet type, not by individual `Class` instances, so this should still work.

**Risk:** Big surface change. `Class` currently extends `Declarations` — that inheritance breaks. Need to either:
- Pull the `Declarations` populating logic up to a `static readonly Class Card = new("card") { ... };` syntax with a constructor and an indexer-only collection initializer, OR
- Keep `Class` as a class but make a thin struct wrapper for the markup-side accessor (`ClassRef`?) — adds API noise.

This is the most invasive option and probably not worth it unless [A]+[B] still leave us far from const-equivalent.

**Expected delta:** removes one indirection (the `Class` reference dereference), saves the `.Variant()` heap allocation. Time ~1–2 ns more, allocation drops to 0 for `.Variant()` if we can also rewrite that path.

**Decision criteria:** Land this only if (a) the realistic Blazor render benchmark still shows headroom after [A]+[B] AND (b) the API surface change can be made non-breaking via implicit conversions and overloaded operators. If it breaks user code, reject — the absolute time delta is too small to be worth it.

### 4.4 [D] Bypass the `.Variant()` allocation via direct string concat

**Change:** add an overload `Class.PlusVariant(string slug) → ClassList` that produces the composed list directly without allocating an intermediate `Class`. Or have the analyzer rewrite `A + A.Variant(slug)` patterns at compile time.

```csharp
public ClassList PlusVariant(string slug) =>
    new ClassList().Add(this).Add($"{Name}--{slug}");
```

**Hypothesis:** The intermediate `Class` allocation in `.Variant()` is pure waste — it exists for one `+` operation and is immediately GC'd.

**Risk:** Two ways to write the same thing; the analyzer would need to nudge users toward the cheap one. If we just add the helper, users may not find it.

**Expected delta:** removes one heap allocation per kanban-card render. ~32 B/op saved. Time small (allocations on this scale are fast).

**Decision criteria:** Merge the helper unconditionally (it's strictly an additive option). Add the analyzer (BCA005?) only if we measure the unhelped path being a real allocation hot spot in realistic renders.

### 4.5 [E] Razor source-gen interception that rewrites `class="@DnD.Card"` to a literal

**Change:** an incremental source generator running over `.razor.g.cs` output (or, more cleanly, an `IRazorEngineFeature` that runs during Razor compilation) detects `@MyStyleSheet.Field` patterns where `MyStyleSheet : StyleSheet` and `Field : Class`, and rewrites them to `(string)__StyleSheet_Field_Name`, where the name is a `const string` emitted by the existing `CssClassNameGenerator`.

**Hypothesis:** This is the only way to literally hit const-equivalent. Everything else has at least a field load + method call.

**Risk:** Large complexity bump. Razor source-gen interaction is poorly documented. The Razor team's stable hooks may not let us intercept at the right phase. Likely a multi-week investigation.

**Expected delta:** drops to const literal — ~1 ns and 0 B per access.

**Decision criteria:** Pursue only if (a) [A]+[B]+[D] together still show a measurable per-render delta against const literal in the realistic Blazor benchmark AND (b) the same delta would matter to a real consumer (MitWare's render measurements). Otherwise, document as "deferred — diminishing returns."

### 4.6 [F] `Name` from property to plain `readonly` field

**Change:** `public string Name { get; set; }` → `public string Name;`. The setter is currently public (so the source-gen ModuleInitializer can write it from another assembly).

**Hypothesis:** A property getter call vs a field load. The JIT inlines property getters for trivial properties to a field load anyway, so this is probably zero delta in Release mode.

**Risk:** Public mutable field is a code-smell signal in C#. We made the setter public for source-gen reasons; making it a field doesn't change the abstraction.

**Expected delta:** ~0 ns. Don't bother unless disassembly shows the property isn't being inlined.

**Decision criteria:** Skip unless disassembly proves there's a real difference. Most likely a no-op.

---

## 5. A/B comparison protocol

For each candidate:

1. **Branch from `main`.** Name: `perf/cscss-A-skip-ensurescanned` (etc.).
2. **Run baseline** on `main`: `dotnet run -c Release ... --filter "*"`. Capture markdown table to `docs/plans/benchmarks/YYYY-MM-DD-baseline.md`.
3. **Apply the change** on the branch.
4. **Run candidate**: same command, capture to `docs/plans/benchmarks/YYYY-MM-DD-candidate-A.md`.
5. **Diff side-by-side.** BenchmarkDotNet's exporter can produce a comparison table: `BenchmarkRunner.Run<T>(config.AddExporter(MarkdownExporter.GitHub))`. The Mean / Allocated columns tell the story.
6. **Decide.** Is the delta:
   - **Real** (outside the noise band, typically the StdDev column, ideally 3× StdDev)?
   - **Big enough to matter** in the realistic Blazor render benchmark?
   - **Worth the complexity** of the change?
7. **Document.** Even rejected candidates get their numbers committed to `benchmarks/`. Future maintainers want to know what's already been tried.
8. **Merge or close.** No "we'll revisit later" without a written decision.

---

## 6. End-to-end Blazor render benchmark — the sanity check

The microbenchmarks above measure pieces in isolation. Reality is: a Razor render does many things, and our hot path is one of them. A 5-ns improvement on `Class → string` may be invisible against a render that takes 50 µs.

Therefore: **every microbenchmark optimization must also be tested against the realistic render.**

Strategy: a Blazor component that mirrors the DnD test page's kanban column (a realistic small component — header with variant, body with N cards). Render it via `RenderTreeBuilder` directly:

```csharp
public class KanbanColumnRenderBenchmark {
    private RenderTreeBuilder _builder;
    private KanbanColumn _component;

    [Params(3, 10, 50)]
    public int CardCount;

    [GlobalSetup]
    public void Setup() {
        _builder = new RenderTreeBuilder();
        _component = new KanbanColumn { Cards = MakeCards(CardCount) };
    }

    [Benchmark]
    public void Render() {
        _builder.Clear();
        _component.BuildRenderTreeForBenchmark(_builder);  // helper exposed for tests
    }
}
```

Run before/after each candidate. **The microbenchmark may show a 30% improvement that disappears at this level. That's the signal to skip the candidate** — the cost wasn't actually on the critical path.

---

## 7. Decision criteria — what counts as "real"

| Outcome on microbenchmark | Outcome on realistic render | Action |
|---|---|---|
| ≥10% faster, ≤noise on render | nothing measurable | Reject — micro win, not a real win |
| ≥10% faster, ≥1% on render | clearly outside noise | Merge if complexity is small |
| <10% faster on either | — | Reject — within measurement noise |
| Slower on either | — | Reject (obviously) |
| Faster but allocation up | — | Reject — alloc churn is harder to reason about than ns |

Discipline: **don't merge based on intuition.** If the numbers don't say merge, don't merge — even if the change feels right. The plan exists so future readers can see *what was actually tried and what worked*.

---

## 8. Rollout order

Sequencing matters because earlier changes affect what later ones can measure:

1. **Land the benchmark project itself.** No production-code changes. PR includes the §3 baseline numbers. This is the foundation; everything below references it.
2. **[A] Skip EnsureScanned.** Smallest change with the biggest expected win. Low risk if we keep the lazy fallback for first-access-on-uninitialized.
3. **[D] Bypass `.Variant()` allocation.** Independent of [A], purely additive. Worth a separate run to isolate the allocation savings from the time savings.
4. **[B] AggressiveInlining.** After [A]; rerun to see if this still moves the needle.
5. **End-to-end render check** with current state of [A]+[B]+[D]. If we're within noise of const literal at the realistic-render level: stop here.
6. **[E] Razor interception** — only if step 5 says we're not done yet. Big effort, only justified by a measurable shortfall.
7. **[F] field vs property** — only if disassembly shows the property getter isn't being inlined.
8. **[C] struct Class** — last resort. Big API impact for small absolute savings.

---

## 9. Open questions to resolve via the benchmarks

These are predictions from reading the code; the benchmarks decide them definitively:

- **Is `EnsureScanned` the dominant cost on the hot path?** Theory says yes (volatile read + one indirection). [A] benchmark confirms or refutes.
- **Does the JIT already inline `Class → string`?** Disassembly check before [B]. If yes, [B] is a no-op.
- **Is the StringBuilder in `ClassList.ToString` a real cost?** [B]-section benchmark shows allocation count. If 0 B (some pooling we missed?) — disregard. If ~120 B per call — meaningful.
- **Does any of this matter against a real Razor render?** §6 benchmark settles it. If a kanban column with 50 cards takes 5 µs and our biggest microbenchmark saving is 50 ns × 50 = 2.5 µs, that's significant. If it takes 5 ms, we're chasing 0.05%.
- **Do Apple Silicon and x86 give materially different numbers?** Volatile reads behave differently. Worth a comparison run on a Linux x86 box if we have one — note the difference in the rollout doc.

---

## 10. Acceptance / done criteria for this whole effort

The work is "done" when:
1. The benchmark project lives in the repo with at minimum the §3 baseline benchmarks running green.
2. Each optimization candidate has either landed (with its before/after markdown in `docs/plans/benchmarks/`) or has a written rejection note explaining why the numbers didn't justify it.
3. The realistic Blazor render benchmark shows the chosen set of optimizations together produce a measurable improvement against the `main` baseline — or, if not, we have a written conclusion that the spec's "equivalent to a const" promise is ~5 ns short and explicitly accepted as the practical outcome.
4. The Authoring README's performance section (currently silent) gets one paragraph stating the actual measured cost, so users planning hot-path renders know what to expect.

The point isn't to hit some abstract perf target — it's to **replace handwave-y "should be fine" with a measured number, in the doc, that the next person can trust.**
