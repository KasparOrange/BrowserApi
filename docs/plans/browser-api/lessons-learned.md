# CSS-in-C# — lessons learned during the first integration

**Parent:** [css-in-csharp.md](css-in-csharp.md)

This doc captures what we found when the CSS-in-C# system met its first real consumer (MitWare's `DnDTestPage`). The spec is the design; this is the field report. Read it before doing the next migration.

---

## 1. Status — when to use this and when not to

**Use it for new components, immediately.** Anywhere you'd otherwise hand-write a `.css` file, prefer cscss. Type safety, refactoring, IntelliSense, design-token-as-type — all real wins, no production-relevant downsides at this scale.

**Don't migrate existing CSS wholesale.** Sample size is one page (DnDTestPage). The property surface is ~80 of CSS's hundreds; you will hit "this property isn't typed yet" on real stylesheets. Source maps aren't wired. Sass intermediate isn't wired (we emit CSS direct via relative-color syntax). Hot-reload behavior is unverified.

## 2. Staged migration order — do it like this

If you're migrating MitWare's existing CSS files, do them in this order:

1. **Run the perf plan first** — at minimum capture baseline numbers from the benchmark project so the conversation about "is this fast enough" has data behind it. See [`docs/plans/const-equivalent-cscss-performance.md`](../const-equivalent-cscss-performance.md).
2. **Pick 2–3 small/medium files** — `mw-menu.css`, `mw-accordion.css`, `mw-spoken-language-select.css`. Each ≤200 lines, isolated, low-risk. Each will surface 1–2 missing properties or ergonomic gaps. Fix them in BrowserApi as they come up.
3. **After 3 successful migrations**, you'll have a real sense of "what's missing." That's when to land the property-surface generator from `specs/css/*.json` so the long tail fills in mechanically.
4. **Then attack the big ones** — `scheduler.css`, `mw-data-grid.css`, `mw-form-builder.css`, `app.css`. Separate PRs. Visual diff every time. Keep both stylesheets loaded during the transition; toggle the old one off at the end.
5. **Source maps before the last 30%** — by the time you're touching `app.css`, debugging without source maps will hurt enough to justify the effort to add them.

**Don't migrate `mw-dnd.css` further.** The `mw-dt-*` test rules already moved. The structural `mw-dnd-*` classes (ghost, source, insertion-line, gap-spacer, collapsed) are referenced by JS module names; moving them is a coordinated change with the `.ts` file, not a quick win.

**Keep `.razor.css` scoped CSS as-is.** It's a different mechanism (build-time CSS isolation per component) for a different need (one-off component-specific styles that shouldn't leak). cscss is for global, prefixed, themeable styles. They don't compete.

## 3. Spec-violation audit — checklist for reviewing a stylesheet

Things that creep into a cscss file when you're not paying attention. Run this checklist before merging any new stylesheet PR.

### Real violations — typed alternative exists, you used a string anyway

- [ ] **Raw `Cursor = new("grab")` instead of `Cursor.Grab`.** All the keyword enums have the value you want; check Keywords.cs before using the string ctor escape hatch.
- [ ] **Raw `new CssColor("var(--something)")` for external CSS variables.** Use `CssVar.External<CssColor>("--something")` instead — typed, participates in `IsVariable` taint, supports `.Or()` fallbacks.
- [ ] **Raw `color-mix(...)` strings for transparency or blending.** Use `((CssColor)x).WithAlpha(0.10)` or `.Mix(other, weight)`. Same emitted CSS, typed at the call site, goes through spec §29 dispatch.
- [ ] **`Transition = "opacity 150ms"` strings.** Use `Transition.For(property, duration)`, `Transition.All(...)`, `Transition.Combine(...)`. Raw fallback is `new Transition("...")` — visually distinct.
- [ ] **`BoxShadow = "0 4px 12px ..."` strings.** Use `Shadow.Box(offsetX, offsetY, blur:, spread:, color:, inset:)`. Multi-shadow: `Shadow.Combine(...)`.

### Inconsistencies inside the same file

- [ ] **Padding/margin sometimes via `Sides.Of(a, b)` and sometimes via `(a, b)` tuples.** Pick one — spec §18 prefers the tuple form; BCA001 steers users that way for the 4-element case.
- [ ] **Some keyword enums fully qualified (`global::BrowserApi.Css.X`) and others bare.** Add a using-alias block at the top of the file covering every collision (typically `Position`, `AlignItems`, `JustifyContent`, `Cursor`, `Display`, `Overflow`, `TextTransform`, `WhiteSpace`, `FlexDirection`, `BoxSizing`, `Visibility`, `FlexWrap`, `Transition`, `Shadow`, `Easing`).

