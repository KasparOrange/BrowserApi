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

## [0.1.0-preview.2] — 2026-04-21

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
