# CSS-in-C# emission modes — hot reload, sidecar, static assets

**Decided 2026-08-21 (Konrad + MitWare "MudBlazor Exodus" planning).** This plan
gives the CSS-in-C# system three output modes around ONE canonical renderer,
kills the edit-loop downside (a `.css.cs` tweak currently costs a full watch
rebuild), and records the retirement of the SCSS intermediate.

Driving consumer: MitWare's component-library migration
(`MitWare/docs/plans/mudblazor-exodus-plan.md` §4a) — but every piece is
general-purpose library work, not MitWare-specific.

## The mode model

`StyleSheet.Render` (the runtime renderer, snapshot- and unit-tested) is the **single
canonical emitter**. Modes differ only in where its output lands:

| Mode | When | Mechanics |
|---|---|---|
| 1. Runtime `<style>` | default, zero-build (today) | `<BrowserApiCss/>` renders once, cached |
| 2. Live reload | dev | in-process island compile → `CssRegistry.SetOverride` → `<BrowserApiCss/>` re-renders |
| 3. Static asset | prod, exports | build runs `browserapi-css build` automatically, writes `.css` to `wwwroot`, app serves `<link>` with drift guard |

Authoring never changes: same `.css.cs` files, same typed API. `css.StaticAssetPath`
set/unset and `css.HotReload` in `AddBrowserApiCss` select the mode; the
component picks `<style>` vs `<link>` accordingly.

## Configuration — decided 2026-08-21: `Program.cs` is the only place

The first cut of this work shipped with config spread over three surfaces
(`AddBrowserApiCss` lambda, `browserapi-css --prefix/--out`, MSBuild
`BrowserApiCssEmit/Prefix/Output`) with the drift guard as the only thing
keeping them honest. Rejected on review — "correct but three places to find
and keep in sync" is the opposite of the DX north star. Also rejected: a
hand-written assembly attribute as the single place (one place, but not the
one .NET developers expect).

Shipped instead — the spec §20 "aspirational" path 1:

- `AddBrowserApiCss(css => { … })` is the single surface: `GlobalPrefix`,
  `StaticAssetPath`, `HotReload`, `AllowAppReferences`.
- `CssConfigGenerator` (in `BrowserApi.Css.SourceGen`) reads the lambda at
  build time and emits `[assembly: BrowserApiCssConfig(...)]` from its
  **constant** assignments. The `browserapi-css` tool reads the attribute
  from the built DLL via `MetadataLoadContext` (no app execution). The
  class-name generator takes `GlobalPrefix` from the same lambda.
- **BCA005** warns when a build-time-relevant option is non-constant.
- MSBuild target in the Compiler package runs unconditionally after `Build`,
  passes `--app $(TargetPath)`, and the tool exits quietly when
  `StaticAssetPath` is unset. Tool presence is detected via the tool
  manifest; absence → one-line message, never a build failure.
- Hot reload: `AddBrowserApiCss` discovers `BrowserApi.Css.Compiler` by
  reflection (no Roslyn dependency in the Blazor package); the hosted
  service is a no-op outside Development.
- CLI flags (`--out`, `--prefix`, `--allow-app-references`, `--reference`)
  remain as explicit overrides for export scenarios only.
- `CssEmissionMode` removed: `StaticAssetPath` set ⇒ static with drift-guard
  fallback; unset ⇒ inline.

Constraint accepted: values must be literals/consts to reach build-time
tools. That is how ~all apps set a prefix; the analyzer makes the exception
visible rather than surprising.

## Status (2026-08-21) — modes 2 and 3 shipped; WS1 as written was infeasible

Built in one pass, with a design change to Workstream 1 (rationale below):

| Piece | Where |
|---|---|
| `StyleSheet.RenderAll` — two-pass render over an explicit type list, deterministic order | `src/BrowserApi/Css/Authoring/StyleSheet.cs` |
| `CssRegistry.SetOverride` / `HasOverride` / `Changed` event; island assemblies excluded from the scan | `src/BrowserApi/Css/Authoring/CssRegistry.cs` |
| `CssOptions.{StaticAssetPath, HotReload, AllowAppReferences}` + generated `BrowserApiCssConfigAttribute` | `src/BrowserApi/Css/Authoring/CssOptions.cs` |
| Island compiler, watcher, hot-reload hosted service (auto-registered by `AddBrowserApiCss`) | `src/BrowserApi.Css.Compiler/` (new package) |
| `CssConfigGenerator` + BCA005 analyzer | `src/BrowserApi.Css.SourceGen/CssConfigGenerator.cs` |
| `browserapi-css build|watch` dotnet tool | `src/BrowserApi.Css.Cli/` |
| `BrowserApiCssEmit` MSBuild target (ships with the Compiler package; needs the CLI tool installed) | `src/BrowserApi.Css.Compiler/buildTransitive/*.targets` |
| `<BrowserApiCss />`: re-renders on `Changed`; `<link>` + drift guard in StaticAsset mode | `src/BrowserApi.Blazor/BrowserApiCss.cs` |
| Tests: island ≡ runtime output, cross-sheet order, failure path, watcher debounce, override, config generator/analyzer, app references | `tests/BrowserApi.Css.Compiler.Tests/` |

