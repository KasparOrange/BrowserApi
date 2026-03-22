# Migration from Raw IJSRuntime

If you have an existing Blazor application that uses `IJSRuntime` directly (or `IJSInProcessRuntime`) to interact with browser APIs, this guide shows you how to migrate to BrowserApi. Each section presents a common pattern as it looks today with raw interop, then shows the equivalent BrowserApi code.

## DOM Queries

**Before -- raw IJSRuntime:**

```csharp
// In your component or service
[Inject] IJSRuntime JS { get; set; }

// Query an element by selector
var elementRef = await JS.InvokeAsync<IJSObjectReference>(
    "document.querySelector", "#my-element");

// Query by ID
var byId = await JS.InvokeAsync<IJSObjectReference>(
    "document.getElementById", "header");
```

**After -- BrowserApi:**

```csharp
// Typed DOM access
var element = Document.QuerySelector<HtmlElement>("#my-element");
var header = Document.GetElementById("header");
```

No string method names, no `IJSObjectReference` to manage. The return types are strongly typed -- `HtmlElement`, `HtmlDivElement`, `HtmlInputElement`, etc.

## Property Access

**Before -- raw IJSRuntime:**

```csharp
// Get a property
var text = await JS.InvokeAsync<string>(
    "eval", "document.getElementById('title').textContent");

// Or with a helper JS function you wrote:
var value = await JS.InvokeAsync<string>(
    "getProperty", elementRef, "textContent");

// Set a property
await JS.InvokeVoidAsync(
    "setProperty", elementRef, "textContent", "Hello!");
```

**After -- BrowserApi:**

```csharp
// Get
string text = element.TextContent;

// Set
element.TextContent = "Hello!";
```

Properties are real C# properties on the generated types. The getter calls `GetProperty<string>("textContent")` and the setter calls `SetProperty("textContent", value)` internally -- but you never see that plumbing.

## Style Changes

**Before -- raw IJSRuntime:**

```csharp
// Setting individual styles
await JS.InvokeVoidAsync("eval",
    $"document.getElementById('box').style.marginTop = '16px'");

// Or with a helper function
await JS.InvokeVoidAsync("setStyle", elementRef, "margin-top", "16px");
await JS.InvokeVoidAsync("setStyle", elementRef, "background-color", "rgb(255, 0, 0)");
await JS.InvokeVoidAsync("setStyle", elementRef, "transform", "rotate(45deg) scale(1.5)");
```

**After -- BrowserApi:**

```csharp
// Individual properties with typed values
element.Style.MarginTop = Length.Px(16);
element.Style.BackgroundColor = CssColor.Rgb(255, 0, 0);
element.Style.Transform = Transform.Rotate(Angle.Deg(45)).ThenScale(1.5);

// Shorthand helpers
element.Style.SetMargin(Length.Px(8), Length.Px(16));  // vertical 8px, horizontal 16px
element.Style.SetPadding(Length.Rem(1));               // all sides 1rem
element.Style.SetGap(Length.Px(16));                   // grid/flex gap

// Transitions
element.Style.Transition = Transition.For("opacity", Duration.S(0.3), Easing.EaseInOut);

// Fluent numeric extensions work too
element.Style.MarginTop = 16.Px();
element.Style.FontSize = 1.5.Rem();
```

CSS values are type-checked at compile time. If you pass a `CssColor` where a `Length` is expected, the compiler catches it. The values serialize automatically through `ICssValue.ToCss()` when they reach the JavaScript boundary.

## Event Handling

**Before -- raw IJSRuntime:**

```csharp
// In your JS helper file (wwwroot/interop.js):
// window.addClickListener = (element, dotnetRef) => {
//     element.addEventListener('click', (e) => {
//         dotnetRef.invokeMethodAsync('HandleClick', e.clientX, e.clientY);
//     });
// };

// In your component:
[JSInvokable]
public void HandleClick(double clientX, double clientY) {
    // handle the click
}

protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
        var dotnetRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("addClickListener", elementRef, dotnetRef);
    }
}
```

