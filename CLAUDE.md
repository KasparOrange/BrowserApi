# BrowserApi — AI Assistant Guide

## North Star: developer experience deluxe

This project optimizes for **transparency and readability** above almost everything else. A new contributor — human or agent — should be able to open any file and understand *what it does, why it was written that way, and where to look next* without needing to hunt for context.

Apply this to every change:

- **Code comments explain the *why*, never the *what*.** Identifiers already say what. Comments earn their place by preserving reasoning — a hidden constraint, a past incident, a specific invariant — that a future reader would otherwise have to rediscover.
- **Generated code is self-documenting.** When the source generator emits C#, the output carries a file-header comment pointing at the source `.ts`/`.d.ts`, class-level XML docs describing the shape, and per-property/per-parameter summaries pulled from the TypeScript JSDoc. Reading the generated `.g.cs` should feel the same as reading hand-written documented code.
- **Docs are updated in the same commit as the code.** Never ship a behavior change without touching the article that describes it. The relevant surfaces are:
  - `docs/docfx/articles/source-generator.md` — headline behavior for the SourceGen
  - `docs/docfx/articles/source-generator-support-matrix.md` — reference table of every supported / fallback / unsupported TypeScript construct
  - `docs/explanations/releasing.md` — the release workflow
  - `CHANGELOG.md` — user-visible behavior changes, framed as *"what changed and why you care"*
  - `CLAUDE.md` (this file) — any new principle or workflow that future agents need to know
- **After any doc change, spawn an agent to audit.** Send an Explore agent over the changed docs and ask it to flag inconsistencies, stale references, broken cross-links, or missing sections. Fix what it finds. The cost is a few minutes; the benefit is docs that stay trustworthy.
- **Good is better than exhaustive.** Don't pad. A clear sentence beats a clear paragraph. A well-structured table beats a long bulleted list. The goal is "a reader finds what they need in under a minute," not "every possible edge case is spelled out."

This principle trumps convenience. If shipping a fix faster would mean shipping it undocumented — don't ship faster.

## What This Project Is

BrowserApi is a **code generation** project that produces typed C# wrappers for browser APIs from W3C/WHATWG specifications. The core library has zero dependencies — it's pure types and string serialization. Separate packages add JS interop and Blazor integration.

Read the [README](README.md) for the full architecture and use cases.

## Key Architectural Decisions

### Package Separation

The split is by **dependency**, not by API:

| Package | Depends On | Contains |
|---------|-----------|----------|
| BrowserApi | Nothing | All types: CSS, DOM, Canvas, Fetch, Events, etc. |
| BrowserApi.JSInterop | Microsoft.JSInterop | `IJSRuntime` bridge layer |
| BrowserApi.Blazor | ASP.NET Components | DI, components, lifecycle |
| BrowserApi.Generator | (standalone tool) | WebIDL parser, C# emitter |

Do **not** split by API (e.g., "BrowserApi.Canvas" as a separate package). All types live in the core package, organized by namespace.

### Generated vs. Hand-Written

- **Generated from specs:** interfaces, classes, enums, records, property declarations, event definitions, method signatures
- **Hand-written:** fluent builders, operator overloads, extension methods, unit type factories (`Length.Rem()`, `Color.Hsl()`), the interop backend base class

Generated code goes in `src/BrowserApi/Generated/`. Hand-written code goes alongside it in the normal namespace folders. Both are `partial class` so they compose cleanly.

### CSS-in-C# Authoring (separate from the runtime CSSOM)

`src/BrowserApi/Css/Authoring/` is a hand-written API for *authoring static stylesheets* in C# — distinct from the runtime CSSOM types in `BrowserApi.Css` (which are generated from the CSS WebIDL for live `element.style` manipulation). Two `StyleSheet` types exist on purpose: the CSSOM one for runtime DOM access, the authoring one for static stylesheet declaration. Consumers alias to disambiguate.

Wire it up in three places: `AddBrowserApiCss()` in `Program.cs`, `<BrowserApiCss />` in `App.razor`'s `<HeadContent>`, and any number of `partial class … : StyleSheet` files declaring `static readonly Class`/`Rule`/`CssVar<T>`/`Keyframes` fields. The runtime renders all stylesheets to a single `<style>` tag at first access. Source generator path is decided in spec but not yet shipped — see `docs/plans/browser-api/css-in-csharp.md`.

Read `src/BrowserApi/Css/Authoring/README.md` before touching this code.

### Spec Sources

