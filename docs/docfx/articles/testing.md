# Testing with BrowserEngine

BrowserApi code manipulates the DOM -- elements, styles, attributes, queries. Testing that code normally requires launching a real browser via Playwright or Selenium, which is slow, flaky, and hard to run in CI.

`BrowserEngine` (`src/BrowserApi.Runtime/BrowserEngine.cs`) solves this by providing a Jint JavaScript engine paired with a virtual DOM. Your tests run entirely in-process, with no browser, no WebDriver, and no network. A typical test completes in under a millisecond.

## Setup

Add the `BrowserApi.Runtime` package to your test project:

```xml
<ItemGroup>
  <PackageReference Include="BrowserApi.Runtime" />
</ItemGroup>
```

Create a `BrowserEngine` in each test. It sets `JsObject.Backend` automatically:

```csharp
using BrowserApi.Runtime;

[Fact]
public void Document_has_body() {
    using var engine = new BrowserEngine();

    Assert.NotNull(engine.VirtualDocument.Body);
    Assert.Equal("body", engine.VirtualDocument.Body.TagName);
}
```

`BrowserEngine` is disposable. Use `using` to clean up the Jint engine after each test. If your test framework supports it, you can also create it in a fixture, but be aware that `JsObject.Backend` is a static property -- parallel tests that each create their own `BrowserEngine` will overwrite each other's backend.

## What BrowserEngine Provides

When you construct a `BrowserEngine`, it:

1. Creates a `VirtualDocument` with `<html>`, `<head>`, and `<body>` elements.
2. Creates a `VirtualConsole` that captures `console.log`, `console.error`, `console.warn`, and `console.info` calls.
3. Creates a `JintBackend` that routes all `JsObject` operations to the virtual DOM.
4. Sets `JsObject.Backend` to that `JintBackend`.
5. Creates a BrowserApi `Document` instance wired to the virtual DOM.
6. Initializes a Jint `Engine` with `document` and `console` as global objects.

You can work from either side:

- **C# side**: use `engine.Document` (a BrowserApi `Document`) for typed DOM operations.
- **JavaScript side**: use `engine.Execute(script)` to run JS that manipulates `document`.
- **Inspection side**: use `engine.VirtualDocument` to directly inspect the DOM tree.

All three views see the same underlying data.

## Test Patterns

### Pattern 1: Execute JavaScript, Assert on the Virtual DOM

Run JavaScript code and inspect the resulting DOM tree through `VirtualDocument`:

```csharp
[Fact]
public void CreateElement_and_append() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var div = document.createElement('div');
        div.id = 'app';
        div.textContent = 'Hello from JS!';
        document.body.appendChild(div);
    ");

    var app = engine.VirtualDocument.GetElementById("app");
    Assert.NotNull(app);
    Assert.Equal("div", app.TagName);
    Assert.Equal("Hello from JS!", app.TextContent);
}
```

### Pattern 2: Use BrowserApi C# Types

The `engine.Document` property is a full BrowserApi `Document` backed by the virtual DOM. You can call any generated DOM method:

```csharp
[Fact]
public void QuerySelector_returns_typed_element() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var ul = document.createElement('ul');
        var li = document.createElement('li');
        li.className = 'active';
        li.textContent = 'Item 1';
        ul.appendChild(li);
        document.body.appendChild(ul);
    ");

    var el = engine.Document.QuerySelector("li.active");
    Assert.NotNull(el);
    Assert.Equal("Item 1", el.TextContent);
}
```

### Pattern 3: QuerySelector to Find Elements and Check Properties

Use the virtual DOM's query methods to locate elements and assert on their state:

```csharp
[Fact]
public void SetAttribute_is_reflected() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var input = document.createElement('input');
        input.setAttribute('type', 'email');
        input.setAttribute('placeholder', 'you@example.com');
        input.id = 'email';
        document.body.appendChild(input);
    ");

    var input = engine.VirtualDocument.QuerySelector("input#email");
    Assert.NotNull(input);
    Assert.Equal("email", input.GetAttribute("type"));
    Assert.Equal("you@example.com", input.GetAttribute("placeholder"));
    Assert.True(input.HasAttribute("type"));
}
```

