# CSS-in-C# emission modes — hot reload, sidecar, static assets

**Decided 2026-08-21 (Konrad + MitWare "MudBlazor Exodus" planning).** This plan
gives the CSS-in-C# system three output modes around ONE canonical renderer,
kills the edit-loop downside (a `.css.cs` tweak currently costs a full watch
rebuild), and records the retirement of the SCSS intermediate.

Driving consumer: MitWare's component-library migration
(`MitWare/docs/plans/mudblazor-exodus-plan.md` §4a) — but every piece is
general-purpose library work, not MitWare-specific.

## The mode model

`StyleSheet.Render` (the runtime renderer, 678 tests) is the **single
canonical emitter**. Modes differ only in where its output lands:

| Mode | When | Mechanics |
|---|---|---|
| 1. Runtime `<style>` | default, zero-build (today) | `<BrowserApiCss/>` renders once, cached |
| 2. Hot-reload push | dev | MetadataUpdateHandler → re-render → swap style content live |
| 3. Static asset | prod, exports | MSBuild target / sidecar writes `.css` to `wwwroot`, app serves `<link>` |

Authoring never changes: same `.css.cs` files, same typed API. Mode selection
is an `AddBrowserApiCss` option (+ the component picks `<style>` vs `<link>`).

## Workstream 1 — hot-reload awareness (~1–2 days)

Today a value tweak needs a rebuild because `static readonly` field
initializers run once in the cctor (EnC patches the initializer *body* but
never re-runs it) and `BrowserApiCss` caches its render.

1. `[assembly: MetadataUpdateHandler(typeof(CssHotReload))]` —
   `ClearCache`/`UpdateApplication` fire after every applied EnC delta.
2. **Registry constructs stylesheet instances** instead of reading static
   state. Instance field initializers compile into the ctor body; ctor bodies
   are EnC-patchable; a fresh instance executes the patched body → new
   values materialize. (Declaration syntax barely changes; keep a
   compat shim for the `static readonly` style if feasible.)
3. On update: re-instantiate → re-render → invalidate cache → push to the
   browser (re-render `BrowserApiCss`, or a two-line interop swapping the
   `<style>` textContent — no page reload, Blazor Server state preserved).

Result: save → running app restyles in ~1 s. Boundaries (accepted): value /
body edits and *added* members/types hot-apply; renames and deletions are
rude edits → normal rebuild.

## Workstream 2 — Roslyn sidecar watcher (~1 day)

`.css.cs` files are a **closed dependency island** (BrowserApi + each other
only) — the reason a file-ending-scoped compile is legitimate here when it
isn't in general:

1. Watch `**/*.css.cs`.
2. On change: `CSharpCompilation` of exactly those files + the BrowserApi
   reference — tens of ms, no MSBuild, no Razor.
3. `Emit` to memory → collectible `AssemblyLoadContext` → `StyleSheet.Render`
   → write static CSS output.

Works without the app running; serves design-mode iteration; **doubles as
the token exporter** for non-.NET consumers (MitWare's Svelte builds consume
the generated `theme.css` after its token inversion).

## Workstream 3 — static asset emission (mode 3)

- Emit on build: MSBuild target after compile calls the same renderer
  (via the sidecar binary or an `Exec`ed tool) → `wwwroot/<prefix>.css`.
- **Deterministic names**: compile-time (`CssClassNameGenerator`) and
  runtime name population must agree — already the generator's contract;
  add a snapshot test pinning both paths to identical output.
- **Drift protection** (the one new failure mode): embed a content hash of
  the emitted file; the app verifies at startup that the served file matches
  the loaded assembly, else falls back to runtime mode + logs.
- Gains: browser-cacheable CSS across sessions, no inline `<style>` (strict
  CSP without `unsafe-inline`/nonce), styles exist independent of a running
  circuit, and the export path for foreign build systems.

## Source maps — respec (supersedes spec §27)

Spec §27 chained C# → SCSS → CSS maps. With SCSS retired (below), emit the
CSS→C# map **directly**: the source generator already knows every
declaration's file/line (`GetLocation()`); the renderer writes a `.map`
alongside mode-3 output (and optionally a `sourceMappingURL` on mode 1/2).
Simpler than chaining two maps across an SCSS hop. Priority: after the three
modes; readable prefixed class names carry debugging until then.

## Decision record — SCSS intermediate RETIRED (2026-08-21)

The original spec pipeline (§ "Build Pipeline", §12) was
`C# → generated .scss → dart-sass → .css`, with dart-sass as a documented
peer dependency. During implementation the runtime renderer shipped first
and Sass was parked as a stretch goal. It is now retired **because every
benefit it was chosen for became moot**:

| Believed benefit (spec) | Why it no longer holds |
|---|---|
| `&` parent-reference resolution + nesting flattening ("C# doesn't track this — SCSS handles it") | The runtime renderer had to implement full selector/nesting resolution in C# anyway (recursive indexer, source-order preservation) — under test, shipping. Sass would now re-do finished work. |
| Compile-time color math on literals | Implementation went CSS **relative color syntax**, which also works on `var()` — strictly more capable than Sass, which cannot process CSS variables at all (the spec itself noted this split in §"two strategies"). |
| Free source maps (Sass auto-emits CSS→SCSS; chain with our SCSS→C# map) | The generator knows every declaration's file/line; direct CSS→C# map emission is simpler than chaining across an intermediate. |
| Mature, battle-tested CSS emitter so we don't write one | The emitter got written regardless (the runtime path required it) and is snapshot-tested. Minification, if ever wanted, is a trivial optional pass. |
| Inspectable intermediate artifact | Direct CSS output is equally inspectable — and is the *final* artifact, one fewer translation to reason about. |

And one **new argument that decided it**: dev/hot-reload will always use the
runtime renderer; a Sass-based prod path would mean two pipelines required to
produce byte-identical CSS forever — any drift styles dev and prod
differently. Mode 3 as "the same `Render` output written to a file" is
identical **by construction**.

**Not forgotten:** the C#→SCSS emitter design stays sketched in
`css-in-csharp.md` (historical sections marked, not deleted). It regains
value only if the library ever needs Sass-*ecosystem* interop — consuming
existing SCSS mixin libraries, or emitting SCSS for consumers who
post-process it. Neither is on any horizon; revisit only on a concrete ask.

## Order of work

1. Workstream 1 (hot reload) — unblocks MitWare's design-in-C#-CSS loop.
2. Workstream 3 (static emission + drift guard) — prod story + export path.
3. Workstream 2 (sidecar) — builds on 3's writer; adds the app-not-running
   loop and the token exporter.
4. Source maps — opportunistic, after the modes.

Spec touch-ups shipped with this plan: § Build Pipeline and §27 marked
superseded with pointers here; Authoring README gap list updated.
