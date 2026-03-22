# BrowserApi Documentation

**Typed C# wrappers for every browser API — generated from W3C/WHATWG specs.**

BrowserApi brings the entire browser API surface to C# with full type safety, zero JavaScript, and IntelliSense for every property and method.

## Packages

| Package | Description |
|---------|-------------|
| **BrowserApi** | Core types — DOM, CSS, Canvas, Fetch, Storage, Events, Animations. Zero dependencies. |
| **BrowserApi.JSInterop** | Blazor WebAssembly backend using `IJSRuntime`. |
| **BrowserApi.Blazor** | DI registration, component base class, lifecycle hooks. |
| **BrowserApi.Runtime** | Server-side JS execution via Jint — tests without a browser, SSR, sandboxed scripts. |

## Quick Start

```csharp
@inherits BrowserApiComponentBase

@code {
    protected override async Task OnBrowserApiReadyAsync() {
        // Typed DOM access — no JavaScript needed
        var input = Document.QuerySelector<HtmlInputElement>("#email");
        input.Value = "hello@example.com";

        // Typed events
        input.OnInput(e => Console.WriteLine("Changed!"));

        // Fetch API
        var users = await Http.GetAsync<List<User>>("/api/users");

        // Canvas
        var ctx = Document.QuerySelector<HtmlCanvasElement>("canvas")!.GetContext2D();
        ctx.SetFill(CssColor.Red).FillRect(0, 0, 100, 50);

        // Animations
        input.FadeIn(500);
    }
}
```

## API Reference

Browse the full API reference in the left navigation, organized by namespace:

- **BrowserApi.Common** — Core abstractions (JsObject, IBrowserBackend, JsBatch)
- **BrowserApi.Dom** — DOM types, query extensions, typed events
- **BrowserApi.Css** — CSS value types (Length, CssColor, Transform, etc.)
- **BrowserApi.Canvas** — Canvas 2D fluent API
- **BrowserApi.Fetch** — HTTP client (Http.Get/Post, RequestBuilder)
- **BrowserApi.Animations** — Web Animations (Easing, KeyframeBuilder)
- **BrowserApi.WebStorage** — Typed localStorage/sessionStorage
- **BrowserApi.Runtime** — Server-side BrowserEngine via Jint
