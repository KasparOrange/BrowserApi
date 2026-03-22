# Architecture Overview

BrowserApi is a code-generation project that produces typed C# wrappers for browser APIs. Its central design principle is **separation of types from transport**: the core package contains all the type definitions and serialization logic with zero external dependencies, while separate packages provide the actual communication channel to JavaScript.

## The Five Packages

| Package | Dependencies | Purpose |
|---------|-------------|---------|
| **BrowserApi** | None | All generated and hand-written types: DOM, CSS, Canvas, Fetch, Events, etc. |
| **BrowserApi.JSInterop** | Microsoft.JSInterop | `JSInteropBackend` -- bridges BrowserApi to Blazor's `IJSRuntime` |
| **BrowserApi.Blazor** | ASP.NET Components | `BrowserApiComponentBase`, DI registration via `AddBrowserApi()` |
| **BrowserApi.Runtime** | Jint | `BrowserEngine` -- in-process JS engine + virtual DOM for testing |
| **BrowserApi.Generator** | (standalone CLI) | WebIDL parser and C# code emitter |

This split is by **dependency**, not by API surface. All DOM, CSS, Canvas, Fetch, and other browser types live in the core `BrowserApi` package. You never install `BrowserApi.Canvas` as a separate package -- it does not exist.

## IBrowserBackend -- the Transport Abstraction

The `IBrowserBackend` interface (`src/BrowserApi/Common/IBrowserBackend.cs`) defines every operation the type system needs from a JavaScript runtime:

```csharp
public interface IBrowserBackend : IAsyncDisposable {
    // Property access
    T GetProperty<T>(JsHandle target, string propertyName);
    void SetProperty(JsHandle target, string propertyName, object? value);

    // Method invocation (sync + async)
    void InvokeVoid(JsHandle target, string methodName, object?[] args);
    T Invoke<T>(JsHandle target, string methodName, object?[] args);
    Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args);
    Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args);

    // Object lifecycle
    JsHandle Construct(string jsClassName, object?[] args);
    JsHandle GetGlobal(string name);
    ValueTask DisposeHandle(JsHandle handle);

    // Events
    JsHandle AddEventListener(JsHandle target, string eventName, Action<JsHandle> callback);
    void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId);
}
```

Every method operates on `JsHandle` -- an opaque struct that wraps a backend-specific reference to a JavaScript object. In the Blazor backend, the handle wraps an `IJSObjectReference`. In the test backend, it wraps a `VirtualElement` or `VirtualDocument` directly. Consumer code never inspects the handle's inner value.

The core `BrowserApi` package defines this interface but provides **no implementation**. The implementations live in their respective packages:

- `JSInteropBackend` in `BrowserApi.JSInterop`
- `JintBackend` in `BrowserApi.Runtime`

## How JsObject Works

`JsObject` (`src/BrowserApi/Common/JsObject.cs`) is the abstract base class for every generated browser API type. It has three key aspects:

### 1. Static Backend

A single static property provides the backend for all instances:

```csharp
public abstract class JsObject : IDisposable, IAsyncDisposable {
    public static IBrowserBackend Backend { get; set; }
    public JsHandle Handle { get; internal set; }
}
```

You set `JsObject.Backend` once at startup. Every `JsObject` instance then delegates through it:

```csharp
// Inside JsObject:
protected T GetProperty<T>(string jsName) {
    var raw = Backend.GetProperty<object?>(Handle, jsName);
    return ConvertFromJs<T>(raw);
}

protected void SetProperty(string jsName, object? value) {
    Backend.SetProperty(Handle, jsName, ConvertToJs(value));
}

protected void InvokeVoid(string jsName, params object?[] args) {
    Backend.InvokeVoid(Handle, jsName, ConvertArgs(args));
}

protected T Invoke<T>(string jsName, params object?[] args) {
    var raw = Backend.Invoke<object?>(Handle, jsName, ConvertArgs(args));
    return ConvertFromJs<T>(raw);
}
```