Also fixed along the way: `CssVar.ToCss()` now late-binds an unnamed variable
instead of emitting `var()` — exposed once stylesheet order became
deterministic (a sheet whose cctor reads another sheet's `CssVar` could run
before that sheet was named).

## Workstream 1 — hot reload: original design REJECTED, replaced by the island loop

The original step 2 ("registry constructs stylesheet instances so instance
field initializers re-run under EnC") was scrutinized before building and
dropped:

- Instance fields break the core DX. `class="@AppStyles.Btn"` and
  `Padding = Tokens.Spacing` are static member accesses; an instance model has
  no static surface to offer without a generated facade and a naming scheme
  for it. "Declaration syntax barely changes" did not hold.
- The static alternative was verified dead: `Type.TypeInitializer.Invoke`
  does **not** re-run a type initializer on .NET 10 (tested; the runtime
  short-circuits). No supported way to re-evaluate `static readonly`
  initializers exists.

What replaced it — and is strictly better: the Workstream 2 sidecar runs
**in-process** as a hosted service. Save → Roslyn compiles only the
`*.css.cs` island (tens of ms) → collectible `AssemblyLoadContext` →
`StyleSheet.RenderAll` → `CssRegistry.SetOverride` → `<BrowserApiCss />`
re-renders via `Changed`. The app's own `static readonly` fields are never
touched; class names are a pure function of field name + prefix, so existing
markup keeps resolving while the CSS behind it changes.

Advantages over the EnC design: no dependency on `dotnet watch` or EnC rude-edit
rules (renames/deletes/new members all work, because every save is a full
island compile); works under plain `dotnet run`; one mechanism serves the
live loop, the app-not-running `watch`, and mode-3 emission. Cost: a
dev-only Roslyn dependency (kept out of `BrowserApi.Blazor` in its own
package) and the island rule — `.css.cs` files may reference `BrowserApi`
and each other only.

Boundaries (accepted): Razor references to a *new* class still need
`dotnet watch`'s own hot reload; `.css.cs` files that reach outside the
island fail the island compile (reported as a normal C# diagnostic; last
good CSS keeps serving).

## Workstream 2 — island compiler (shipped as `BrowserApi.Css.Compiler`)

As planned, plus: `css.AllowAppReferences` (island also references the app's last-built assembly; CS0436 duplicate-type noise suppressed) and CLI `--reference` for islands that
consume a component library's token stylesheets; debounced, serialized
watcher; and the result keyed per stylesheet type so `<BrowserApiCss Only=…>`
keeps working under an override. Known dev-only leak: late-bound `CssVar`
closures pin each island assembly after unload (KBs per save).

## Workstream 3 — static asset emission (shipped)

- `browserapi-css build --project DIR --app DLL` reads config from the DLL's
  `[BrowserApiCssConfig]`; the Compiler package's target runs it after `Build`
  with no user-facing MSBuild properties (`BrowserApiCssEmit=false` and
  `BrowserApiCssToolCommand` exist as escape hatches only). Tool installed via
  local manifest → `dotnet browserapi-css`.
- **Deterministic names/order**: stylesheets are sorted by type full name in
  both the registry and the island compiler — required so app and CLI produce
  identical bytes; pinned by the island-≡-runtime test.
- **Drift protection**: `<BrowserApiCss />` compares the file's content to the
  runtime render once per process (no embedded hash needed — the renderer is
  in the app). Mismatch → inline fallback + warning. The `?v=` query is a
  short SHA-256 of the content.
- Not done: per-stylesheet files, minification pass, source maps.

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

1. ~~Workstream 1 (hot reload)~~ — shipped as the in-process island loop.
2. ~~Workstream 3 (static emission + drift guard)~~ — shipped.
3. ~~Workstream 2 (sidecar)~~ — shipped (`browserapi-css watch`).
4. Source maps — opportunistic, next.
5. MitWare adoption: bump to the release carrying these packages, fix the
   `UserSelect`/`TextOverflow` string→enum breaks from preview.9, add
   `css.HotReload = true` in `AddBrowserApiCss`.

Spec touch-ups shipped with this plan: § Build Pipeline and §27 marked
superseded with pointers here; Authoring README gap list updated.
