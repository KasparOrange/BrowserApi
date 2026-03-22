# Getting Started

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
