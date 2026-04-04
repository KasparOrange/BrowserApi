# JS Module Source Generator

BrowserApi.SourceGen is a Roslyn source generator that reads your JavaScript or TypeScript modules at build time and emits typed C# wrapper classes. You get full IntelliSense, compile-time parameter checking, and XML docs — with zero runtime overhead.

## How It Works

```
Build time:
  your.js / your.d.ts  →  SourceGen  →  YourModule.g.cs (typed C# class)

Runtime:
  YourModule.MethodAsync()  →  IJSRuntime.InvokeAsync()  →  your.js
```

The generated code is identical to what you'd write by hand — thin `InvokeAsync` wrappers around `IJSObjectReference`. No reflection, no runtime parsing.

## Quick Start (Simple Setup)

For projects without a bundler (no Vite, no Webpack):

### 1. Add the package

```bash
dotnet add package BrowserApi.SourceGen
```

### 2. Register your JS files as AdditionalFiles

```xml
<!-- In your .csproj -->
<ItemGroup>
    <AdditionalFiles Include="wwwroot/js/**/*.js" />
</ItemGroup>
```

### 3. Register in DI

```csharp
// Program.cs
builder.Services.AddJsModules(); // auto-generated, registers all discovered modules
```

### 4. Use

```csharp
// Your JS file: wwwroot/js/utils.js
// export function formatCurrency(amount, currency) { ... }

// In a component — "UtilsModule" was auto-generated from "utils.js"
@inject UtilsModule Utils

var price = await Utils.FormatCurrencyAsync(42.99, "USD");
```

That's it. The class name is derived from the filename: `utils.js` → `UtilsModule`, `mw-dnd.js` → `MwDndModule`.

## TypeScript Support (.d.ts)

For much better type safety, use TypeScript declaration files. The generator parses them and produces:

| TypeScript | C# |
|---|---|
| `interface DragConfig { ... }` | `sealed class DragConfig` with `[JsonPropertyName]` |
| `'clone' \| 'template' \| 'none'` | `enum` with `[JsonStringEnumConverter]` |
| `Record<string, T>` | `Dictionary<string, T>` |
| `items: string[]` | `string[] Items` |
| `handle?: string` | `string? Handle` |
| `Promise<string>` | unwrapped to `string` (method returns `Task<string>`) |

### Example

**TypeScript declaration:**

```typescript
// wwwroot/js/src/mw-dnd.d.ts

export interface DragConfig {
    container: string;
    sources: string;
    handle?: string;
    threshold?: number;
    watch: string[];
    ghost?: GhostConfig;
}

export interface GhostConfig {
    mode: 'clone' | 'template' | 'label' | 'moveSource' | 'none';
    sourceClass?: string;
    offsetX?: number;
    offsetY?: number;
}

export function createDrag(dotNetRef: DotNetObjectReference, config: DragConfig): number;
export function destroyDrag(contextId: number): void;
export function dispose(): void;
export function addClassToMatching(selector: string, className: string): void;
```

**Generated C#:**

```csharp
// DragConfig.g.cs
public sealed class DragConfig {
    [JsonPropertyName("container")]
    public required string Container { get; init; }

    [JsonPropertyName("sources")]
    public required string Sources { get; init; }

    [JsonPropertyName("handle")]
    public string? Handle { get; init; }

    [JsonPropertyName("threshold")]
    public double? Threshold { get; init; }

    [JsonPropertyName("watch")]
    public required string[] Watch { get; init; }

    [JsonPropertyName("ghost")]
    public GhostConfig? Ghost { get; init; }
}

// GhostConfigMode.g.cs
[JsonConverter(typeof(JsonStringEnumConverter<GhostConfigMode>))]
public enum GhostConfigMode {
    [JsonStringEnumMemberName("clone")]
    Clone,
    [JsonStringEnumMemberName("template")]
    Template,
    [JsonStringEnumMemberName("label")]
    Label,
    [JsonStringEnumMemberName("moveSource")]
    MoveSource,
    [JsonStringEnumMemberName("none")]
    None
}

// MwDndModule.g.cs
public partial class MwDndModule : IAsyncDisposable {
    public MwDndModule(IJSRuntime js, IJsModulePathResolver? pathResolver = null);

    public async Task<double> CreateDragAsync(object dotNetRef, DragConfig config) { ... }
    public async Task DestroyDragAsync(double contextId) { ... }
    public async Task DisposeModuleAsync() { ... }
    public async Task AddClassToMatchingAsync(string selector, string className) { ... }

    public async ValueTask DisposeAsync() { ... }
}
```

### Setup with .d.ts

```xml
<!-- In your .csproj -->
<ItemGroup>
    <AdditionalFiles Include="wwwroot/js/src/*.d.ts" />
</ItemGroup>
```

If a `.d.ts` file exists for a module, the generator uses it for type information. If not, it falls back to JSDoc parsing from the `.js` file. Both can coexist — you can migrate one module at a time.

