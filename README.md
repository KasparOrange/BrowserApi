# BrowserApi

**Typed C# wrappers for every browser API — generated from W3C/WHATWG specs.**

[![CI](https://github.com/KasparOrange/BrowserApi/actions/workflows/ci.yml/badge.svg)](https://github.com/KasparOrange/BrowserApi/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/KasparOrange/BrowserApi/graph/badge.svg)](https://codecov.io/gh/KasparOrange/BrowserApi)
[![NuGet](https://img.shields.io/nuget/v/BrowserApi?logo=nuget&label=BrowserApi)](https://www.nuget.org/packages/BrowserApi)
[![NuGet](https://img.shields.io/nuget/v/BrowserApi.Blazor?logo=nuget&label=Blazor)](https://www.nuget.org/packages/BrowserApi.Blazor)
[![NuGet](https://img.shields.io/nuget/v/BrowserApi.SourceGen?logo=nuget&label=SourceGen)](https://www.nuget.org/packages/BrowserApi.SourceGen)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-946%20passing-brightgreen)]()
[![GitHub stars](https://img.shields.io/github/stars/KasparOrange/BrowserApi?style=flat&logo=github)](https://github.com/KasparOrange/BrowserApi/stargazers)
[![Last commit](https://img.shields.io/github/last-commit/KasparOrange/BrowserApi)](https://github.com/KasparOrange/BrowserApi/commits/main)
[![API Docs](https://img.shields.io/badge/docs-kasparorange.github.io-blue?logo=github)](https://kasparorange.github.io/BrowserApi/)

> Turn magic strings and untyped `IJSRuntime` calls into compile-time-checked, IntelliSense-rich C# code — without writing a single line of JavaScript.

> [!IMPORTANT]
> **Performance: understand the interop cost before using this library.**
>
> Every property access and method call on a BrowserApi type is a JavaScript interop call under the hood. Each call has overhead from JSON serialization and the WASM/JS boundary crossing. In **Blazor Server**, each call is also a **network roundtrip over SignalR** — meaning latency depends on your connection (typically 10–100ms per call).
>
> **What this means in practice:**
> - Setting 10 element properties = 10 interop calls. This is fine for occasional DOM updates, but not for tight loops or animation frames.
> - Synchronous calls (`IJSInProcessRuntime`) are [~4x faster than async calls](https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm) in WASM, but unavailable on Server.
> - Microsoft's [official guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/javascript-interoperability): *"Avoid excessively fine-grained calls"* and *"roll individual JS interop calls into a single call."*
>
> **BrowserApi provides two solutions for high-performance scenarios:**
> 1. **[`JsBatch`](https://kasparorange.github.io/BrowserApi/articles/performance.html)** — batches N void operations (property sets, method calls) into a single interop call.
> 2. **[`QueryValuesAsync` / `QueryElementsAsync`](https://kasparorange.github.io/BrowserApi/articles/performance.html)** — bulk-reads DOM data in one call, then use LINQ in pure C# (zero interop), then write back with `JsBatch` (one call). Total: 2 interop calls instead of N+N.
> 3. **[`BrowserApi.SourceGen`](https://kasparorange.github.io/BrowserApi/articles/source-generator.html)** — Roslyn source generator that reads your `.ts`, `.d.ts`, or `.js` modules and emits typed C# classes with DI injection, path resolver support, and enum serialization. Same `InvokeAsync` cost as hand-written interop, but with compile-time safety and IntelliSense.
>
> **Bottom line:** Use the typed API freely for event handlers, user interactions, and occasional DOM reads/writes. For bulk operations, use batching. For performance-critical rendering (Canvas animations, real-time updates), consider keeping that logic in a JS module and calling it via `BrowserApi.SourceGen`.

---

## The Problem

Every Blazor developer writes code like this:

```csharp
// Untyped, fragile, no IntelliSense
await js.InvokeVoidAsync("eval", "document.getElementById('hero').style.display = 'flx'");
// "flx" is a typo — but it compiles and silently does nothing.
```

## The Solution

```csharp
// Typed, safe, full IntelliSense
var hero = Document.QuerySelector<HtmlDivElement>("#hero")!;
hero.Style.Display = Display.Flex;    // enum — typo is a compile error
hero.Style.Gap = Length.Rem(1.5);     // strongly-typed CSS value
hero.FadeIn(500);                     // Web Animations API
```

2,693 types generated directly from the same [WebIDL specs](https://github.com/w3c/webref) that browsers implement against. Every property, method, and event — typed.

---

## Quick Start

```csharp
@inherits BrowserApiComponentBase

<h1>@_title</h1>

@code {
    private string _title = "Loading...";

    protected override async Task OnBrowserApiReadyAsync() {
        // DOM
        _title = Document.Title;
        var input = Document.QuerySelector<HtmlInputElement>("#email")!;

        // Events — typed, disposable
        input.OnInput(e => _title = input.Value);

        // Fetch
        var users = await Http.GetAsync<List<User>>("/api/users");

        // Canvas
        var ctx = Document.QuerySelector<HtmlCanvasElement>("canvas")!.GetContext2D();
        ctx.SetFill(CssColor.Red).FillRect(0, 0, 200, 100);
        ctx.Path().MoveTo(10, 10).LineTo(190, 90).Stroke();

        // Storage
        var storage = Window.TypedLocalStorage();
        storage.Set("users", users);

        // Animations
        input.FadeIn(500);

        StateHasChanged();
    }
}
```

**Setup** (two lines):

```csharp
// Program.cs
builder.Services.AddBrowserApi();
```

```html
<!-- index.html -->
<script src="_content/BrowserApi.JSInterop/browserapi.js"></script>
```

---

## Packages

| Package | Dependencies | What it does |
|---------|:---:|---|
| **BrowserApi** | None | 2,693 generated types: DOM, CSS, Canvas, Fetch, Storage, Events, Animations. Pure C#. |
| **BrowserApi.JSInterop** | Microsoft.JSInterop | `IJSRuntime` bridge — connects types to a live browser. |
| **BrowserApi.Blazor** | ASP.NET Components | `AddBrowserApi()` DI, `BrowserApiComponentBase`, lifecycle hooks. |
| **BrowserApi.Runtime** | Jint | Server-side JS execution — test DOM interactions without a browser. |
| **BrowserApi.SourceGen** | (build-time only) | Roslyn source generator: `.ts`/`.d.ts`/`.js` → typed C# module classes with DI, path resolver, and enum serialization. [Guide](https://kasparorange.github.io/BrowserApi/articles/source-generator.html) |
| **BrowserApi.Css.SourceGen** | (build-time only) | Roslyn source generator + analyzers for the CSS-in-C# authoring API: pre-populates class/variable/keyframe names, generates typed `Assets.*` from `wwwroot/`, parses third-party CSS into `Mud.*`-style typed surfaces, and ships BCA001/BCA002/BCA003 diagnostics. Analyzer-only — ships nothing at runtime. [Guide](https://kasparorange.github.io/BrowserApi/articles/css-in-csharp.html) |

---

## Features

### Typed DOM — no casts, no magic strings

```csharp
var input = Document.QuerySelector<HtmlInputElement>("#email")!;
var div = Document.CreateElement<HtmlDivElement>();
div.TextContent = "Created from C#";
Document.QuerySelector<Element>("body")!.AppendChild(div);
```

### Typed Events — not `addEventListener("clck", ...)`

```csharp
using var sub = button.OnClick(e => {
    Console.WriteLine($"Clicked at ({e.ClientX}, {e.ClientY})");
});

input.OnKeyDown(e => {
    if (e.Key == "Enter") Submit();
});
```

### CSS Value Types — not `"1.5rem"` strings

```csharp
element.Style.Margin = 1.5.Rem;
element.Style.Color = CssColor.Hsl(220, 90, 56);
element.Style.Transform = Transform.Rotate(45.Deg).Scale(1.2);
element.Style.Transition = Transition.All(300.Ms, Easing.EaseInOut);
```

### CSS-in-C# Authoring — not separate `.css` files

```csharp
public class AppStyles : StyleSheet {
    public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#0066cc"));
    public static readonly Class Btn = new() {
        Display      = Display.InlineFlex,
        Padding      = (8.Px, 16.Px),
        Background   = Primary,
        BorderRadius = 8.Px,
        [Self.Hover] = new() { Background = ((CssColor)Primary).Darken(8) },
    };
}
```
```razor
@* App.razor — emits combined CSS for every StyleSheet in the AppDomain *@
<HeadContent><BrowserApiCss /></HeadContent>

@* anywhere *@
<button class="@AppStyles.Btn">Click me</button>
```

Typed selectors, typed values, BEM modifiers via `.Variant`, `:hover`/
`:focus`/etc. as nested blocks, full `@media`/`@supports`/`@container`/
`@keyframes` support, design-token variables via `CssVar<T>`. See the
[CSS-in-C# guide](https://kasparorange.github.io/BrowserApi/articles/css-in-csharp.html).

### Fluent Fetch — not `IJSRuntime.InvokeAsync("fetch", ...)`

```csharp
var user = await Http.GetAsync<User>("/api/users/42");

var created = await Http.Post("/api/users")
    .WithJsonBody(new { Name = "Alice" })
    .WithHeader("Authorization", "Bearer token")
    .SendJsonAsync<User>();

// Non-throwing pattern
var result = await Http.Get("/api/data").TrySendAsync<Data>();
if (result.IsSuccess) Use(result.Value!);
```

### Canvas 2D — fluent paths, typed fills, save/restore scoping

```csharp
var ctx = canvas.GetContext2D();

using (ctx.SaveState()) {
    ctx.SetFill(CssColor.Rgb(255, 100, 0))
       .SetShadow(CssColor.Black, blur: 10, offsetX: 3, offsetY: 3);

    ctx.Path()
       .MoveTo(50, 50).LineTo(200, 50).LineTo(125, 150)
       .ClosePath().Fill();
}

var gradient = ctx.LinearGradient(0, 0, 300, 0)
    .AddStop(0, CssColor.Red)
    .AddStop(1, CssColor.Blue)
    .Build();

ctx.Font = CanvasFont.Of(24, "Inter").Bold();
```

### Web Animations — not `element.animate({...}, {...})`

```csharp
element.FadeIn(500);
element.SlideIn(300, "right");

element.Animate(
    new KeyframeBuilder()
        .AddFrame(new { transform = "rotate(0deg)" })
        .AddFrame(new { transform = "rotate(360deg)" }),
    new AnimationOptionsBuilder()
        .Duration(1000)
        .Easing(Easing.EaseInOutCubic)
        .Iterations(double.PositiveInfinity));
```

### Performance — batching & bulk queries

```csharp
// Write: N operations → 1 interop call
await JsBatch.RunAsync(batch => {
    batch.SetProperty(el1, "textContent", "hello");
    batch.SetProperty(el2, "className", "active");
    batch.InvokeVoid(ctx, "fillRect", 0, 0, 100, 100);
});

// Read: fetch all data in 1 call → LINQ in C# → batch write back
var texts = await document.QueryValuesAsync<string>("li", "textContent");
var sorted = texts.Where(t => t.Length > 3).OrderBy(t => t).ToList();
```

### Server-Side Testing — no browser needed

```csharp
var engine = new BrowserEngine();  // Jint + virtual DOM

engine.Execute(@"
    var card = document.createElement('div');
    card.className = 'card';
    card.style.display = 'flex';
    document.body.appendChild(card);
");

var card = engine.VirtualDocument.QuerySelector(".card");
Assert.Equal("flex", card!.Style["display"]);
Assert.Contains("card", engine.VirtualDocument.Body.OuterHtml);
```

---

## Architecture

```
Your C# code
    │
    ▼
┌─────────────────────────────────────────────┐
│  BrowserApi  (zero dependencies)            │
│  2,693 types: DOM, CSS, Canvas, Fetch, ...  │
│  Generated from W3C WebIDL + CSS specs      │
└───────────────┬─────────────┬───────────────┘
                │             │
    ┌───────────▼──┐   ┌──────▼────────────┐
    │  JSInterop   │   │  Runtime (Jint)   │
    │  Backend     │   │  Backend          │
    │  (browser)   │   │  (virtual DOM)    │
    └───────┬──────┘   └──────────────────┘
            │
    ┌───────▼──────┐
    │  Blazor      │
    │  Integration │
    └──────────────┘
```

The `IBrowserBackend` abstraction separates types from transport. Today: `IJSRuntime` (Blazor) and Jint (server-side). Future: native WASM Component Model imports.

---

## Code Generation

Types are generated from official W3C/WHATWG specs — not hand-written:

```
337 WebIDL specs + 124 CSS data files
          │
    BrowserApi.Generator (CLI)
          │
    2,693 generated .g.cs files
```

Generated code is checked in for full IDE support and reviewable diffs.

**Hand-written ergonomic layers** (fluent builders, operators, factory methods) extend the generated `partial` types without modifying them.

---

## Project Structure

```
BrowserApi/
├── src/
│   ├── BrowserApi/                 # Core types (zero deps)
│   │   ├── Common/                 # JsObject, IBrowserBackend, JsBatch
│   │   ├── Css/                    # Length, CssColor, Transform, Shadow, ...
│   │   ├── Dom/                    # QuerySelector<T>, EventExtensions, ...
│   │   ├── Canvas/                 # PathBuilder, GradientBuilder, CanvasFont
│   │   ├── Fetch/                  # Http, RequestBuilder, FetchResult
│   │   ├── Storage/                # TypedStorage, StorageExtensions
│   │   ├── Events/                 # Key, Modifiers, typed event extensions
│   │   ├── Animations/             # Easing, KeyframeBuilder, AnimateExtensions
│   │   └── Generated/              # 2,693 auto-generated .g.cs files
│   ├── BrowserApi.JSInterop/       # IJSRuntime backend + browserapi.js
│   ├── BrowserApi.Blazor/          # DI + BrowserApiComponentBase
│   ├── BrowserApi.Runtime/         # Jint + VirtualDom + BrowserEngine
│   └── BrowserApi.Generator/       # WebIDL/CSS → C# code generator
├── tests/                          # 599 tests, no browser needed
├── specs/                          # W3C/WHATWG spec files (generator input)
└── docs/                           # docfx API documentation
```

---

## Documentation

Full API reference with examples: **[kasparorange.github.io/BrowserApi](https://kasparorange.github.io/BrowserApi/)**

---

## License

[MIT](LICENSE)
