# BrowserApi

Typed C# wrappers for browser APIs, generated from W3C/WHATWG specs.

Turn magic strings into compile-time errors. Get IntelliSense for every CSS property, DOM method, and browser API — without leaving C#.

## Why

Every C# developer working with browser APIs writes code like this:

```csharp
await js.InvokeVoidAsync("setStyle", element, "background-color", "rgba(255, 0, 0, 0.5)");
await js.InvokeVoidAsync("setStyle", element, "display", "flx"); // typo — silent failure
```

BrowserApi makes it this:

```csharp
element.Style.BackgroundColor = Color.Rgba(255, 0, 0, 0.5f);
element.Style.Display = Display.Flex; // enum — typo is a compile error
element.Style.Gap = Length.Rem(1.5);
```

The types are generated directly from the same [WebIDL specs and CSS data](https://github.com/w3c/webref) that browsers implement against.

## Packages

| Package | Dependencies | Purpose |
|---------|-------------|---------|
| **BrowserApi** | None | Core types: CSS values, DOM interfaces, enums, records. Pure C# — no browser needed. |
| **BrowserApi.JSInterop** | Microsoft.JSInterop | Bridges typed APIs to a live browser via `IJSRuntime`. |
| **BrowserApi.Blazor** | ASP.NET Core Components | Blazor-specific: DI registration, component base classes, lifecycle hooks. |
| **BrowserApi.Generator** | (CLI tool) | Reads WebIDL + CSS specs, emits C# source files. |

## Use Cases

### BrowserApi (core) — no browser, no framework

The core package has **zero dependencies**. It's a typed vocabulary for web concepts.

- **Server-side CSS generation** — build stylesheets in C# with compile-time safety
- **Email templates** — type-safe inline styles (no more Outlook surprises from CSS typos)
- **Design tokens** — define a design system in C#, export to CSS variables, Tailwind config, etc.
- **Static site generators** — typed HTML/CSS output from C# build tools
- **Test assertions** — assert that components produce correct CSS/HTML
- **PDF styling** — reuse the same styling vocabulary across web and PDF output

### BrowserApi.JSInterop — anything with IJSRuntime

Works with any framework that provides `IJSRuntime`:

- **Blazor Server** — SignalR-based interop
- **Blazor WebAssembly** — in-process interop
- **MAUI Blazor Hybrid** — mobile/desktop apps with web UI
- **Custom hosts** — anything implementing `IJSRuntime` (Electron, WebView2, CEF)

### BrowserApi.Blazor — Blazor integration

- `services.AddBrowserApi()` DI registration
- Component base classes with typed DOM access
- Lifecycle-aware browser API wrappers

## Architecture

```
Your C# code
    |
BrowserApi (pure types, zero deps)
    |
    +--- used directly for CSS/HTML generation, testing, design tokens
    |
BrowserApi.JSInterop (Microsoft.JSInterop)
    |
    +--- bridges types to live browser via IJSRuntime
    |
BrowserApi.Blazor (ASP.NET Core Components)
    |
    +--- Blazor DI, components, lifecycle
```

The interop backend is deliberately separated from the types. Today it uses `IJSRuntime`. If WebAssembly ever gets direct browser API access via the [Component Model](https://component-model.bytecodealliance.org/), a native backend can be swapped in without changing consumer code.

## Code Generation

Types are generated from official specs, not hand-written:

- **WebIDL specs** from [w3c/webref](https://github.com/w3c/webref) (337 `.idl` files)
- **CSS property data** from [w3c/webref](https://github.com/w3c/webref) (124 CSS JSON files)

The generator (`BrowserApi.Generator`) reads these specs and emits C# files. Generated code is checked in, not generated at build time — this gives full IDE support and makes changes reviewable.

```
WebIDL specs + CSS data  -->  BrowserApi.Generator  -->  src/BrowserApi/Generated/
```

## Project Structure

```
BrowserApi/
├── src/
│   ├── BrowserApi/                 # Core types (zero dependencies)
│   │   ├── Css/                    # CSS value types, properties, selectors
│   │   ├── Dom/                    # DOM interfaces, elements, nodes
│   │   ├── Canvas/                 # Canvas 2D context types
│   │   ├── Fetch/                  # Fetch, Request, Response, Headers
│   │   ├── Storage/                # localStorage, sessionStorage
│   │   ├── Events/                 # Event types (Pointer, Keyboard, etc.)
│   │   ├── Animations/             # Web Animations API types
│   │   └── Common/                 # Shared primitives (unions, callbacks)
│   ├── BrowserApi.JSInterop/       # IJSRuntime bridge
│   ├── BrowserApi.Blazor/          # Blazor integration
│   └── BrowserApi.Generator/       # WebIDL/CSS → C# code generator
├── tests/
│   ├── BrowserApi.Tests/           # Pure unit tests (TDD, no browser)
│   ├── BrowserApi.Generator.Tests/ # Generator input→output tests
│   └── BrowserApi.BrowserTests/    # Integration tests (headless browser)
├── specs/                          # W3C/WHATWG spec files (input to generator)
│   ├── idl/                        # WebIDL files (.idl)
│   └── css/                        # CSS property data (.json)
└── docs/plans/                     # Design documents and implementation plans
```

## Status

Early stage. The project structure, spec sources, and design plans are in place. No generated code yet.

## License

TBD
