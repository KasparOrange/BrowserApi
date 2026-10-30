# BrowserApi.SourceGen

> **Preview** — This package is under active development. We use it internally in production, but the API may change between versions. No stability guarantees.

Roslyn source generator that reads your `.ts`, `.d.ts`, or `.js` modules at build time and emits typed C# wrapper classes for Blazor JS interop.

## Quick Start

```xml
<!-- .csproj -->
<ItemGroup>
    <AdditionalFiles Include="wwwroot/js/**/*.js" />
</ItemGroup>
```

```csharp
// Program.cs
builder.Services.AddJsModules();
```

```razor
@inject UtilsModule Utils

var result = await Utils.FormatCurrencyAsync(42.99, "USD");
```

## What It Does

- Parses exported functions from `.ts` and `.d.ts` (typed) or `.js` (JSDoc-only)
- Generates typed async C# methods with XML docs flowed from TypeScript JSDoc
- TypeScript interfaces become C# records with `[JsonPropertyName]`
- String literal unions become enums with `[JsonStringEnumConverter]`
- `DotNetObjectReference` parameters become generic methods with type inference
- **Ambient TypeScript declarations** ship with the package: numeric width aliases (`int`, `long`, `float`, `uint`, `byte`, `Guid`, …) and a typed `DotNetObjectReference` with `invokeMethodAsync<TResult>` and `dispose()`. Add `obj/browserapi-types/**/*.d.ts` to your `tsconfig.json` `include` to pick them up.
- Optional `IJsModulePathResolver` for Vite/bundler integration (the interface ships in `BrowserApi.JSInterop`; reference that package to use it)
- Auto-generates DI registration: `AddJsModules()` inside the assembly, `Add{AssemblyName}JsModules()` from any other
- `HTMLElement` and friends as function parameters become `ElementReference`; `T | null` becomes `T?`
- Zero runtime overhead — same `InvokeAsync` calls you'd write by hand

## Using it in a library and an app

Both may run the generator. Each assembly gets its own registration, and all of them share one `IJsModulePathResolver`:

```csharp
// Host Program.cs. AddJsModules() is internal, so it only ever means this assembly's modules.
builder.Services.AddJsModules();
builder.Services.AddMyLibraryJsModules(); // generated public, named after the assembly
```

Two optional properties in the library's `.csproj`:

| Property | Default | Use it when |
|---|---|---|
| `<BrowserApiSourceGenNamespace>` | `JsModules` | The app has a module or interface of the same name, which would otherwise collide (CS0436). |
| `<BrowserApiSourceGenModuleBasePath>` | Derived from `wwwroot/` | The bundles are served elsewhere, like `./_content/MyLibrary/js` in a Razor class library (write the package id out, `$(PackageId)` is still empty there). |

A resolver registered by the app also receives library modules. Override `Resolve(moduleName, assemblyName)` and return `null` to leave a module on its generated default path.

## Documentation

Full guide: [Source Generator article](https://kasparorange.github.io/BrowserApi/articles/source-generator.html)