### 2. Generated Types Delegate Through JsObject

Every generated class calls `GetProperty`, `SetProperty`, `Invoke`, or `InvokeVoid`. For example, the generated `Document` class:

```csharp
// Generated (Document.g.cs):
public partial class Document : Node {
    [JsName("URL")]
    public string Url => GetProperty<string>("URL");

    [JsName("characterSet")]
    public string CharacterSet => GetProperty<string>("characterSet");
}
```

The `[JsName]` attribute records the original JavaScript name for tooling and reflection. The string passed to `GetProperty`/`SetProperty`/`Invoke` is the camelCase JavaScript name.

### 3. The Conversion Pipeline

Before values cross the interop boundary, `ConvertToJs` and `ConvertFromJs<T>` handle type translation automatically.

**C# to JS (`ConvertToJs`):**

```csharp
internal static object? ConvertToJs(object? value) {
    return value switch {
        null => null,
        JsObject obj => obj.Handle,        // unwrap to handle
        ICssValue css => css.ToCss(),       // serialize: Length.Rem(1.5) -> "1.5rem"
        IWebIdlSerializable s => s.ToJs(),  // serialize complex WebIDL types
        Enum e => GetStringValue(e) ?? value, // ScrollBehavior.Smooth -> "smooth"
        _ => value                          // primitives pass through
    };
}
```

**JS to C# (`ConvertFromJs<T>`):**

- If `T` is a `JsObject` subclass, a new instance is created and assigned the returned handle.
- If `T` is an enum, the string value is matched against `[StringValue]` attributes.
- If `T` is `IConvertible`, `Convert.ChangeType` handles numeric conversions.
- Otherwise, a direct cast is attempted.

## Data Flow Diagram

```
                C# Application Code
                       |
                       v
    ┌──────────────────────────────────────┐
    │           Generated Types            │
    │  Document, Element, HtmlInputElement │
    │  CssStyleDeclaration, Event, ...     │
    │                                      │
    │  GetProperty<T>("textContent")       │
    │  SetProperty("className", value)     │
    │  Invoke<T>("querySelector", sel)     │
    └──────────────┬───────────────────────┘
                   │
                   v
    ┌──────────────────────────────────────┐
    │             JsObject                 │
    │                                      │
    │  ConvertToJs(value)                  │
    │    JsObject  -> Handle               │
    │    ICssValue -> .ToCss() string      │
    │    Enum      -> [StringValue] string  │
    │                                      │
    │  ConvertFromJs<T>(raw)               │
    │    Handle -> new T { Handle = h }    │
    │    string -> enum via [StringValue]   │
    └──────────────┬───────────────────────┘
                   │
                   v
    ┌──────────────────────────────────────┐
    │         IBrowserBackend              │
    │                                      │
    │  GetProperty, SetProperty            │
    │  Invoke, InvokeVoid                  │
    │  InvokeAsync, InvokeVoidAsync        │
    │  Construct, GetGlobal                │
    │  AddEventListener                    │
    └──────────┬───────────┬───────────────┘
               │           │
               v           v
    ┌─────────────┐  ┌──────────────┐
    │ JSInterop   │  │ JintBackend  │
    │ Backend     │  │              │
    │             │  │ VirtualDOM   │
    │ IJSRuntime  │  │ Jint engine  │
    │ -> browser  │  │ -> in-memory │
    └─────────────┘  └──────────────┘
```

## Generated Code + Hand-Written Partial Classes

BrowserApi uses `partial class` and `partial struct` to compose generated code with hand-written ergonomic extensions. The generator produces the structural skeleton; hand-written code adds developer-friendly APIs on top.

**Generated** (`src/BrowserApi/Generated/Css/Length.g.cs`):

```csharp
// <auto-generated/>
public readonly partial struct Length : ICssValue {
    private readonly string _value;

    public Length(string value) {
        _value = value;
    }

    public string ToCss() => _value;
    public override string ToString() => _value;
}
```