## Custom Class Names ([JsModule] Attribute)

By default, the class name comes from the filename. To choose your own:

```csharp
[JsModule("./js/src/mw-dnd.js")]
public partial class DragDropService;
```

Now the generated class is `DragDropService` instead of `MwDndModule`. The attribute is optional — most projects don't need it.

## Path Resolver (Vite / Bundler Integration)

### The Problem

Build tools like Vite produce content-hashed filenames for cache busting:

```
wwwroot/js/src/mw-dnd.js  →  /js/dist/mw-dnd.a1b2c3d4.mjs
```

The generated code needs to `import()` the hashed path, not the source path.

### The Solution: IJsModulePathResolver

The generator emits an `IJsModulePathResolver` interface. Implement it to hook into your build tool's manifest:

```csharp
// Implement the interface (wraps your existing path service)
public class VitePathResolver : IJsModulePathResolver {
    private readonly JSInteropPathService _pathService;

    public VitePathResolver(JSInteropPathService pathService)
        => _pathService = pathService;

    public string Resolve(string moduleName)
        => _pathService.GetScriptPath(moduleName);
}
```

Register it in DI:

```csharp
// Program.cs
builder.Services.AddSingleton<IJsModulePathResolver, VitePathResolver>();
builder.Services.AddJsModules();
```

Now every generated module class automatically resolves `"mw-dnd"` → `"/js/dist/mw-dnd.a1b2c3d4.mjs"` via your Vite manifest. No per-module configuration needed.

### Without a Path Resolver

If you don't register an `IJsModulePathResolver`, the generated code uses the raw file path from the `AdditionalFiles` entry. This works fine for development or projects without a bundler.

### How It Works Internally

The generated constructor accepts the resolver as an optional parameter:

```csharp
public MwDndModule(IJSRuntime js, IJsModulePathResolver? pathResolver = null) {
    _js = js;
    _modulePath = pathResolver?.Resolve("mw-dnd") ?? "./js/src/mw-dnd.js";
}
```

DI injects it if registered, otherwise the fallback path is used.

## Module Loading

All generated modules use lazy loading via ES `import()`:

```csharp
private async Task<IJSObjectReference> GetModuleAsync() {
    return _module ??= await _js.InvokeAsync<IJSObjectReference>("import", _modulePath);
}
```

The module JavaScript is fetched **only when the first method is called**, not at startup. This is the recommended pattern for Blazor — heavy JS modules don't block the initial page load.

## Enum Serialization

String literal unions in TypeScript become C# enums with proper JSON serialization:

```typescript
mode: 'clone' | 'template' | 'none'
```

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<GhostConfigMode>))]
public enum GhostConfigMode {
    [JsonStringEnumMemberName("clone")]
    Clone,
    [JsonStringEnumMemberName("template")]
    Template,
    [JsonStringEnumMemberName("none")]
    None
}
```

`GhostConfigMode.Clone` serializes to `"clone"` in JSON — matching what the JavaScript expects. No custom converters or naming policies needed.

## JSDoc Support

For plain `.js` files (no `.d.ts`), the generator reads JSDoc comments:

```javascript
/**
 * Formats a number as currency.
 * @param {number} amount - The amount to format.
 * @param {string} currency - ISO 4217 currency code.
 * @returns {string} The formatted string.
 */
export function formatCurrency(amount, currency) { ... }
```

This produces typed parameters (`double amount, string currency`) and XML doc comments. The type mapping:

| JSDoc | C# |
|---|---|
| `{number}` | `double` |
| `{string}` | `string` |
| `{boolean}` | `bool` |
| `{void}` | `Task` (no return) |
| `{Promise<T>}` | unwrapped to `T` |
| `{Array<T>}` or `{T[]}` | `T[]` |
| `{any}` / `{object}` | `object` |
| (missing) | `object` (fallback) |

## Comparison: Simple vs Production Setup

| | Simple | Production (Vite + TypeScript) |
|---|---|---|
| **csproj** | `<AdditionalFiles Include="**/*.js" />` | `<AdditionalFiles Include="**/*.d.ts" />` |
| **Program.cs** | `AddJsModules()` | `AddSingleton<IJsModulePathResolver, ViteResolver>()` + `AddJsModules()` |
| **Type safety** | Basic (JSDoc types, unknown → `object`) | Full (TS interfaces → records, unions → enums) |
| **Import paths** | Raw file paths | Hashed via manifest |
| **Cache busting** | No | Yes |
| **Config** | 2 lines | ~15 lines (one-time) |
| **Runtime cost** | Same | Same |

## Limitations

- Only `export function` declarations are processed. Default exports, arrow function exports, and class method exports are not supported.
- `.d.ts` parsing uses regex, not a full TypeScript parser. Complex generic types, conditional types, and mapped types are not supported.
- Intersection types (`A & B`) are not supported.
- The path resolver requires a DI container. If you're not using DI, construct the module manually: `new MyModule(jsRuntime)`.
