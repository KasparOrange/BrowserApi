# JSImport Backend (Proposal)

**Parent:** [browser-api.md](browser-api.md)

**Status:** Proposal. Not part of the current roadmap. Worth considering once `BrowserApi.SourceGen` stabilizes and the main type generation work is further along.

## The Idea in One Sentence

Keep the TS-driven developer experience of `BrowserApi.SourceGen` exactly as it is, but when the consuming project targets Blazor WebAssembly, have the generator emit method bodies that use .NET's `[JSImport]` attribute instead of `IJSRuntime.InvokeAsync` — trading zero code changes for a 10–100× speedup on the hot path.

## Background: The Two Interop Paradigms

.NET has two fundamentally different JavaScript interop mechanisms, and they don't overlap:

**`IJSRuntime` (classic, Microsoft.JSInterop)**

- Stringly-typed RPC: `js.InvokeAsync<T>("methodName", args)`
- Args round-trip through JSON serialization
- Async by default; sync requires casting to `IJSInProcessRuntime`
- Works in both Blazor Server (over SignalR) and Blazor WebAssembly
- Per-call overhead in WASM: ~1–100 µs depending on payload and whether it's sync or async
- In Blazor Server: every call is a network round-trip (milliseconds, not microseconds)

**`[JSImport]` / `[JSExport]` (.NET 7+, System.Runtime.InteropServices.JavaScript)**

- Partial methods bound to real JS functions by a source generator
- Goes through the mono WASM runtime's direct marshaling layer — no JSON, no `IJSRuntime`, no async plumbing
- Sync by default
- Supports `JSObject` handles, `Span<T>` memory views (zero-copy), delegate-as-callback marshaling
- **WebAssembly only.** Cannot work in Blazor Server because there's no shared memory — all "interop" in Server is a network call
- Per-call overhead: ~100–500 ns for simple calls

Today, `BrowserApi.SourceGen` emits `IJSRuntime` calls exclusively. This is the right default: it works everywhere. But for WASM consumers, it leaves a lot of performance on the table.

## What This Proposal Adds

A second emission backend that produces `[JSImport]`-based method bodies, selected automatically based on the consuming project's target framework (or build property), without changing the public API surface.

The TS/JSDoc parsing stays the same. The record and enum generation stays the same. The `AddJsModules()` DI extension stays the same. Users write the same C# code. What changes is *what the generator emits* behind the `partial class`.

### Illustrative emission difference

Given this JavaScript:

```typescript
// mw-dnd.ts
export function startDrag(elementId: string, x: number, y: number): void { /* ... */ }
export function getBounds(elementId: string): Bounds { /* ... */ }
```

**Today's emission (IJSRuntime backend):**

```csharp
public async Task StartDragAsync(string elementId, double x, double y) {
    var module = await GetModuleAsync();
    await module.InvokeVoidAsync("startDrag", elementId, x, y);
}

public async Task<Bounds> GetBoundsAsync(string elementId) {
    var module = await GetModuleAsync();
    return await module.InvokeAsync<Bounds>("getBounds", elementId);
}
```

**Proposed WASM emission ([JSImport] backend):**

```csharp
public partial class MwDndModule {
    [JSImport("startDrag", "mw-dnd")]
    internal static partial void StartDragCore(string elementId, double x, double y);

    [JSImport("getBounds", "mw-dnd")]
    internal static partial string GetBoundsCoreJson(string elementId);

    public void StartDrag(string elementId, double x, double y) {
        EnsureImported();
        StartDragCore(elementId, x, y);
    }

    public Bounds GetBounds(string elementId) {
        EnsureImported();
        var json = GetBoundsCoreJson(elementId);
        return JsonSerializer.Deserialize<Bounds>(json)!;
    }
}
```

Notice that the WASM version's public methods are **synchronous** — no `Async` suffix, no `Task`, no `await` at the callsite. This is a significant ergonomic win in tight loops, animation frames, and event handlers.

(The JSON step for complex types could be replaced over time with direct `JSObject` marshaling for even better performance. First pass can just use JSON — still much faster than `IJSRuntime` because it skips the RPC dispatch, serialization context, and async state machine.)

## Why It's Worth Doing

### 1. Performance where it matters

For typical UI interop (`setFocus`, `scrollIntoView`), nobody notices the overhead. But BrowserApi's sweet spot includes things that are much more sensitive:

- **Canvas 2D rendering loops** — 60 fps × dozens of draw calls per frame = thousands of interop calls per second
- **Web Animations driving** — same story
- **WebGL / WebGPU** wrapping — where every microsecond counts
- **Drag-and-drop position updates** — continuous pointer events
- **Typed buffer uploads** (audio samples, vertex data) — zero-copy is the whole point

A `IJSRuntime`-backed canvas wrapper is usable but never going to be great. A `[JSImport]`-backed one can legitimately compete with hand-written TypeScript.