### Things that aren't violations but should be commented

- [ ] **`Class.External("...")` for a third-party class.** Per spec §10 this is the *manual fallback* for "edge cases the parser can't handle." If it's because the third-party CSS isn't yet wired through `ExternalCssGenerator`, say so in a comment so future readers know whether to fix the parser path or accept the escape hatch.
- [ ] **`new RawValue(...)`.** The escape hatch for property values that don't have a typed builder yet. Comment why — "no typed gradient builder for `repeating-linear-gradient`" tells the next reader whether to type the missing piece or accept the string.

### Property-surface gaps (not your fault — but flag them)

- [ ] **`UserSelect = "none"`, `TextOverflow = "ellipsis"`, `FontFamily = "monospace"`** — these properties don't have typed enums or builders today. They take strings. When you hit one, file an issue or add the typed wrapper as part of your PR.

## 4. Known gotchas the spec doesn't warn about

### `Class.Variant` returns `Class`, `Selector.Variant` returns `Selector`

Both methods exist. They have the same name on different types because they serve different roles:

```csharp
// In a stylesheet — this is on Self (a Selector) and is used as an indexer key.
[Self.Variant("active")] = new() { Background = ... }
//   ^^^^^^^^^^^^^^^^^^^^ Selector

// In Razor markup — this is on a Class and is used to compose a class list.
class="@(Card + Card.Variant("active"))"
//                  ^^^^^^^^^^^^^^^^^^^^ Class
```

The bug we hit: `Class.Variant` originally returned `Selector`. With `Class` implicitly converting to `Selector`, the `+` between them resolved as `Selector + Selector` (the adjacent-sibling combinator) instead of `Class + Class` → `ClassList`. The rendered class attribute was literally `class=".kanban-header + .kanban-header--todo"` — the browser split that on whitespace, treated each word as a class, and none matched. Variants silently never applied.

The fix is in the regression test `Variant_on_Class_composes_into_ClassList_not_selector`. If anyone is tempted to "simplify" by making `Class.Variant` return `Selector` (since "they should be the same"), this is why they shouldn't.

### `Name` setters on `Class` / `CssVar<T>` / `Keyframes` are `public set`

This is intentional. The `BrowserApi.Css.SourceGen` source generator emits a `[ModuleInitializer]` in the *consuming* assembly (e.g. MitWare) that pre-populates the names. From that assembly's perspective, `internal set` on the BrowserApi side is inaccessible — CS0200. We considered an `InternalsVisibleTo` trick or a shadow type with the right access, but the simplest and clearest fix is `public set`. Misuse (a user writing `MyStyles.Card.Name = "spoofed"`) is theoretically possible but unusual enough that it doesn't justify the encapsulation gymnastics.

### Two-pass scan in `CssRegistry`

Pass 1 (`PopulateFieldNames`) sets `Name` on every `Class`/`CssVar`/`Keyframes` field across every stylesheet. Pass 2 renders each stylesheet to its CSS string. They're separate because cross-stylesheet references (`Background = OtherStyles.Spacing`) only resolve correctly if every variable's name is known *before* any rendering happens. A single-pass scan would emit `var()` for variables whose stylesheet hadn't been rendered yet.

### Late-binding registry for `.Or()` and `Keyframes`-as-string

`Or()` is typically called inside a static field initializer:

```csharp
public static readonly Class Btn = new() {
    Background = Brand.Or(Primary.Or(CssColor.Blue)),
};
```

At that point Brand and Primary haven't been populated by `PopulateFieldNames` yet — they're empty. So `.Or()` doesn't compute the `var(...)` string immediately. It registers a placeholder token (`__late_bind_N__`) in `CssVarFallbackRegistry` and returns a primitive carrying that token. After Pass 1 completes, Pass 2 renders, then resolves all placeholders against the now-populated names. Same pattern handles `Keyframes` referenced as strings (`Animation = SlideIn + " 200ms"`).

If you find yourself touching this, the registry is in `CssVar.cs` next to `CssVar<T>` itself.

### MudBlazor / CSSOM name collisions

