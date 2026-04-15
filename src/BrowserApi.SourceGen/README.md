# BrowserApi.SourceGen

> **Preview** — This package is under active development. We use it internally in production, but the API may change between versions. No stability guarantees.

Roslyn source generator that reads your `.js` or `.d.ts` modules at build time and emits typed C# wrapper classes for Blazor JS interop.

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

- Parses exported functions from `.js` (via JSDoc) or `.d.ts` files
- Generates typed async C# methods with XML docs
- TypeScript interfaces become C# records with `[JsonPropertyName]`
- String literal unions become enums with `[JsonStringEnumConverter]`
- Optional `IJsModulePathResolver` for Vite/bundler integration
- Auto-generates `AddJsModules()` DI registration
- Zero runtime overhead — same `InvokeAsync` calls you'd write by hand

## Documentation

Full guide: [Source Generator article](https://kasparorange.github.io/BrowserApi/articles/source-generator.html)