### 2. Sync calls cleanup the API surface

Half the "awkwardness" of C#-in-the-browser is the forced async everywhere. `element.Style.Display = Display.Flex` becomes `await element.Style.SetDisplayAsync(Display.Flex)`. With `[JSImport]`, simple property setters can actually be synchronous, matching JavaScript's mental model and eliminating hundreds of unnecessary `await` keywords in user code.

### 3. Zero-copy is a killer feature for typed buffers

`[JSImport]` supports `[JSMarshalAs<JSType.MemoryView>] Span<T>`, which gives JavaScript a direct view into WASM linear memory. For anything involving numerical buffers — audio PCM data, vertex arrays, image pixels, simulation state — this is transformative. Today's `IJSRuntime` path would require base64 encoding or massive JSON arrays.

If BrowserApi ever grows a Canvas, WebGL, WebGPU, or Web Audio wrapper, this matters enormously.

### 4. It differentiates BrowserApi from every other JS interop library

The current `IJSRuntime` wrapper market is crowded. Adding a `[JSImport]` backend — on top of the existing schema-first DX — puts BrowserApi in a position nothing else occupies:

> "Write your JS module with TypeScript types. Get both a Blazor Server wrapper and a native-speed Blazor WASM wrapper, from the same source of truth, with zero extra work."

That's a genuinely novel value proposition. Nothing in the .NET ecosystem does that. Microsoft's `[JSImport]` requires you to hand-write every method declaration; BrowserApi.SourceGen derives them from the JS. Combining the two captures the best of both worlds.

### 5. Users don't have to choose

With a dual-backend generator, the same JavaScript module and the same TS types produce:

- A Blazor Server wrapper (async, `IJSRuntime`, works over SignalR)
- A Blazor WASM wrapper (sync where possible, `[JSImport]`, native speed)

The user writes their app against the same `MwDndModule` class either way. Swap the host, redeploy, done. This is a very strong pitch for libraries that want to support both hosting models without maintaining two implementations.

## How It Might Work Technically

### Backend selection

Two plausible strategies:

1. **MSBuild property driven.** A `<BrowserApiBackend>wasm</BrowserApiBackend>` property in the consuming csproj, detected by the generator via `AnalyzerConfigOptions`. Default to `iskruntime` for compatibility.

2. **Target framework driven.** Detect `browser-wasm` / Blazor WebAssembly SDK automatically and switch backends without user configuration.

Option 2 is cleaner for the common case but less flexible. Option 1 is explicit and easier to debug. Probably offer both: auto-detect by default, override via property.

### Dual emission

The generator currently has a single `EmitFunction` method. Refactoring would introduce:

```
IEmitBackend
├── IJSRuntimeBackend  (current behavior)
└── JSImportBackend    (new)
```

Each backend takes the parsed `JsFunctionInfo` and emits the method body (and any supporting partial methods, using statements, or class-level state like `_modulePath`).

### Async vs sync in WASM mode

Three tiers for the WASM backend:

