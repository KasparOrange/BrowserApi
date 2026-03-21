# Runtime (Experimental Extension)

**Parent:** [browser-api.md](browser-api.md)

**Status:** Future idea. Not part of the core roadmap. Only relevant after Phases 1–5 are complete.

## The Problem This Solves

BrowserApi gives you typed C# wrappers for browser APIs. But those wrappers only *do* something when connected to a real browser (via JSInterop in Blazor). Without a browser, the types can serialize to strings (`ToCss()`, `ToHtml()`) but can't *execute* anything.

That means:

- **You can't test browser interactions without a browser.** Want to verify that your code correctly manipulates the DOM? You need Playwright or Selenium spinning up a headless Chrome. That's slow, flaky, and annoying in CI.
- **You can't run third-party JavaScript on the server.** If a library exists only in JS (a date formatter, a markdown parser, a chart layout engine), you either find a C# equivalent or set up Node.js as a sidecar process.
- **You can't let users write scripts.** If your app wants to let admins write custom validation rules, transformation logic, or automation scripts, those scripts need a runtime to execute in.

## The Solution: Jint as a Server-Side Browser

[Jint](https://github.com/sebastienros/jint) is a JavaScript interpreter written in pure C#. No native dependencies, no V8 binaries, no Node.js. You give it a JS string, it executes it inside your .NET process.

The idea: wire BrowserApi's typed DOM/CSS/Canvas types as Jint host objects. JavaScript code running in Jint would see `document.querySelector()`, `element.style.color = "red"`, `fetch()` — all backed by your C# types instead of a real browser.

```
┌─────────────────────────────────────────┐
│  JavaScript code (user-provided)        │
│  document.querySelector('.card')        │
│    .style.backgroundColor = 'red';      │
└─────────────────┬───────────────────────┘
                  │  Jint interprets this
                  ▼
┌─────────────────────────────────────────┐
│  Jint Engine (pure C#, no browser)      │
│  Sees 'document' → calls your C# type  │
└─────────────────┬───────────────────────┘
                  │  delegates to
                  ▼
┌─────────────────────────────────────────┐
│  BrowserApi types (Document, Element,   │
│  CssStyleDeclaration, etc.)             │
│  Records what happened, or builds HTML  │
└─────────────────────────────────────────┘
```

## Concrete Use Cases

### 1. Fast Integration Tests (No Browser)

Today, testing that your Blazor component correctly sets `display: flex` on a div requires launching a headless browser. With a runtime:

```csharp
var engine = new BrowserEngine();  // Jint + BrowserApi types
engine.Execute(@"
    var card = document.createElement('div');
    card.className = 'card';
    card.style.display = 'flex';
    card.style.gap = '1rem';
    document.body.appendChild(card);
");

var card = engine.Document.QuerySelector(".card");
Assert.Equal(Display.Flex, card.Style.Display);
Assert.Equal(Length.Rem(1), card.Style.Gap);
```

Runs in milliseconds. No Chrome. No Playwright. No flakiness. Pure C# test.

### 2. Server-Side Rendering (SSR)

Render a page on the server by executing JS against a virtual DOM, then serialize to HTML:

```csharp
var engine = new BrowserEngine();
engine.Execute(componentScript);  // JS that builds DOM
var html = engine.Document.Body.ToHtml();  // serialize to string
// Send pre-rendered HTML to the client
```

This is how frameworks like jsdom work in Node.js — but entirely in .NET.

### 3. User-Defined Scripts (Sandboxed)

Let festival admins write custom logic in MitWare. Example: a script that auto-assigns shift colors based on rules:

```javascript
// Admin writes this in a script editor in the app
for (var shift of shifts) {
    if (shift.team === 'Security') {
        shift.element.style.backgroundColor = 'hsl(0, 70%, 50%)';
    } else if (shift.duration > 6) {
        shift.element.style.backgroundColor = 'hsl(40, 90%, 50%)';
    }
}
```

Jint executes this safely with memory limits and timeouts. The script can't access the filesystem, network, or anything you don't explicitly expose. If it runs too long, it gets killed.

### 4. Running JS Libraries Without a Browser

Need a JS library that has no C# equivalent? Run it in Jint:

```csharp
var engine = new BrowserEngine();
engine.Execute(markdownLibrarySource);  // load the JS library
engine.Execute("var html = marked.parse('# Hello World');");
var html = engine.GetValue("html").AsString();
```

No Node.js sidecar. No HTTP calls to a rendering service. Just pure .NET.

### 5. JS → C# Migration Helper

Parse existing JavaScript with Acornima (Jint's parser), analyze what browser APIs it uses, and suggest BrowserApi C# equivalents:

```
Input:  document.getElementById('foo').style.display = 'none';
Output: Suggest → Document.GetElementById("foo").Style.Display = Display.None;
```

A developer tool that helps migrate JS-heavy code to typed C#.

## Key Dependencies

| Package | Purpose | Downloads | Status |
|---------|---------|-----------|--------|
| [Jint](https://github.com/sebastienros/jint) | JS interpreter | 26M | Active (ES2025, updated daily) |
| [Acornima](https://github.com/adams85/acornima) | JS parser (used by Jint) | 4M | Active (Jint's dependency) |

Both are pure .NET, BSD-licensed, zero native dependencies. Same author ecosystem (Sebastien Ros / Microsoft ASP.NET team).

Note: `esprima-dotnet` (same author) is the predecessor to Acornima and is now in maintenance mode. Acornima is the active successor.

## Package Structure

```
BrowserApi.Runtime              ← new optional package
    depends on: BrowserApi      ← core types
    depends on: Jint            ← JS interpreter
```

Does NOT depend on JSInterop or Blazor. Completely standalone.

## Why This Is Worth Remembering

The core BrowserApi types (DOM, CSS, Events) are just data structures. They don't care *who* is driving them. Today, a real browser drives them via JSInterop. But those same types can also be driven by:

- **Jint** (server-side JS execution) ← this plan
- **Test code** (assertions on DOM state)
- **A build tool** (generating HTML/CSS)

The runtime package just adds one more driver. The investment in typed browser APIs pays off in multiple directions — that's the whole point of separating types from transport.
