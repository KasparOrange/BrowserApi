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
- Optional `IJsModulePathResolver` for Vite/bundler integration
- Auto-generates `AddJsModules()` DI registration
- Zero runtime overhead — same `InvokeAsync` calls you'd write by hand

## Documentation

Full guide: [Source Generator article](https://kasparorange.github.io/BrowserApi/articles/source-generator.html)
