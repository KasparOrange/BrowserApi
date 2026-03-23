# Getting Started

## Performance: What You Need to Know First

Every BrowserApi call (property access, method invocation) is a JavaScript interop call. Each call has overhead:

- **Blazor WASM**: JSON serialization + WASM↔JS boundary crossing. Synchronous calls via `IJSInProcessRuntime` are [~4x faster than async](https://www.meziantou.net/optimizing-js-interop-in-a-blazor-webassembly-application.htm), and BrowserApi uses sync calls by default in WASM.
- **Blazor Server**: Same overhead **plus a network roundtrip over SignalR** for every call. Latency depends on connection quality (typically 10–100ms per call).

**This is fine for:** event handlers, user interactions, reading a few properties, occasional DOM updates.

**This is NOT fine for:** tight loops setting 100 properties, animation frames, real-time rendering.

**For bulk operations**, BrowserApi provides [`JsBatch`](performance.md) (N writes → 1 call) and [`QueryValuesAsync`](performance.md) (N reads → 1 call). See the [Performance Guide](performance.md) for patterns that reduce interop to 2 calls regardless of data size.

## Installation

Add the BrowserApi packages to your Blazor WebAssembly project:

```bash
dotnet add package BrowserApi
dotnet add package BrowserApi.Blazor
```

## Setup

### 1. Register services

In your `Program.cs`:

```csharp
builder.Services.AddBrowserApi();
```

### 2. Add the JavaScript helper

In your `index.html`, add the script tag before the closing `</body>`:

```html
<script src="_content/BrowserApi.JSInterop/browserapi.js"></script>
```

### 3. Create a component

Inherit from `BrowserApiComponentBase` and override `OnBrowserApiReadyAsync`:

```razor
@inherits BrowserApiComponentBase

<h1>@_title</h1>
<p>Window width: @_width px</p>

@code {
    private string _title = "Loading...";
    private int _width;

    protected override async Task OnBrowserApiReadyAsync() {
        _title = Document.Title;
        _width = Window.InnerWidth;
        StateHasChanged();
    }
}
```

## Performance: Batching

For bulk DOM updates, use `JsBatch` to reduce interop roundtrips:

```csharp
// 1 interop call instead of 3
await JsBatch.RunAsync(batch => {
    batch.SetProperty(element, "textContent", "hello");
    batch.SetProperty(element, "className", "active");
    batch.InvokeVoid(element, "focus");
});
```

For bulk reads, use `QueryValuesAsync` to fetch data in one call, then process with LINQ:

```csharp
// 1 interop call → pure C# LINQ → 1 interop call
var texts = await document.QueryValuesAsync<string>("li", "textContent");
var filtered = texts.Where(t => t.Length > 5).OrderBy(t => t).ToList();
await JsBatch.RunAsync(batch => {
    batch.SetProperty(output, "textContent", string.Join(", ", filtered));
});
```

## Server-Side Testing

Use `BrowserApi.Runtime` to test browser interactions without a browser:

```bash
dotnet add package BrowserApi.Runtime
```

```csharp
var engine = new BrowserEngine();
engine.Execute(@"
    var div = document.createElement('div');
    div.className = 'card';
    div.style.display = 'flex';
    document.body.appendChild(div);
");

var card = engine.VirtualDocument.QuerySelector(".card");
Assert.Equal("flex", card.Style["display"]);
```