All specs come from [w3c/webref](https://github.com/w3c/webref):

- `specs/idl/` — WebIDL files (`.idl`), one per web spec (dom.idl, html.idl, fetch.idl, etc.)
- `specs/css/` — CSS property data (`.json`), one per CSS module (css-flexbox.json, css-grid.json, etc.)

These are checked into the repo. Update by re-downloading from webref when specs change.

## Working with the Generator

The generator (`src/BrowserApi.Generator/`) is a CLI tool:

```
Input:  specs/idl/*.idl + specs/css/*.json
Output: src/BrowserApi/Generated/**/*.cs
```

### WebIDL → C# Mapping Rules

| WebIDL | C# |
|--------|-----|
| `interface Foo : Bar` | `public partial class Foo : Bar` |
| `attribute DOMString name` | `public string Name { get; set; }` |
| `readonly attribute` | `get`-only property |
| `Promise<T> method()` | `Task<T> MethodAsync()` |
| `sequence<T>` | `T[]` or `IReadOnlyList<T>` |
| `T?` | `T?` (nullable) |
| `(A or B)` | method overloads (native `union` types when targeting .NET 11+) |
| `enum { "a", "b" }` | `enum` with `[StringValue]` attributes |
| `dictionary` | `record class` |
| `callback` | `delegate` / `Action<>` / `Func<>` |

### CSS Data → C# Mapping

CSS properties come from JSON files with `name`, `value` (grammar), `initial`, `inherited`, etc. The generator produces:

- A property on `CssStyleDeclaration` with the correct value type
- A value type struct implementing `ICssValue` with `ToCss()` → string

Ergonomic factory methods (`Color.Rgb()`, `Length.Calc()`) are hand-written on top.

## Testing Strategy

~80-90% of tests need **no browser**:

| Layer | Test Type | Needs Browser |
|-------|-----------|:---:|
| Generator (WebIDL → C#) | String in → string out unit tests | No |
| Type consistency | Compilation (if it builds, it's correct) | No |
| CSS serialization | `ToCss()` → string assertions | No |
| Fluent builders | Unit tests on output | No |
| Snapshot tests | Detect regeneration drift | No |
| Interop correctness | Headless browser (Playwright) | Yes |

Write generator tests as: given this WebIDL input, assert this C# output. Pure TDD.

Write CSS tests as: `Assert.Equal("1.5rem", Length.Rem(1.5).ToCss())`. Pure TDD.

## Code Style

- Opening brace on same line (K&R style)
- 4 spaces indentation
- `partial class` on all generated types (allows hand-written extensions)
- Generated files start with `// <auto-generated/>` comment
- PascalCase for C# (even when WebIDL uses camelCase)
- Nullable reference types enabled everywhere

## Plan Files

Plans follow the [large project plan structure](docs/plans/browser-api/browser-api.md). One master file orchestrates, one file per API concern. Check `docs/plans/browser-api/` before starting work on any API area.

## Common Tasks

### Update specs from webref

```bash
# Clone/pull webref, copy IDL, CSS, and pre-parsed IDL JSON files
git clone --depth 1 https://github.com/w3c/webref /tmp/webref
cp /tmp/webref/ed/idl/*.idl specs/idl/
cp /tmp/webref/ed/css/*.json specs/css/
cp /tmp/webref/ed/idlparsed/*.json specs/idlparsed/
```

### Regenerate types

```bash
dotnet run --project src/BrowserApi.Generator -- \
    --webidl specs/idl/ \
    --css-data specs/css/ \
    --output src/BrowserApi/Generated/
```

### Run tests

```bash
dotnet test               # all tests
dotnet test tests/BrowserApi.Tests/        # core type tests only
dotnet test tests/BrowserApi.Generator.Tests/  # generator tests only
```

### SourceGen: Local Development

MitWare.Blazor consumes `BrowserApi.SourceGen` as a local NuGet package. For local dev, pack and install from the local feed:

```bash
# Pack a new version
dotnet pack src/BrowserApi.SourceGen/BrowserApi.SourceGen.csproj -c Release \
    -o nupkgs -p:Version=0.1.0-local.X

# In MitWare.Blazor, update to the new version
dotnet add package BrowserApi.SourceGen --version 0.1.0-local.X
```

The local NuGet source at `nupkgs/` is already registered as `BrowserApiLocal`.

### SourceGen: Deploy to Production

When the source generator changes and MitWare needs the update on the server, run:

```bash
./scripts/publish-local.sh 0.1.0-local.X
```

This packs the `.nupkg` and copies it via `scp` to the MitWare server's `tools/nupkg/` directories (main + dev). The server's `NuGet.Config` already has `tools/nupkg/` as a local source — no server-side config needed.

Then update `MitWare.Blazor.csproj` to reference the new version and deploy MitWare normally.

Normal MitWare deploys do not touch the `tools/nupkg/` directory — only run this script when the source generator itself changes.

### Publishing a new NuGet release

Edit `CHANGELOG.md` (move entries from `[Unreleased]` to a new `## [<version>]` section), commit, then push a tag `v<version>`. The publish workflow does the rest — packing, pushing to nuget.org, and creating the GitHub Release with notes pulled from the CHANGELOG. Full flow and gotchas in [docs/explanations/releasing.md](docs/explanations/releasing.md).

## What NOT to Do

- Do not add API-specific NuGet packages (no BrowserApi.Canvas, etc.)
- Do not use source generators (Roslyn) — this is a standalone CLI tool
- Do not hand-write types that can be generated from specs
- Do not add Blazor/JSInterop dependencies to the core BrowserApi package
- Do not initialize properties to non-default values in generated types