### Pattern 4: Style Assertions

`VirtualElement.Style` is a `VirtualStyle` object that provides dictionary-style access to CSS properties. Styles set from JavaScript (via camelCase names) are stored as kebab-case keys:

```csharp
[Fact]
public void Inline_styles_are_captured() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var div = document.createElement('div');
        div.style.color = 'red';
        div.style.fontSize = '16px';
        div.style.backgroundColor = '#f0f0f0';
        document.body.appendChild(div);
    ");

    var div = engine.VirtualDocument.QuerySelector("div");
    Assert.NotNull(div);

    // VirtualStyle stores properties in kebab-case
    Assert.Equal("red", div.Style["color"]);
    Assert.Equal("16px", div.Style["font-size"]);
    Assert.Equal("#f0f0f0", div.Style["background-color"]);

    // CssText gives the full inline style string
    Assert.Contains("color: red", div.Style.CssText);
    Assert.Equal(3, div.Style.Count);
}
```

### Pattern 5: Console.log Capture

`VirtualConsole` records all console output as `ConsoleMessage` records with a `Level` (`"log"`, `"error"`, `"warn"`, `"info"`) and `Text` (space-joined string of arguments):

```csharp
[Fact]
public void Console_messages_are_captured() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        console.log('Hello', 'world');
        console.error('Something went wrong');
        console.warn('Deprecation notice');
        console.info('Version 2.0');
    ");

    Assert.Equal(4, engine.VirtualConsole.Messages.Count);

    Assert.Equal("log", engine.VirtualConsole.Messages[0].Level);
    Assert.Equal("Hello world", engine.VirtualConsole.Messages[0].Text);

    Assert.Equal("error", engine.VirtualConsole.Messages[1].Level);
    Assert.Equal("Something went wrong", engine.VirtualConsole.Messages[1].Text);

    Assert.Equal("warn", engine.VirtualConsole.Messages[2].Level);
    Assert.Equal("info", engine.VirtualConsole.Messages[3].Level);
}
```

You can use this to verify that your code logs expected messages, or to debug test failures:

```csharp
[Fact]
public void Debug_with_console() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var items = document.querySelectorAll('li');
        console.log('Found', items.length, 'items');
    ");

    // Print captured console output if a test fails:
    foreach (var msg in engine.VirtualConsole.Messages)
        Console.WriteLine($"[{msg.Level}] {msg.Text}");
}
```

### Pattern 6: HTML Serialization for Snapshot Testing

`VirtualElement` provides `OuterHtml` and `InnerHtml` properties that serialize the DOM subtree to HTML strings. Use these for snapshot-style assertions:

```csharp
[Fact]
public void OuterHtml_snapshot() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var nav = document.createElement('nav');
        nav.id = 'main-nav';
        nav.className = 'sidebar';

        var a = document.createElement('a');
        a.textContent = 'Home';
        a.setAttribute('href', '/');
        nav.appendChild(a);

        document.body.appendChild(nav);
    ");

    var nav = engine.VirtualDocument.GetElementById("main-nav");
    Assert.NotNull(nav);

    Assert.Equal(
        "<nav id=\"main-nav\" class=\"sidebar\"><a href=\"/\">Home</a></nav>",
        nav.OuterHtml
    );

    Assert.Equal(
        "<a href=\"/\">Home</a>",
        nav.InnerHtml
    );
}
```

OuterHtml includes the element's tag, id, class, inline style, and all attributes. InnerHtml includes only the children.

### Pattern 7: Evaluating JavaScript Expressions

Use `Evaluate<T>` to run a JavaScript expression and get the result back as a CLR type:

```csharp
[Fact]
public void Evaluate_returns_typed_result() {
    using var engine = new BrowserEngine();

    var sum = engine.Evaluate<double>("2 + 2");
    Assert.Equal(4.0, sum);

    var pi = engine.Evaluate<double>("Math.PI");
    Assert.Equal(Math.PI, pi);

    engine.Execute("var x = 42;");
    var x = engine.Evaluate<int>("x");
    Assert.Equal(42, x);
}
```

