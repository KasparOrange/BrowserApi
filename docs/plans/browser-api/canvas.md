# Canvas

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed Canvas 2D rendering context. Generated from `html.idl` (CanvasRenderingContext2D) with hand-written fluent extensions.

## Use Cases

- **Type-safe canvas drawing** via JS interop
- **Fluent API** — method chaining for paths, transforms, drawing operations
- **Resource management** — `using` blocks for save/restore

## Key Interfaces

| WebIDL | C# Class | Key Members |
|--------|----------|-------------|
| `CanvasRenderingContext2D` | `CanvasRenderingContext2D` | `FillRect`, `StrokePath`, `DrawImage`, etc. |
| `Path2D` | `Path2D` | `AddPath`, `MoveTo`, `LineTo`, `Arc`, etc. |
| `CanvasGradient` | `CanvasGradient` | `AddColorStop` |
| `CanvasPattern` | `CanvasPattern` | `SetTransform` |
| `TextMetrics` | `TextMetrics` | `Width`, `ActualBoundingBoxAscent`, etc. |
| `ImageData` | `ImageData` | `Width`, `Height`, `Data` |

## Ergonomic Additions (hand-written)

- **Fluent path building** — `ctx.BeginPath().MoveTo(x, y).LineTo(x, y).ClosePath().Fill(color)`
- **Save/restore via using** — `using (ctx.Save()) { ... }` auto-restores
- **Typed fill/stroke** — overloads accepting `CssColor`, `CanvasGradient`, or `CanvasPattern`
- **Font builder** — `Font.Of(size: 24.Px(), family: "Inter", weight: FontWeight.Bold)`
- **Gradient builder** — `Gradient.Linear(0, 0, 200, 0).AddStop(0, Color.Red).AddStop(1, Color.Blue)`

## Scope

Phase 1: Core drawing (rect, path, text, images)
Phase 2: Gradients, patterns, compositing
Phase 3: OffscreenCanvas, ImageBitmap