**After -- BrowserApi:**

```csharp
// Subscribe (returns a disposable subscription)
using var sub = button.OnClick(e => {
    Console.WriteLine($"Clicked at ({e.ClientX}, {e.ClientY})");
});

// Keyboard events with typed properties
using var keySub = input.OnKeyDown(e => {
    if (e.IsKey(Key.Enter)) {
        SubmitForm();
    }
});

// Pointer events with device type detection
using var pointerSub = canvas.OnPointerMove(e => {
    if (e.IsPointerType(PointerType.Pen)) {
        // Handle stylus with pressure sensitivity
    }
});
```

No JavaScript helper files. No `DotNetObjectReference`. No `[JSInvokable]` attributes. Event objects are fully typed -- `MouseEvent`, `KeyboardEvent`, `PointerEvent` -- with all their standard properties available as C# members.

When you dispose the `EventSubscription`, the underlying `removeEventListener` call happens automatically.

## Fetch / HTTP Requests

**Before -- raw IJSRuntime:**

```csharp
// In JS helper:
// window.fetchJson = async (url) => {
//     const res = await fetch(url);
//     return await res.json();
// };

var data = await JS.InvokeAsync<JsonElement>("fetchJson", "/api/users");

// POST with body:
// window.postJson = async (url, body) => {
//     const res = await fetch(url, {
//         method: 'POST',
//         headers: { 'Content-Type': 'application/json' },
//         body: JSON.stringify(body)
//     });
//     return await res.json();
// };
var result = await JS.InvokeAsync<JsonElement>("postJson", "/api/users", newUser);
```

**After -- BrowserApi:**

```csharp
// Simple GET with automatic deserialization
var users = await Http.GetAsync<List<User>>("/api/users");

// POST with JSON body and headers
var created = await Http.Post("/api/users")
    .WithJsonBody(new { Name = "Alice", Email = "alice@example.com" })
    .WithHeader("Authorization", "Bearer token123")
    .SendJsonAsync<User>();

// PUT, PATCH, DELETE follow the same pattern
await Http.Put("/api/users/42")
    .WithJsonBody(updatedUser)
    .SendAsync();

await Http.Delete("/api/users/42").SendAsync();

// Non-throwing error handling
var result = await Http.Get("/api/data").TrySendAsync();
if (result.IsSuccess)
    Console.WriteLine($"Status: {result.Response!.Status}");
else
    Console.WriteLine($"Error: {result.Error!.Message}");

// Non-throwing with deserialization
var typedResult = await Http.Get("/api/users/42").TrySendAsync<User>();
if (typedResult.IsSuccess)
    Console.WriteLine($"User: {typedResult.Value!.Name}");
```

The `RequestBuilder` fluent API supports `WithHeader`, `WithHeaders`, `WithJsonBody`, `WithBody`, `WithMode`, `WithCredentials`, `WithCache`, `WithRedirect`, and `WithSignal` for abort support. Response handling includes `SendAsync` (raw response), `SendJsonAsync<T>` (auto-deserialize), and `TrySendAsync` / `TrySendAsync<T>` (non-throwing).

## Canvas 2D

**Before -- raw IJSRuntime:**

```csharp
// In JS helper:
// window.canvasHelper = {
//     fillRect: (ctx, x, y, w, h) => ctx.fillRect(x, y, w, h),
//     setFillColor: (ctx, color) => { ctx.fillStyle = color; },
//     beginPath: (ctx) => ctx.beginPath(),
//     moveTo: (ctx, x, y) => ctx.moveTo(x, y),
//     lineTo: (ctx, x, y) => ctx.lineTo(x, y),
//     stroke: (ctx) => ctx.stroke(),
// };

await JS.InvokeVoidAsync("canvasHelper.setFillColor", ctxRef, "red");
await JS.InvokeVoidAsync("canvasHelper.fillRect", ctxRef, 10, 10, 100, 50);
await JS.InvokeVoidAsync("canvasHelper.beginPath", ctxRef);
await JS.InvokeVoidAsync("canvasHelper.moveTo", ctxRef, 0, 0);
await JS.InvokeVoidAsync("canvasHelper.lineTo", ctxRef, 100, 100);
await JS.InvokeVoidAsync("canvasHelper.stroke", ctxRef);
```