- **Simple value types in + void out:** emit sync, direct `[JSImport]`
- **Simple value types in + simple value types out:** emit sync, direct `[JSImport]` with return marshaling
- **Complex types (records, arrays of records, JS objects):** emit async for now, with the method body doing `JSImport` + JSON round-trip (still faster than `IJSRuntime`, mainly because there's no async dispatch)

Over time, the third tier can be upgraded to use direct `JSObject` marshaling and generated marshal helpers, reducing the overhead further.

The public method signature should change between backends: `DoThingAsync` on Server, `DoThing` on WASM where the call is actually sync. This is a break from the "one API surface" principle but it's the right call — forcing users to `await` something that doesn't actually await defeats the purpose. Users writing cross-host code can wrap calls themselves, or the generator can emit *both* sync and async variants in WASM mode for compatibility.

### Module loading

`[JSImport]` modules must be registered via `JSHost.ImportAsync("moduleName", "./path.js")` before any of their methods are called. The generator would emit a `EnsureImportedAsync()` helper on the partial class that lazily does this, or a `ImportModulesAsync()` registration helper that the user calls once at app startup. The `IJsModulePathResolver` hook still applies — it just feeds into `JSHost.ImportAsync` instead of the dynamic `import()` call.

### Project structure impact

A new assembly or MSBuild target may be needed:

- `BrowserApi.SourceGen` stays as the single generator package
- A small runtime-support library `BrowserApi.SourceGen.WasmSupport` may be needed to host shared helper methods called from generated code (JSON marshaling helpers, module registration helpers, etc.). Only referenced by WASM projects.

Or keep everything in the generated code and avoid a runtime dependency — simpler to consume but more boilerplate in every generated file.

## What Stays the Same

- **The user's JS/TS source is unchanged.** Same JSDoc, same `.d.ts`, same modules.
- **The user's C# callsites are unchanged** (with the caveat about async→sync for tight hot-path methods — this is opt-in).
- **The parser (`TsDeclarationParser`, `JsDocParser`) is unchanged.**
- **Record and enum generation is unchanged.**
- **The `IJsModulePathResolver` hook is unchanged.**
- **`AddJsModules()` still works**, though in WASM mode it may do module registration differently.

This is crucial: the proposal is purely additive. Existing users of `BrowserApi.SourceGen` get the WASM speedup for free when they build a WASM project. Nothing breaks.

## Non-Goals

- **Not replacing `IJSRuntime` emission.** The classic backend remains. Blazor Server is a first-class target and must continue to work.
- **Not introducing a new public C# API.** The generator output is the API; users should not need to know which backend produced a given class.
- **Not wrapping `[JSImport]` itself as a user-facing feature.** Users who want to write raw `[JSImport]` methods can do so directly — that's already supported by .NET. This proposal is about having the generator emit them as an implementation detail.
- **Not attempting to support `[JSImport]` in Blazor Server.** It's architecturally impossible.

## Risks and Open Questions

### Risk: async↔sync API divergence

If WASM wrappers expose sync methods and Server wrappers expose async methods, library authors targeting both hosts will have to write conditional code. Mitigation: emit async *wrappers* around sync methods in WASM mode too, so `FooAsync()` exists in both cases and users can write host-agnostic code. Users who want the raw sync performance opt in explicitly.

### Risk: marshal complexity for records

The first version's "JSON round-trip through `[JSImport]`" is a compromise. It's not as fast as `[JSImport]` can be, but it's far faster than `IJSRuntime` and much easier to implement than a full custom marshaler. Over time, generate direct field-by-field marshal code using `JSObject` accessors. This is significant work — probably a second phase after the basic backend ships.

### Risk: `[JSImport]` is not a Roslyn source generator the user controls

It's another source generator, owned by Microsoft, that runs on your `[JSImport]`-annotated partial methods. That means BrowserApi.SourceGen emits code that triggers another generator. Generator composition is well-supported in modern Roslyn but edge cases exist (initialization order, analyzer conflicts). Needs testing across .NET versions.

### Risk: tight coupling to .NET runtime version

`[JSImport]` is .NET 7+. BrowserApi currently targets .NET 10 so that's fine today, but any marshal attribute additions Microsoft makes in future versions (e.g. new `JSType.*` variants) are moving targets. The generator needs to target a specific `[JSImport]` surface and probably gate on a minimum .NET version.

### Open question: is there a Server shortcut too?

Blazor Server's interop path is network-bound. There's no analog to `[JSImport]` for it. But there *is* room for optimization: batching multiple interop calls into a single SignalR frame, or using `IJSUnmarshalledRuntime`-style tricks if any exist server-side. This is out of scope for this proposal but worth noting as a separate future topic.

### Open question: debug experience

`IJSRuntime` errors come back with useful stack traces that name the JS method. `[JSImport]` errors bubble up as WASM runtime exceptions, which can be harder to read. The generator could wrap `[JSImport]` calls in try/catch with better error messages, at a small performance cost (toggleable).

## When to Revisit

Not now. The priorities are:

1. Finish the core WebIDL-driven type generation (Phases 1–3 in the [master plan](browser-api.md))
2. Build out the `IJSRuntime` interop backend (Phase 4)
3. Ship `BrowserApi.SourceGen` as a stable, useful tool in its current form
4. Collect real usage feedback on what interop patterns matter most

**Revisit this proposal when:** (a) `BrowserApi.SourceGen` has real users, (b) performance complaints or feature requests around WASM interop surface, or (c) a compelling BrowserApi use case emerges that's bottlenecked by `IJSRuntime` overhead (Canvas loops, WebGL, audio — the likely candidates).

Until then, this document exists so the idea doesn't get lost.

## Inspiration / Prior Art

- **Microsoft's own rewrite of `HttpClient` for WASM.** In .NET 7, the browser `HttpClient` backend was rewritten from an `IJSRuntime`-based handler to one that uses `[JSImport]` to call `fetch()` directly. Massive performance improvement for HTTP in WASM. The same pattern applies here.
- **`[DllImport]` → source-generated `LibraryImport`.** .NET's native interop went through exactly this evolution: from a runtime-reflected, string-based mechanism to a source-generated, strongly-typed one. `[JSImport]` is the JavaScript equivalent. BrowserApi.SourceGen can piggyback on that transition.
- **Protobuf / OpenAPI codegen tools.** They all derive strongly-typed clients from a schema. BrowserApi.SourceGen already does this for JS, with `.d.ts` as the schema. Adding a faster transport is the same pattern these tools use when they offer gRPC vs REST backends.
