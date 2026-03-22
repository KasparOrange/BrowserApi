# Performance Guide

Every BrowserApi property access and method call crosses the .NET-to-JavaScript interop boundary. In Blazor WebAssembly, each crossing costs roughly 1-5 microseconds. In Blazor Server, each crossing is a SignalR round-trip -- milliseconds or more. This guide covers the batching and bulk-query APIs that let you collapse many individual calls into one.

## The Problem: N+1 Interop Calls

Consider updating a list of 50 elements:

```csharp
// Naive approach: 50 elements x 2 property sets = 100 interop calls
foreach (var item in items) {
    var el = document.GetElementById(item.Id);
    el.TextContent = item.Text;      // interop call #1
    el.ClassName = item.CssClass;    // interop call #2
}
```

Or reading data from multiple elements:

```csharp
// Naive approach: 1 querySelectorAll + 20 property reads = 21 interop calls
var rows = document.QuerySelectorAll("tr.data-row");
foreach (var row in rows) {
    var text = row.TextContent;  // interop call per row
    results.Add(text);
}
```

BrowserApi provides two complementary solutions: **JsBatch** for writes and **bulk query extensions** for reads.

## JsBatch: Batching Writes

`JsBatch` (`src/BrowserApi/Common/JsBatch.cs`) collects multiple void operations -- property sets and method calls -- and executes them in a single interop round-trip via the JavaScript `browserApi.batch` function.

### RunAsync: The One-Liner

For the common case, use `JsBatch.RunAsync`:

```csharp
await JsBatch.RunAsync(batch => {
    batch.SetProperty(element, "textContent", "Hello");
    batch.SetProperty(element, "className", "active");
    batch.InvokeVoid(element, "focus");
});
// All three operations execute in 1 interop call
```

### Instance API: Build-Then-Execute

For more control, create a `JsBatch` instance, queue operations, and execute:

```csharp
var batch = new JsBatch();

foreach (var item in items) {
    batch.SetProperty(item.Element, "textContent", item.Text);
    batch.SetProperty(item.Element, "className", item.CssClass);
}

Console.WriteLine($"Queued {batch.Count} operations");
await batch.ExecuteAsync();
// batch is cleared and can be reused
```

### BatchAsync: Per-Object Fluent API

When all operations target a single object, `BatchAsync` (`src/BrowserApi/Common/JsObjectBatchExtensions.cs`) provides a fluent scope:

```csharp
await element.BatchAsync(scope => scope
    .Set("textContent", "Updated text")
    .Set("className", "highlight")
    .Set("hidden", false)
    .Call("scrollIntoView")
);
```

`JsBatchScope.Set` maps to `JsBatch.SetProperty` and `JsBatchScope.Call` maps to `JsBatch.InvokeVoid`. The scope is syntactic sugar -- under the hood it creates a `JsBatch` and calls `ExecuteAsync()`.

### Target Deduplication

`JsBatch` tracks target objects by handle. If you queue 10 property sets on the same element, the element's handle is transmitted to JavaScript only once. The command array references it by index:

```csharp
// Internally, the batch sends:
// targets: [elementHandle]
// commands: [
//   { t: 0, o: 0, n: "textContent", v: "Hello" },   // t=0 means targets[0]
//   { t: 0, o: 0, n: "className", v: "active" },
//   { t: 0, o: 1, n: "focus", a: [] },               // o=1 means method call
// ]
```

### CSS Value Conversion in Batches

CSS values and other special types are automatically converted before being added to the batch, exactly as they are in normal property sets:

```csharp
await JsBatch.RunAsync(batch => {
    // ICssValue types are serialized via ToCss()
    batch.SetProperty(element, "style.width", Length.Percent(100));
    batch.SetProperty(element, "style.color", CssColor.Rgb(255, 0, 0));

    // Enums are serialized via [StringValue]
    batch.SetProperty(element, "style.display", DisplayValue.Flex);
});
```

## Bulk Queries: Batching Reads

While `JsBatch` handles writes, reading multiple values requires a different approach. BrowserApi provides two levels of bulk read APIs.

### GetPropertiesAsync: Multiple Properties from One Object

`GetPropertiesAsync` (`src/BrowserApi/Common/JsObjectBulkExtensions.cs`) reads several properties from a single `JsObject` in one call:

```csharp
var props = await element.GetPropertiesAsync(
    "offsetWidth", "offsetHeight", "scrollTop", "scrollLeft"
);

var width = (double)props["offsetWidth"]!;
var height = (double)props["offsetHeight"]!;
var scrollTop = (double)props["scrollTop"]!;
var scrollLeft = (double)props["scrollLeft"]!;
// 1 interop call instead of 4
```

This calls the JavaScript `browserApi.getProperties` helper, which reads all requested properties in a single invocation and returns them as a dictionary.