**After -- BrowserApi:**

```csharp
// Typed context with direct method calls
ctx.SetFill(CssColor.Red)
   .FillRect(10, 10, 100, 50);

// Fluent path builder
ctx.Path()
   .MoveTo(0, 0)
   .LineTo(100, 100)
   .Stroke();

// Gradient builder
var gradient = ctx.LinearGradient(0, 0, 200, 0)
    .AddStop(0, CssColor.Red)
    .AddStop(1, CssColor.Blue)
    .Build();
ctx.SetFill(gradient).FillRect(0, 0, 200, 100);

// Save/restore with disposable scope
using (ctx.SaveState()) {
    ctx.Translate(50, 50);
    ctx.Rotate(Math.PI / 4);
    ctx.FillRect(0, 0, 100, 100);
}
// State automatically restored here
```

## Storage

**Before -- raw IJSRuntime:**

```csharp
// Get
var value = await JS.InvokeAsync<string?>("localStorage.getItem", "key");
var parsed = value is not null ? JsonSerializer.Deserialize<MyData>(value) : null;

// Set
var json = JsonSerializer.Serialize(data);
await JS.InvokeVoidAsync("localStorage.setItem", "key", json);

// Remove
await JS.InvokeVoidAsync("localStorage.removeItem", "key");
```

**After -- BrowserApi:**

```csharp
// Typed storage access
var data = storage.Get<MyData>("key");
storage.Set("key", data);
storage.Remove("key");
```

## What Changes in Your Component Structure

If you are using `ComponentBase` today, you switch to `BrowserApiComponentBase`:

**Before:**

```csharp
@inherits ComponentBase
@inject IJSRuntime JS

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            // manual JS interop setup
        }
    }
}
```

**After:**

```csharp
@inherits BrowserApiComponentBase

@code {
    protected override async Task OnBrowserApiReadyAsync() {
        // BrowserApi is initialized -- Document, Window, etc. are available
        var title = Document.Title;
    }
}
```

`BrowserApiComponentBase` sets up the `JsObject.Backend` automatically on first render and provides access to `Document`, `Window`, and other global objects.

## What You Can Delete

After migrating to BrowserApi, you can typically remove:

- **JavaScript helper files** (`wwwroot/interop.js`, `wwwroot/js/helpers.js`, etc.) -- the entire category of "bridge" JS that existed only to expose browser APIs to C#.

- **String constants for JS function names** -- no more `"document.querySelector"`, `"localStorage.getItem"`, `"eval"` string literals scattered through your C# code.

- **`[JSInvokable]` callback methods** -- event handling goes through typed `EventSubscription` objects instead of .NET-to-JS callback wiring.

- **`DotNetObjectReference` management** -- BrowserApi handles the C#-to-JS callback lifecycle internally.

- **Manual JSON serialization for fetch** -- `RequestBuilder.WithJsonBody()` and `SendJsonAsync<T>()` handle serialization and deserialization.

- **`IJSObjectReference` tracking and disposal** -- BrowserApi wraps JS object references in typed C# classes with proper `IDisposable`/`IAsyncDisposable` support.

## Migration Strategy

You do not need to migrate everything at once. BrowserApi can coexist with raw `IJSRuntime` calls in the same application:

1. **Add the BrowserApi packages** (`BrowserApi`, `BrowserApi.JSInterop`, `BrowserApi.Blazor`).

2. **Switch one component** to `BrowserApiComponentBase` and convert its JS interop calls.

3. **Verify** the component works identically.

4. **Repeat** for remaining components.

5. **Delete** the JS helper files once no component references them.

The typed APIs are additive -- they do not conflict with existing `IJSRuntime` usage.
