# DOM

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed DOM interfaces — Node, Element, Document, and all HTML/SVG element classes. Generated from `dom.idl`, `html.idl`, and related specs.

## Use Cases

- **Typed DOM manipulation** via JS interop (Blazor, MAUI Hybrid)
- **Server-side HTML generation** — typed document builder
- **Test assertions** — verify DOM structure

## Key Interfaces (from WebIDL)

### Inheritance Chain

```
EventTarget
  └── Node
        ├── Document
        ├── DocumentFragment
        ├── CharacterData
        │     ├── Text
        │     └── Comment
        └── Element
              ├── HTMLElement
              │     ├── HTMLDivElement
              │     ├── HTMLSpanElement
              │     ├── HTMLInputElement
              │     ├── HTMLButtonElement
              │     ├── HTMLAnchorElement
              │     ├── HTMLImageElement
              │     ├── HTMLCanvasElement
              │     └── ... (100+ element types)
              └── SVGElement
                    └── ... (SVG element types)
```

### Core Interfaces

| WebIDL | C# Class | Key Members |
|--------|----------|-------------|
| `EventTarget` | `EventTarget` | `AddEventListener`, `RemoveEventListener` |
| `Node` | `Node` | `ChildNodes`, `ParentNode`, `AppendChild`, `RemoveChild` |
| `Element` | `Element` | `ClassName`, `GetAttribute`, `QuerySelector` |
| `HTMLElement` | `HtmlElement` | `Style`, `InnerText`, `Hidden`, `Click()` |
| `Document` | `Document` | `CreateElement`, `QuerySelector`, `Body`, `Head` |

## Generation Notes

- `html.idl` is huge (~3000 lines) — defines all HTML element interfaces
- Many interfaces use `partial interface` across multiple specs (e.g., `Document` is extended by many specs)
- The generator must merge partial interfaces from multiple `.idl` files

## Scope

Phase 1: Core hierarchy (EventTarget → Node → Element → HTMLElement) + Document
Phase 2: All HTML element classes
Phase 3: SVG elements