### QueryValuesAsync: One Property from Many Elements

`QueryValuesAsync` (`src/BrowserApi/Dom/BulkQueryExtensions.cs`) runs a CSS selector query and reads a single property from every matching element in one call:

```csharp
// Read all list-item text in 1 interop call instead of N+1
string[] texts = await document.QueryValuesAsync<string>("li.todo", "textContent");
```

This replaces the common pattern of `querySelectorAll` + per-element property reads. It works on both `Document` and `Element`:

```csharp
// Scoped to a container element
string[] prices = await container.QueryValuesAsync<string>(".price", "textContent");
```

### QueryPropertiesAsync: Multiple Properties from Many Elements

`QueryPropertiesAsync` reads several properties from each matching element:

```csharp
var rows = await document.QueryPropertiesAsync(
    "tr.data-row",
    "id", "className", "textContent"
);

foreach (var row in rows) {
    var id = (string)row["id"]!;
    var cls = (string)row["className"]!;
    var text = (string)row["textContent"]!;
}
// 1 interop call regardless of how many rows match
```

### QueryElementsAsync: Live Element Handles

When you need to interact with the matched elements further (not just read values), `QueryElementsAsync` returns `Element[]` with live `JsHandle` references:

```csharp
Element[] buttons = await document.QueryElementsAsync("button.action");

// Now you can batch writes against these elements:
await JsBatch.RunAsync(batch => {
    foreach (var button in buttons) {
        batch.SetProperty(button, "disabled", true);
    }
});
```

This is 2 interop calls total (1 query + 1 batch) regardless of how many buttons exist.

## The Fetch-LINQ-Batch Pattern

A common real-world pattern combines bulk reads, LINQ processing, and batched writes:

```csharp
// Step 1: Read all todo items in 1 call
var todos = await document.QueryPropertiesAsync(
    ".todo-item",
    "id", "className", "textContent"
);

// Step 2: Process in C# with LINQ (zero interop calls)
var completedIds = todos
    .Where(t => ((string)t["className"]!).Contains("completed"))
    .Select(t => (string)t["id"]!)
    .ToList();

// Step 3: Get live handles for the elements we need to update
Element[] elements = await document.QueryElementsAsync(
    string.Join(", ", completedIds.Select(id => $"#{id}"))
);

// Step 4: Batch all updates in 1 call
await JsBatch.RunAsync(batch => {
    foreach (var el in elements) {
        batch.SetProperty(el, "hidden", true);
        batch.InvokeVoid(el, "remove");
    }
});

// Total: 3 interop calls instead of potentially hundreds
```

## What Cannot Be Batched

Not everything can be collapsed into a single call. The following require individual interop round-trips:

| Operation | Why |
|-----------|-----|
| `GetProperty<T>()` on a single element | Returns a value -- must be synchronous |
| `Invoke<T>()` with a return value | The return value is needed immediately |
| Event registration (`AddEventListener`) | Returns a listener handle |
| `Construct()` | Returns a new `JsHandle` |
| Conditional logic depending on JS state | Need the value before deciding the next step |

For these, use the normal single-call API. The batching APIs only support **void** operations (property sets and void method calls).

## When Batching Matters

### Always batch when:

- Setting multiple properties on one or more elements in a loop
- Applying CSS class changes across a collection
- Performing bulk DOM mutations (show/hide, enable/disable)
- Running on Blazor Server (every call is a network round-trip)

### Probably fine without batching when:

- Setting a single property in a click handler
- Reading one property from one element
- Running on Blazor WebAssembly with fewer than ~10 operations
- During initial page setup (user won't notice a few ms)

### Rule of thumb:

- **1-3 operations**: individual calls are fine
- **4-20 operations**: use `JsBatch.RunAsync` or `BatchAsync`
- **20+ operations or reading from many elements**: use `QueryValuesAsync`/`QueryPropertiesAsync` for reads and `JsBatch` for writes
- **Blazor Server**: always batch when you can -- even 2-3 operations benefit from collapsing round-trips

## Quick Reference

| Goal | API | Location |
|------|-----|----------|
| Batch void writes to multiple objects | `JsBatch.RunAsync(batch => { ... })` | `JsBatch` |
| Batch void writes to one object | `element.BatchAsync(s => s.Set(...).Call(...))` | `JsObjectBatchExtensions` |
| Read N properties from 1 object | `element.GetPropertiesAsync("a", "b", "c")` | `JsObjectBulkExtensions` |
| Read 1 property from N elements | `doc.QueryValuesAsync<string>(selector, prop)` | `BulkQueryExtensions` |
| Read N properties from N elements | `doc.QueryPropertiesAsync(selector, props...)` | `BulkQueryExtensions` |
| Get live handles for N elements | `doc.QueryElementsAsync(selector)` | `BulkQueryExtensions` |
