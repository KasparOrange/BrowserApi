# BrowserApi — Diagrams

## Package Dependency Graph

```
┌──────────────────────┐
│   BrowserApi.Blazor  │  ASP.NET Components
└──────────┬───────────┘
           │
┌──────────▼───────────┐
│ BrowserApi.JSInterop │  Microsoft.JSInterop
└──────────┬───────────┘
           │
┌──────────▼───────────┐
│     BrowserApi       │  Zero dependencies
└──────────────────────┘
```

## Code Generation Pipeline

```
┌─────────────────┐     ┌─────────────────┐
│  specs/idl/*.idl│     │specs/css/*.json  │
│  (337 files)    │     │  (124 files)     │
└────────┬────────┘     └────────┬─────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│  WebIDL Parser  │     │ CSS Data Parser │
└────────┬────────┘     └────────┬─────────┘
         │                       │
         └──────────┬────────────┘
                    ▼
         ┌─────────────────┐
         │   Unified AST   │
         └────────┬────────┘
                  │
      ┌───────────┼───────────┐
      ▼           ▼           ▼
┌──────────┐┌──────────┐┌──────────┐
│  Types   ││  Enums   ││   CSS    │
│ (classes,││ (WebIDL  ││Properties│
│ records) ││  enums)  ││ (typed)  │
└──────────┘└──────────┘└──────────┘
      │           │           │
      └───────────┼───────────┘
                  ▼
       src/BrowserApi/Generated/
```

## WebIDL → C# Type Mapping Flow

```
WebIDL interface          C# partial class
═══════════════          ══════════════════
interface Element    →    public partial class Element
  : Node                    : Node
{                         {
  attribute DOMString  →      public string ClassName
    className;                  { get; set; }

  readonly attribute   →      public string TagName
    DOMString tagName;          => GetProperty<string>("tagName");

  Element? closest     →      public Element? Closest(string selectors)
    (DOMString sel);            => Invoke<Element?>("closest", selectors);

  Promise<void>        →      public Task RequestFullscreenAsync()
    requestFullscreen();        => InvokeAsync("requestFullscreen");
}                         }
```

## Interop Backend Architecture

```
Consumer code
      │
      ▼
┌─────────────────────────────┐
│  Generated Type (partial)   │
│  public partial class Element│
│  {                          │
│    public string TagName    │
│      => GetProperty<>(..); ─┼──┐
│  }                          │  │
└─────────────────────────────┘  │
                                 │  calls base class method
┌─────────────────────────────┐  │
│  JsObject (base class)      │◄─┘
│  {                          │
│    T GetProperty<T>(name)   │
│    T Invoke<T>(method, ..)  │
│    Task<T> InvokeAsync(..)  │
│  }                          │
└──────────────┬──────────────┘
               │  delegates to backend
┌──────────────▼──────────────┐
│   IBrowserBackend           │
│   {                         │
│     T GetProperty<T>(..);   │
│     T Invoke<T>(..);        │
│     Task<T> InvokeAsync(..);│
│   }                         │
└──────────────┬──────────────┘
               │
    ┌──────────┴──────────┐
    ▼                     ▼
┌──────────┐      ┌──────────────┐
│JSInterop │      │NativeWasm    │
│Backend   │      │Backend       │
│(today)   │      │(future)      │
└──────────┘      └──────────────┘
```