**Hand-written** (`src/BrowserApi/Css/Length.cs`):

```csharp
public readonly partial struct Length : IEquatable<Length> {
    public static Length Zero { get; } = new("0");
    public static Length Auto { get; } = new("auto");

    public static Length Px(double value) => new($"{FormatNumber(value)}px");
    public static Length Rem(double value) => new($"{FormatNumber(value)}rem");
    public static Length Percent(double value) => new($"{FormatNumber(value)}%");
    public static Length Calc(string expression) => new($"calc({expression})");

    // Implicit conversion: Length margin = 16; -> "16px"
    public static implicit operator Length(int value) => Px(value);

    // Arithmetic operators produce calc() expressions
    public static Length operator +(Length a, Length b) =>
        new($"calc({a.ToCss()} + {b.ToCss()})");
    public static Length operator -(Length a, Length b) =>
        new($"calc({a.ToCss()} - {b.ToCss()})");
}
```

The same pattern applies to `CssColor` (generated skeleton + hand-written `Rgb()`, `Hsl()`, `Hex()` factories), `Transform` (hand-written `Translate()`, `Rotate()`, `Scale()` with fluent `Then()` chaining), and all other CSS value types.

## Why Swappable Backends Matter

Because `IBrowserBackend` is the only dependency the type system has on any runtime, you can swap backends freely:

| Backend | Package | Use Case |
|---------|---------|----------|
| `JSInteropBackend` | BrowserApi.JSInterop | Production Blazor apps (WASM and Server) |
| `JintBackend` | BrowserApi.Runtime | Unit/integration tests without a browser |
| Custom mock | Your test project | Targeted test doubles for specific scenarios |
| (Future) WASM Component Model | TBD | Direct WASM host interop without Blazor |

The test backend (`JintBackend`) is particularly powerful: it combines a Jint JavaScript engine with a virtual DOM, so you can run both C# BrowserApi code and JavaScript against the same in-memory DOM tree. See the [Testing with BrowserEngine](testing.md) article for details.

## Backend Setup in Practice

### Blazor (production)

```csharp
// Program.cs
builder.Services.AddBrowserApi();

// Your component:
@inherits BrowserApiComponentBase

@code {
    protected override async Task OnBrowserApiReadyAsync() {
        // Window and Document are ready here
        var title = Document.Title;
    }
}
```

`BrowserApiComponentBase` creates a `JSInteropBackend` from the injected `IJSRuntime` on first render, assigns it to `JsObject.Backend`, and provides `Window` and `Document` properties.

### Testing

```csharp
using var engine = new BrowserEngine();
// JsObject.Backend is set automatically
// engine.Document is a live BrowserApi Document backed by VirtualDocument

engine.Execute("document.body.innerHTML = '<div id=\"app\">Hello</div>'");
var el = engine.VirtualDocument.GetElementById("app");
Assert.Equal("Hello", el?.TextContent);
```

### Manual setup

```csharp
var backend = new JSInteropBackend(jsRuntime);
JsObject.Backend = backend;

var doc = new Document { Handle = backend.GetGlobal("document") };
doc.Title = "Hello, BrowserApi!";
```

## Key Takeaways

1. **Types have zero dependencies.** The core `BrowserApi` package is pure C# -- no Blazor, no JSInterop, no Jint.
2. **One interface separates types from transport.** `IBrowserBackend` is the single seam between your typed C# code and the JavaScript world.
3. **Conversion is automatic.** `ConvertToJs`/`ConvertFromJs<T>` handle `JsObject` unwrapping, CSS serialization, enum mapping, and primitive coercion transparently.
4. **Partial classes compose generated + hand-written code.** The generator provides the structural mapping; hand-written code adds ergonomic factory methods, operators, and builders.
5. **Backends are swappable.** The same `Document.QuerySelector("#app")` call works identically against a real browser or a virtual DOM in a unit test.
