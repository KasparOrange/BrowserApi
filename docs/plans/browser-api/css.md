# CSS

**Parent:** [browser-api.md](browser-api.md)

## Purpose

Typed CSS properties, values, selectors, and stylesheet generation. The largest hand-written ergonomic surface in the project — generated property declarations topped with beautiful factory methods and operators.

## Use Cases

- **Type-safe inline styles** — `element.Style.Display = Display.Flex` instead of magic strings
- **Server-side stylesheet generation** — build `.css` files from C# with compile-time safety
- **Email templates** — inline styles without typo risk
- **Design tokens** — define a design system as C# types, export to CSS variables

## Core Types

### ICssValue

Every CSS value type implements this:
```csharp
public interface ICssValue {
    string ToCss();
}
```

### Value Types (hand-written ergonomic layer)

| Type | Represents | Example |
|------|-----------|---------|
| `Length` | `<length>` | `Length.Rem(1.5)` → `"1.5rem"` |
| `CssColor` | `<color>` | `CssColor.Hsl(220, 90, 56)` → `"hsl(220,90%,56%)"` |
| `Duration` | `<time>` | `Duration.Ms(300)` → `"300ms"` |
| `Angle` | `<angle>` | `Angle.Deg(45)` → `"45deg"` |
| `Percentage` | `<percentage>` | `Percentage.Of(50)` → `"50%"` |
| `Resolution` | `<resolution>` | `Resolution.Dpi(96)` → `"96dpi"` |
| `Flex` | `<flex>` | `Flex.Fr(1)` → `"1fr"` |

### Composite Types

| Type | Represents | Example |
|------|-----------|---------|
| `Transform` | `<transform-function>` | `Transform.Rotate(45.Deg()).Scale(1.5)` |
| `Gradient` | `<gradient>` | `Gradient.Linear(...)` |
| `Shadow` | `<shadow>` | `Shadow.Box(0, 2.Px(), 8.Px(), CssColor.Rgba(0,0,0,0.1))` |
| `Transition` | `<transition>` | `Transition.For(Property.Opacity, 300.Ms(), Easing.EaseInOut)` |

### CssStyleDeclaration (generated)

One property per CSS property, typed with the correct value type. Generated from the 124 CSS JSON files.

```csharp
public partial class CssStyleDeclaration {
    public Display Display { get; set; }                    // enum
    public CssColor BackgroundColor { get; set; }           // CssColor
    public Length Gap { get; set; }                         // Length
    public GridTemplateColumns GridTemplateColumns { get; set; } // complex
}
```

## Scope

The 124 CSS spec JSON files define hundreds of properties. Prioritize by usage:

1. **Layout** — display, flex, grid, position, box model
2. **Visual** — color, background, border, shadow, opacity
3. **Typography** — font, text, line-height, letter-spacing
4. **Animation** — transition, animation, transform
5. **Everything else** — expand over time

## Testing

Pure TDD — every value type's `ToCss()` is tested as string in → string out:
```csharp
Assert.Equal("1.5rem", Length.Rem(1.5).ToCss());
Assert.Equal("hsl(220,90%,56%)", CssColor.Hsl(220, 90, 56).ToCss());
Assert.Equal("calc(100% - 2rem)", Length.Calc(100.Percent() - 2.Rem()).ToCss());
```