### Pattern 8: Testing CSS Value Serialization

CSS value types (`Length`, `CssColor`, `Transform`, etc.) are pure structs with no interop dependency. Test them directly without `BrowserEngine`:

```csharp
[Fact]
public void Length_factory_methods() {
    Assert.Equal("16px", Length.Px(16).ToCss());
    Assert.Equal("1.5rem", Length.Rem(1.5).ToCss());
    Assert.Equal("50%", Length.Percent(50).ToCss());
    Assert.Equal("auto", Length.Auto.ToCss());
    Assert.Equal("0", Length.Zero.ToCss());
}

[Fact]
public void Length_calc_via_operators() {
    var result = Length.Percent(100) - Length.Px(20);
    Assert.Equal("calc(100% - 20px)", result.ToCss());
}

[Fact]
public void Length_implicit_conversion_uses_px() {
    Length margin = 8;
    Assert.Equal("8px", margin.ToCss());
}

[Fact]
public void CssColor_functional_notation() {
    Assert.Equal("rgb(255, 128, 0)", CssColor.Rgb(255, 128, 0).ToCss());
    Assert.Equal("rgba(0, 0, 0, 0.5)", CssColor.Rgba(0, 0, 0, 0.5).ToCss());
    Assert.Equal("hsl(200, 50%, 70%)", CssColor.Hsl(200, 50, 70).ToCss());
    Assert.Equal("#ff0080", CssColor.Hex("#ff0080").ToCss());
}

[Fact]
public void Transform_chaining() {
    var t = Transform.Translate(Length.Px(10), Length.Px(20))
        .ThenRotate(Angle.Deg(45))
        .ThenScale(1.5);

    Assert.Equal(
        "translate(10px, 20px) rotate(45deg) scale(1.5)",
        t.ToCss()
    );
}
```

These tests require only the `BrowserApi` package -- no `BrowserApi.Runtime`, no Jint, no browser.

### Pattern 9: Building Complex DOM Trees

Combine JavaScript execution and virtual DOM inspection for more complex scenarios:

```csharp
[Fact]
public void Complex_dom_structure() {
    using var engine = new BrowserEngine();

    engine.Execute(@"
        var table = document.createElement('table');
        for (var i = 0; i < 3; i++) {
            var tr = document.createElement('tr');
            tr.className = 'row';
            var td = document.createElement('td');
            td.textContent = 'Cell ' + i;
            tr.appendChild(td);
            table.appendChild(tr);
        }
        document.body.appendChild(table);
    ");

    var rows = engine.VirtualDocument.QuerySelectorAll("tr.row");
    Assert.Equal(3, rows.Count);

    Assert.Equal("Cell 0", rows[0].QuerySelector("td")?.TextContent);
    Assert.Equal("Cell 1", rows[1].QuerySelector("td")?.TextContent);
    Assert.Equal("Cell 2", rows[2].QuerySelector("td")?.TextContent);
}
```

## The Virtual DOM in Detail

### VirtualDocument

`VirtualDocument` (`src/BrowserApi.Runtime/VirtualDom/VirtualDocument.cs`) starts with a standard HTML skeleton:

```
#document
  └─ <html>
       ├─ <head>
       └─ <body>
```

Key methods:

| Method | Description |
|--------|-------------|
| `CreateElement(tagName)` | Creates a detached `VirtualElement` |
| `CreateTextNode(data)` | Creates a detached `VirtualTextNode` |
| `GetElementById(id)` | Finds element by `id` attribute |
| `QuerySelector(selector)` | First match for CSS selector |
| `QuerySelectorAll(selector)` | All matches for CSS selector |
| `Body` | The `<body>` element |
| `Head` | The `<head>` element |
| `DocumentElement` | The `<html>` element |

### VirtualElement

`VirtualElement` (`src/BrowserApi.Runtime/VirtualDom/VirtualElement.cs`) supports:

