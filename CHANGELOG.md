# Changelog

All notable changes to BrowserApi packages are documented here.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) · Versioning: [SemVer](https://semver.org/spec/v2.0.0.html)

Packages published from this repo:

- `BrowserApi` — typed browser API wrappers
- `BrowserApi.JSInterop` — `IJSRuntime` bridge
- `BrowserApi.Blazor` — Blazor DI + components
- `BrowserApi.Runtime` — Jint-based server runtime
- `BrowserApi.SourceGen` — Roslyn source generator for `.js` / `.d.ts` modules

Entries are grouped by package. When an entry applies to a single package, the package name is in the heading.

## [Unreleased]

## [0.1.0-preview.6] — 2026-04-22

### BrowserApi.SourceGen

- **Added — JSDoc flows through to C# XML `<summary>`.** The parser now extracts JSDoc comments that sit above interface declarations and above interface properties, and the generator emits them as real XML documentation. Hover `config.Container` in your Blazor component and IntelliSense shows the TypeScript author's description — one place to write documentation, three places it shows up (TS editor, C# IntelliSense, docfx site). When no JSDoc is present, the generator falls back to a descriptive boilerplate (`Maps to TypeScript property <c>foo</c>`) so consumers never see an empty `<summary>`.
- **Added — generated-file headers tell you where the file came from.** Every `.g.cs` now starts with a block naming the originating `.d.ts` / `.ts` source and noting that the file is regenerated on each build. Opening an unfamiliar generated file in the IDE immediately answers "what is this, and where do I edit it?"
- **Added — richer module-class XML remarks.** The generated module class now documents that it's lazily imported on first call and explains the DI registration pattern, not just the one-liner.
- **Added — per-enum-member XML summaries.** Each enum member now has `<summary>Serializes to the TypeScript literal "x".</summary>`, making the JSON contract visible at the member level.
- **Changed — `DotNetObjectReference<T>` is now emitted unqualified.** `using Microsoft.JSInterop;` is already in every generated module file, and Path C's skip-list guarantees no colliding `JsModules.DotNetObjectReference` class exists. The fully-qualified form from preview.5 was defensive over-engineering; the unqualified form is cleaner and works identically.

### BrowserApi, BrowserApi.JSInterop, BrowserApi.Blazor, BrowserApi.Runtime

- No behavioral changes. Republished at the shared version so all packages stay version-aligned.

## [0.1.0-preview.5] — 2026-04-22

### BrowserApi.SourceGen

- **Changed — `DotNetObjectReference` parameters are now strongly typed via generic methods.** Preview.4 tried to map them to `Microsoft.JSInterop.DotNetObjectReference`, but that's a static factory class and can't be used as a parameter type (CS0721). This release solves the same problem the right way: when a function parameter is a `DotNetObjectReference` (with or without a type argument), the generator promotes the whole method to generic over a fresh `TDotNetRef` type parameter and emits `Microsoft.JSInterop.DotNetObjectReference<TDotNetRef>` as the parameter type, with `where TDotNetRef : class` and the `[DynamicallyAccessedMembers(PublicMethods)]` annotation for AOT / trim safety. Consumers pass their `DotNetObjectReference.Create(x)` directly and C# infers `TDotNetRef` at the call site — no configuration, no wrappers, no `object` on the C# side.
- **Fixed — the CS0721 regression from preview.4** is gone as a consequence of the change above. Consumers upgrading from preview.3 or preview.4 get real typing at the call site without changing their `.d.ts` files.
- **Added — compile-the-output integration test.** A new driver test runs the generator, takes its syntax trees, and compiles them against the real `Microsoft.JSInterop` and `Microsoft.Extensions.DependencyInjection.Abstractions` references a Blazor consumer would use. String-based "does the output contain X?" tests can't detect CS0721-class errors or missing references; this test can. Would have blocked preview.4 before it shipped.
- **Added — consumer-side call test.** Parses a realistic C# snippet that calls the generated generic method with a `DotNetObjectReference<SomeClass>` and compiles it together with the generator output. Verifies that C# type inference actually resolves `TDotNetRef` from the argument — not just that the method signature looks valid.
- **Added — source-generator support matrix doc.** [New article](docs/docfx/articles/source-generator-support-matrix.md) lists every supported TypeScript pattern, every fallback, and every unsupported feature with a prose explanation of why each behaves the way it does. The main source-generator article now points at the matrix instead of duplicating the details.
- **Process — local-feed-first is now the default for SourceGen output-shape changes.** `docs/explanations/releasing.md` was updated so any change that alters emitted C# shape must be validated in MitWare via the local feed before a public nuget.org release. Non-output-shape changes can still go straight to public.

### BrowserApi, BrowserApi.JSInterop, BrowserApi.Blazor, BrowserApi.Runtime

- No behavioral changes. Republished at the shared version so all packages stay version-aligned.

## [0.1.0-preview.4] — 2026-04-22

### BrowserApi.SourceGen

- **Fixed — stub `interface DotNetObjectReference {}` declarations in `.d.ts` files no longer produce a colliding C# class.** Preview.3's broadened interface handling emitted `JsModules.DotNetObjectReference` for these stubs, which collided with `Microsoft.JSInterop.DotNetObjectReference` at any consumer call site that had both namespaces in scope (CS0104 ambiguous reference). The generator now recognizes `DotNetObjectReference` as a Blazor interop primitive and skips the declaration entirely — no TypeMap entry, no emitted class.
- **Changed — `DotNetObjectReference` parameters are now strongly typed.** Previously these mapped to C# `object` in generated method signatures; they now map to `Microsoft.JSInterop.DotNetObjectReference` (the non-generic abstract base, which accepts any `DotNetObjectReference<T>` the caller creates). Callers get IntelliSense and compile-time safety at the call site without needing to pass a generic type parameter the `.d.ts` can't resolve.

### BrowserApi, BrowserApi.JSInterop, BrowserApi.Blazor, BrowserApi.Runtime

- No behavioral changes. Republished at the shared version so all packages stay version-aligned.

## [0.1.0-preview.3] — 2026-04-21

> Skipped `0.1.0-preview.2` for this set of changes — a `v0.1.0-preview.2` tag already pointed at an earlier commit that had been published via the old release-triggered workflow. This release is the first one cut via the new tag-driven pipeline that reads notes from `CHANGELOG.md`.

### BrowserApi.SourceGen

- **Added — non-exported interfaces are now registered and emitted as typed records.** A declaration like `interface CacheEntry { ... }` (no `export`) referenced by a public signature previously fell back to `object`; it is now emitted as a C# record the same way `export interface` is. The TS `export` keyword controls module `import` visibility, not JSON shape, so private helper interfaces map to valid records.
- **Added — `BAPI002` diagnostic.** When the parser encounters a TS type it cannot map to a C# type (complex generics, intersection types `A & B`, unresolved cross-file references, or typos), the source generator now emits a compiler warning identifying the location (`funcName(paramName)`, `funcName return type`, or `Interface.property`) and the unmapped type. Previously these silently degraded to `object`. Intentional mappings — `any`, `null`, and `DotNetObjectReference` — are **not** reported.

### BrowserApi, BrowserApi.JSInterop, BrowserApi.Blazor, BrowserApi.Runtime

- No behavioral changes. Republished at the shared version so all packages stay version-aligned.

## [0.1.0-preview.1] — BrowserApi.SourceGen

Initial preview release.

- Parses `.js` (JSDoc) and `.d.ts` modules as `AdditionalFiles` and emits typed C# wrapper classes implementing `IAsyncDisposable`.
- TypeScript interfaces → sealed C# classes with `[JsonPropertyName]`.
- String literal unions → enums with `[JsonStringEnumConverter]` / `[JsonStringEnumMemberName]`.
- `Record<string, T>` → `Dictionary<string, T>`; `T[]`, `Array<T>`, `Promise<T>` supported.
- Auto-generates `IServiceCollection.AddJsModules()` DI registration.
- Optional `IJsModulePathResolver` for Vite / bundler hashed-path integration.
- `[JsModule]` attribute for custom wrapper class names.