A typical cscss file in a MudBlazor app needs aliases for `Position`, `AlignItems`, `JustifyContent`, `Cursor`, `Display`, `Overflow`, `TextTransform`, `WhiteSpace`, `Transition`, `Shadow`, `Easing`, `FlexDirection`, `BoxSizing`, `Visibility`, `FlexWrap`. Some of these collide with MudBlazor's types, others with the CSSOM-generated types in `BrowserApi.Css`. The `using X = BrowserApi.Css.Authoring.X;` block at the top of `DnDTestStyles.css.cs` is the canonical pattern.

When you're refactoring and remove a using-alias, the remaining bare references will trip into the wrong type silently — they'll use a MudBlazor enum that happens to have a member named `Center` or whatever. Watch for unexpected output if a refactor changes which types are in scope.

## 5. Open gaps — what's still unfinished

Items called out in the spec as deferred or not-yet-done. Capturing here so they don't get lost:

| Gap | Spec ref | Effort | Notes |
|---|---|---|---|
| Full property surface auto-generated from `specs/css/*.json` | §17 | medium | ~80 properties typed today, hundreds in CSS spec. Pattern is clear; needs a CodeGen pass over the JSON files. |
| Sass intermediate + chained source maps | §12, §27 | large | Currently emit CSS direct via relative-color syntax. Sass would let users debug at the SCSS layer and produce cleaner output for literal-color cases. |
| Source-gen DX features | §28 | each medium | Scaffold code fix, CSS preview comments, CSS-to-C# converter, extract-to-CssVar refactor. Three analyzers shipped today (BCA001/2/3); the rest are separate work. |
| Property-specific keyword types | §18 | medium | `Width.Auto`, `FontSize.Large` — typed keywords on per-property union types. Some exist (`Length.Auto`, `Length.MinContent`), most don't. |
| Trig functions, scroll-driven animations | §33 | low / wait | Listed as Post-MVP. Low priority. |
| Single-place `Program.cs` config | §20 | medium | Currently config is split across MSBuild properties (`GlobalPrefix`), `.editorconfig` (analyzer severity), and `AddBrowserApiCss(opts =>)` runtime. The aspiration is one place. Three paths sketched in §20; pick one after the MVP friction map is real. |
| Razor source-gen interception for true const-equivalent name access | perf plan §4.5 | large | Only path to ~1-ns access for `class="@AppStyles.X"`. Worth pursuing only if perf benchmarks (which haven't run yet) say it matters at the realistic-render level. |
| BCA004 override-conflict analyzer | §35 | medium-large | "Same property declared twice with declining specificity within the same stylesheet." Hard part is the "could match the same element" analysis. Defer until BCA003 has signal from real usage. |

## 6. The `CHANGELOG` framing

When this lands in a release, frame the entry as:

- **Headline: a major addition, but additive — no breaking changes** to the existing JS-interop / CSSOM types.
- **Two breaking changes, both small and mechanical:** `CssUnitExtensions` from methods to extension properties (`16.Px()` → `16.Px`); `BoxShadow`/`Transition` setters from `string` to typed `Shadow`/`Transition` (raw-string callers wrap as `new Shadow("...")` / `new Transition("...")`).
- **Status:** ready for new components, not yet for wholesale CSS migration. Point at this doc.
- **Cross-link** the spec, the user guide (DocFX article), and the perf plan.

## 7. Files that matter for the next agent

Sorted by relevance for "I need to understand the system before I touch it":

1. **[`docs/plans/browser-api/css-in-csharp.md`](css-in-csharp.md)** — the master spec. 35 sections. Read this first.
2. **[`src/BrowserApi/Css/Authoring/README.md`](../../../src/BrowserApi/Css/Authoring/README.md)** — feature matrix, three-place wiring, sample rendered output.
3. **This file** — what we found in practice. The audit checklist (§3) is the most reusable artifact.
4. **[`docs/docfx/articles/css-in-csharp.md`](../../docfx/articles/css-in-csharp.md)** — user-facing guide; lighter than the spec, more concrete than this doc.
5. **[`docs/plans/const-equivalent-cscss-performance.md`](../const-equivalent-cscss-performance.md)** — the measurement plan. Not yet executed.
6. **`src/BrowserApi/Css/Authoring/*.cs`** — every public type has full XML docs. The implementation doc IS the code.
7. **`src/BrowserApi.Css.SourceGen/*.cs`** — three generators, three analyzers, all with header docs.
8. **`MitWare.Blazor/Components/Pages/DnDTestStyles.css.cs`** (in the MitWare repo) — the canonical real-world example. Read this to see what a clean file looks like after applying the §3 audit.

Anything not in those eight files is either implementation detail or out of scope.