| Property/Method | Description |
|-----------------|-------------|
| `TagName` | Lowercase tag name (`"div"`, `"span"`) |
| `Id` | The `id` attribute |
| `ClassName` | Space-separated class names |
| `Style` | `VirtualStyle` for inline CSS |
| `Attributes` | All attributes as a dictionary |
| `Children` | Child elements (excludes text nodes) |
| `ChildNodes` | All child nodes (elements + text) |
| `TextContent` | Text content (inherited from `VirtualNode`) |
| `GetAttribute(name)` / `SetAttribute(name, value)` | Attribute access |
| `HasAttribute(name)` / `RemoveAttribute(name)` | Attribute queries |
| `QuerySelector(selector)` / `QuerySelectorAll(selector)` | Scoped queries |
| `OuterHtml` | Full HTML including this element |
| `InnerHtml` | HTML of children only |
| `AppendChild(node)` / `RemoveChild(node)` | Tree mutation |

### VirtualStyle

`VirtualStyle` (`src/BrowserApi.Runtime/VirtualDom/VirtualStyle.cs`) stores CSS properties in a dictionary with kebab-case keys:

```csharp
var style = new VirtualStyle();
style["color"] = "red";
style["font-size"] = "16px";

Assert.Equal("color: red; font-size: 16px", style.CssText);
Assert.Equal(2, style.Count);

// Setting to empty removes the property
style["color"] = "";
Assert.Equal(1, style.Count);
```

When accessed through the `IVirtualNode` interface (as JavaScript does), camelCase names like `backgroundColor` are automatically converted to `background-color`.

### VirtualConsole

`VirtualConsole` (`src/BrowserApi.Runtime/VirtualDom/VirtualConsole.cs`) records messages as `ConsoleMessage(Level, Text)` records:

```csharp
var console = new VirtualConsole();
console.Log("value:", 42);

Assert.Equal("log", console.Messages[0].Level);
Assert.Equal("value: 42", console.Messages[0].Text);

console.Clear();
Assert.Empty(console.Messages);
```

### CSS Selector Support

The virtual DOM supports simple CSS selectors via `SimpleSelector`:

- Tag name: `div`, `span`, `input`
- ID: `#app`, `#main-nav`
- Class: `.active`, `.btn.primary`
- Compound: `div.container`, `input#email`, `li.active`
- Comma-separated: `h1, h2, h3`

It does **not** support combinators (`>`, `+`, `~`), pseudo-classes (`:hover`, `:nth-child`), pseudo-elements (`::before`), or attribute selectors (`[type="text"]`).

## Limitations

The virtual DOM is a simplified model. It does not support:

| Feature | Why Not |
|---------|---------|
| CSS layout / computed styles | No layout engine -- `offsetWidth`, `getBoundingClientRect()` are not implemented |
| Visual rendering | No pixel rendering -- cannot screenshot or visually compare |
| Network requests (`fetch`, `XMLHttpRequest`) | No network stack in Jint |
| Web APIs beyond basic DOM | No `IntersectionObserver`, `ResizeObserver`, `MutationObserver` |
| CSS cascade / specificity | Inline styles only -- no stylesheet processing |
| Complex selector combinators | `>`, `+`, `~` are not supported |
| Event propagation / bubbling | `AddEventListener` is a no-op in `JintBackend` |
| Timers (`setTimeout`, `setInterval`) | Not wired in the default Jint setup |

## When to Use a Real Browser

Use `BrowserEngine` for:

- DOM structure tests (create elements, set attributes, query)
- CSS value serialization tests (pure `ToCss()` assertions)
- Console output assertions
- HTML snapshot testing
- Unit testing component logic that manipulates the DOM

Use Playwright/Selenium when you need:

- Visual regression testing
- CSS layout verification (flexbox, grid, positioning)
- Network request testing
- Real event simulation (click, scroll, drag)
- Browser-specific behavior validation
- Performance profiling

A good testing strategy uses `BrowserEngine` for the bulk of tests (fast, reliable, CI-friendly) and reserves real browser tests for the scenarios that genuinely require rendering or network behavior.
