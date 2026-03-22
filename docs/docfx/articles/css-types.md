# CSS Type System

BrowserApi provides a strongly-typed CSS value system that catches errors at compile time and serializes to valid CSS strings automatically. Instead of passing raw strings like `"16px"` or `"rgb(255, 0, 0)"`, you work with value types such as `Length`, `CssColor`, and `Angle` that guarantee well-formed output.

## The ICssValue Interface

Every CSS value type in BrowserApi implements the `ICssValue` interface, defined in `BrowserApi.Common`:

```csharp
public interface ICssValue {
    string ToCss();
}
```

This single method is the serialization contract. When you assign a CSS value to a style property, the interop layer calls `ToCss()` automatically via `JsObject.ConvertToJs`:

```csharp
// Inside JsObject.ConvertToJs (called automatically by property setters):
ICssValue css => css.ToCss(),   // Length, CssColor, etc. all go through here
```

This means you never need to call `ToCss()` yourself when setting styles -- the framework does it for you. But you can call it directly when you need the CSS string for testing, logging, or composing values manually.

## Primitive Value Types

### Length

`Length` is the most commonly used CSS value type. It represents pixel values, relative units, percentages, and `calc()` expressions.

**Factory methods:**

```csharp
Length px   = Length.Px(16);        // "16px"
Length rem  = Length.Rem(1.5);      // "1.5rem"
Length em   = Length.Em(2);         // "2em"
Length vh   = Length.Vh(100);       // "100vh"
Length vw   = Length.Vw(50);        // "50vw"
Length pct  = Length.Percent(50);   // "50%"
Length calc = Length.Calc("100% - 20px"); // "calc(100% - 20px)"
```

**Special values:**

```csharp
Length auto = Length.Auto;   // "auto"
Length zero = Length.Zero;   // "0"
```

**Implicit conversions from numeric types (defaults to pixels):**

```csharp
Length margin = 16;        // equivalent to Length.Px(16) -> "16px"
Length width = 100.5;      // equivalent to Length.Px(100.5) -> "100.5px"
```

This allows you to write concise code when pixels are the intended unit:

```csharp
element.Style.MarginTop = 16;      // sets margin-top to "16px"
element.Style.Width = Length.Auto;  // sets width to "auto"
```

**Arithmetic operators produce `calc()` expressions:**

```csharp
// Addition
var total = Length.Rem(2) + Length.Px(10);
total.ToCss(); // "calc(2rem + 10px)"

// Subtraction
var remaining = Length.Percent(100) - Length.Px(20);
remaining.ToCss(); // "calc(100% - 20px)"

// Negation
var negative = -Length.Px(10);
negative.ToCss(); // "calc(-1 * 10px)"
```

This is how you build mixed-unit calculations that would require `calc()` in raw CSS.

### CssColor

`CssColor` provides named color constants, functional notations (RGB, HSL), and hex values.

**Named colors:**

```csharp
CssColor red    = CssColor.Red;
CssColor blue   = CssColor.Blue;
CssColor black  = CssColor.Black;
CssColor white  = CssColor.White;
CssColor gray   = CssColor.Gray;
CssColor orange = CssColor.Orange;
CssColor purple = CssColor.Purple;
// Also: Green, Yellow, Cyan, Magenta, Transparent
```

**CSS keywords:**

```csharp
CssColor inherit = CssColor.Inherit;         // "inherit"
CssColor current = CssColor.CurrentColor;    // "currentcolor"
CssColor clear   = CssColor.Transparent;     // "transparent"
```

**Functional notation:**

```csharp
// RGB (0-255 per channel)
CssColor custom = CssColor.Rgb(128, 0, 255);
custom.ToCss(); // "rgb(128, 0, 255)"

// RGBA with alpha (0.0 - 1.0)
CssColor semi = CssColor.Rgba(0, 0, 0, 0.5);
semi.ToCss(); // "rgba(0, 0, 0, 0.5)"

// HSL (hue 0-360, saturation 0-100, lightness 0-100)
CssColor hsl = CssColor.Hsl(200, 50, 70);
hsl.ToCss(); // "hsl(200, 50%, 70%)"

// HSLA with alpha
CssColor hsla = CssColor.Hsla(120, 100, 50, 0.75);
hsla.ToCss(); // "hsla(120, 100%, 50%, 0.75)"
```

**Hex notation:**

```csharp
CssColor hex = CssColor.Hex("#ff0080");  // "#ff0080"
CssColor shortHex = CssColor.Hex("#f08"); // "#f08"
```

The `Hex` factory validates the format -- it requires a leading `#` and exactly 3 or 6 hex digits. Invalid input throws `ArgumentException`.

### Angle

`Angle` represents rotational measurements used in transforms and gradients.

```csharp
Angle degrees  = Angle.Deg(45);      // "45deg"
Angle radians  = Angle.Rad(3.14);    // "3.14rad"
Angle gradians = Angle.Grad(100);    // "100grad"
Angle turns    = Angle.Turn(0.25);   // "0.25turn"
Angle zero     = Angle.Zero;         // "0deg"
Angle calc     = Angle.Calc("90deg + 10deg"); // "calc(90deg + 10deg)"
```

### Duration

`Duration` represents time values for transitions and animations.

```csharp
Duration fast = Duration.Ms(200);    // "200ms"
Duration slow = Duration.S(1.5);    // "1.5s"
Duration zero = Duration.Zero;      // "0s"
Duration calc = Duration.Calc("0.3s + 100ms"); // "calc(0.3s + 100ms)"
```

### Percentage

`Percentage` is used where the CSS grammar requires a standalone `<percentage>` rather than a `<length-percentage>`. (For length contexts that accept percentages, use `Length.Percent()` instead.)

```csharp
Percentage half = Percentage.Of(50);     // "50%"
Percentage full = Percentage.Of(100);    // "100%"
Percentage zero = Percentage.Zero;       // "0%"
```

### Resolution

`Resolution` represents display density, primarily used in media queries.

```csharp
Resolution standard = Resolution.Dpi(96);     // "96dpi"
Resolution retina   = Resolution.Dppx(2);     // "2dppx"
Resolution metric   = Resolution.Dpcm(300);   // "300dpcm"
```

### Flex

`Flex` represents the `fr` (fraction) unit used in CSS Grid layouts to distribute available space proportionally.

```csharp
Flex one  = Flex.Fr(1);      // "1fr"
Flex two  = Flex.Fr(2);      // "2fr"
Flex half = Flex.Fr(0.5);    // "0.5fr"
```

## Fluent Extension Methods

The `CssUnitExtensions` class adds extension methods on `int` and `double` that mirror CSS unit syntax. Import them with `using BrowserApi.Css;`.

```csharp
// Length
Length margin   = 16.Px();       // Length.Px(16) -> "16px"
Length fontSize = 1.5.Rem();     // Length.Rem(1.5) -> "1.5rem"
Length spacing  = 2.0.Em();      // Length.Em(2) -> "2em"
Length height   = 100.0.Vh();    // Length.Vh(100) -> "100vh"
Length width    = 80.0.Vw();     // Length.Vw(80) -> "80vw"

// Duration
Duration fast = 200.Ms();       // Duration.Ms(200) -> "200ms"
Duration slow = 0.5.S();        // Duration.S(0.5) -> "0.5s"

// Angle
Angle rotation = 45.Deg();      // Angle.Deg(45) -> "45deg"
Angle tilt     = 30.0.Deg();    // Angle.Deg(30) -> "30deg"

// Percentage
Percentage half = 50.Percent(); // Percentage.Of(50) -> "50%"

// Flex
Flex col = 1.Fr();              // Flex.Fr(1) -> "1fr"
Flex wide = 2.0.Fr();           // Flex.Fr(2) -> "2fr"
```

These extensions are purely cosmetic -- they delegate directly to the corresponding static factory method. Choose whichever style you find more readable.

## Complex Value Types

### Transform

`Transform` represents CSS transform functions. It supports all standard 2D transforms and provides fluent chaining via `Then` methods.

**Single transforms:**

```csharp
Transform t1 = Transform.Translate(Length.Px(10), Length.Px(20));
t1.ToCss(); // "translate(10px, 20px)"

Transform t2 = Transform.TranslateX(Length.Px(50));
t2.ToCss(); // "translateX(50px)"

Transform t3 = Transform.TranslateY(Length.Percent(100));
t3.ToCss(); // "translateY(100%)"

Transform t4 = Transform.Rotate(Angle.Deg(45));
t4.ToCss(); // "rotate(45deg)"

Transform t5 = Transform.Scale(1.5);
t5.ToCss(); // "scale(1.5)"

Transform t6 = Transform.Scale(2, 0.5);
t6.ToCss(); // "scale(2, 0.5)"

Transform t7 = Transform.SkewX(Angle.Deg(10));
t7.ToCss(); // "skewX(10deg)"

Transform t8 = Transform.Skew(Angle.Deg(10), Angle.Deg(20));
t8.ToCss(); // "skew(10deg, 20deg)"

Transform t9 = Transform.Matrix(1, 0, 0, 1, 50, 100);
t9.ToCss(); // "matrix(1, 0, 0, 1, 50, 100)"

Transform none = Transform.None;
none.ToCss(); // "none"
```

**Chaining multiple transforms:**

CSS applies transform functions in sequence. Use `Then` (or the convenience `ThenRotate`, `ThenScale`, etc.) to build them fluently:

```csharp
var chained = Transform.Translate(Length.Px(100), Length.Px(0))
    .ThenRotate(Angle.Deg(45))
    .ThenScale(1.5);
chained.ToCss(); // "translate(100px, 0) rotate(45deg) scale(1.5)"

// Other chaining methods: ThenTranslate, ThenSkewX, ThenSkewY
var complex = Transform.Rotate(Angle.Deg(10))
    .ThenTranslate(Length.Px(50), Length.Px(0))
    .ThenSkewX(Angle.Deg(5));
complex.ToCss(); // "rotate(10deg) translate(50px, 0) skewX(5deg)"
```

### Gradient

`Gradient` supports linear, radial, conic, and their repeating variants. Each factory accepts `GradientStop` values, and `CssColor` implicitly converts to `GradientStop` for convenience.

**Linear gradients:**

```csharp
// Simple (top to bottom)
var g1 = Gradient.Linear(CssColor.Red, CssColor.Blue);
g1.ToCss(); // "linear-gradient(red, blue)"

// With angle
var g2 = Gradient.Linear(Angle.Deg(45), CssColor.Red, CssColor.Blue);
g2.ToCss(); // "linear-gradient(45deg, red, blue)"

// With positioned stops
var g3 = Gradient.Linear(
    Angle.Deg(90),
    GradientStop.At(CssColor.Red, Percentage.Of(0)),
    GradientStop.At(CssColor.White, Percentage.Of(50)),
    GradientStop.At(CssColor.Blue, Percentage.Of(100)));
g3.ToCss(); // "linear-gradient(90deg, red 0%, white 50%, blue 100%)"
```

**Radial gradients:**

```csharp
// Default shape
var r1 = Gradient.Radial(CssColor.White, CssColor.Black);
r1.ToCss(); // "radial-gradient(white, black)"

// With shape descriptor
var r2 = Gradient.Radial("circle", CssColor.White, CssColor.Black);
r2.ToCss(); // "radial-gradient(circle, white, black)"
```

**Conic gradients:**

```csharp
var c1 = Gradient.Conic(CssColor.Red, CssColor.Yellow, CssColor.Green);
c1.ToCss(); // "conic-gradient(red, yellow, green)"

var c2 = Gradient.Conic(Angle.Deg(90), CssColor.Red, CssColor.Blue);
c2.ToCss(); // "conic-gradient(from 90deg, red, blue)"
```

**Repeating variants:**

```csharp
var rg = Gradient.RepeatingLinear(Angle.Deg(45),
    GradientStop.At(CssColor.Red, Length.Px(0)),
    GradientStop.At(CssColor.Blue, Length.Px(20)));
rg.ToCss(); // "repeating-linear-gradient(45deg, red 0, blue 20px)"
```

`RepeatingRadial` and `RepeatingConic` follow the same pattern.

**GradientStop:**

The `GradientStop` record struct pairs a color with an optional position:

```csharp
// Implicit conversion from CssColor (no position)
GradientStop s1 = CssColor.Red;
s1.ToCss(); // "red"

// With length position
var s2 = GradientStop.At(CssColor.Blue, Length.Px(100));
s2.ToCss(); // "blue 100px"

// With percentage position
var s3 = GradientStop.At(CssColor.Green, Percentage.Of(75));
s3.ToCss(); // "green 75%"
```

### Shadow

`Shadow` covers both `box-shadow` and `text-shadow` properties, with distinct factory methods since box shadows support `spread` and `inset` while text shadows do not.

**Box shadows:**

```csharp
var shadow = Shadow.Box(
    Length.Px(2), Length.Px(4),
    blur: Length.Px(6),
    color: CssColor.Rgba(0, 0, 0, 0.3));
shadow.ToCss(); // "2px 4px 6px rgba(0, 0, 0, 0.3)"

// With spread
var elevated = Shadow.Box(
    Length.Px(0), Length.Px(4),
    blur: Length.Px(8),
    spread: Length.Px(2),
    color: CssColor.Rgba(0, 0, 0, 0.15));
elevated.ToCss(); // "0 4px 8px 2px rgba(0, 0, 0, 0.15)"

// Inset shadow
var inset = Shadow.Box(
    Length.Px(0), Length.Px(0),
    blur: Length.Px(10),
    spread: Length.Px(2),
    color: CssColor.Blue,
    inset: true);
inset.ToCss(); // "inset 0 0 10px 2px blue"
```

**Text shadows:**

```csharp
var textShadow = Shadow.Text(
    Length.Px(1), Length.Px(1),
    blur: Length.Px(2),
    color: CssColor.Gray);
textShadow.ToCss(); // "1px 1px 2px gray"
```

**Combining multiple shadows:**

```csharp
var layered = Shadow.Combine(
    Shadow.Box(Length.Px(0), Length.Px(2), blur: Length.Px(4),
        color: CssColor.Rgba(0, 0, 0, 0.1)),
    Shadow.Box(Length.Px(0), Length.Px(4), blur: Length.Px(8),
        color: CssColor.Rgba(0, 0, 0, 0.2)));
layered.ToCss();
// "0 2px 4px rgba(0, 0, 0, 0.1), 0 4px 8px rgba(0, 0, 0, 0.2)"
```

### Transition

`Transition` defines CSS transition declarations for smooth property animations.

```csharp
// Transition a single property
var fade = Transition.For("opacity", Duration.S(0.3), Easing.EaseInOut);
fade.ToCss(); // "opacity 0.3s ease-in-out"

// Transition all properties
var all = Transition.All(Duration.Ms(200));
all.ToCss(); // "all 200ms"

// With delay
var delayed = Transition.For("transform", Duration.S(0.5), Easing.Ease, Duration.Ms(100));
delayed.ToCss(); // "transform 0.5s ease 100ms"

// Combine multiple transitions
var multi = Transition.Combine(
    Transition.For("opacity", Duration.S(0.3), Easing.EaseIn),
    Transition.For("transform", Duration.S(0.5), Easing.EaseOut));
multi.ToCss(); // "opacity 0.3s ease-in, transform 0.5s ease-out"

// Disable transitions
var none = Transition.None;
none.ToCss(); // "none"
```

### Easing

`Easing` represents CSS timing functions used with transitions and animations.

**Named keywords:**

```csharp
Easing.Ease       // "ease"
Easing.Linear     // "linear"
Easing.EaseIn     // "ease-in"
Easing.EaseOut    // "ease-out"
Easing.EaseInOut  // "ease-in-out"
Easing.StepStart  // "step-start"
Easing.StepEnd    // "step-end"
```

**Custom curves:**

```csharp
// Material Design standard curve
var material = Easing.CubicBezier(0.4, 0, 0.2, 1);
material.ToCss(); // "cubic-bezier(0.4, 0, 0.2, 1)"

// Step function
var steps = Easing.Steps(4, "jump-end");
steps.ToCss(); // "steps(4, jump-end)"

var simpleSteps = Easing.Steps(5);
simpleSteps.ToCss(); // "steps(5)"
```

## Shorthand Helpers

The generated `CssStyleDeclaration` exposes only individual longhand properties (e.g., `MarginTop`, `MarginRight`). The hand-written shorthand methods on the partial class provide the familiar CSS shorthand patterns.

### SetMargin

```csharp
// All sides equal
style.SetMargin(Length.Px(16));
// Sets MarginTop = MarginRight = MarginBottom = MarginLeft = "16px"

// Vertical / horizontal
style.SetMargin(Length.Px(8), Length.Px(16));
// Sets MarginTop = MarginBottom = "8px", MarginRight = MarginLeft = "16px"

// All four sides individually
style.SetMargin(Length.Px(10), Length.Px(20), Length.Px(10), Length.Px(20));
// Sets top=10px, right=20px, bottom=10px, left=20px
```

### SetPadding

Identical overloads to `SetMargin`:

```csharp
style.SetPadding(Length.Rem(1));                    // all sides
style.SetPadding(Length.Px(8), Length.Px(16));       // vertical / horizontal
style.SetPadding(10.Px(), 20.Px(), 10.Px(), 20.Px()); // individual (using extensions)
```

### SetGap

For CSS Grid and Flexbox gap:

```csharp
// Uniform gap
style.SetGap(Length.Px(16));
// Sets RowGap = ColumnGap = "16px"

// Row / column independently
style.SetGap(Length.Px(8), Length.Px(16));
// Sets RowGap = "8px", ColumnGap = "16px"
```

## How CSS Values Flow Through the Interop Layer

When you set a style property on an element, the value flows through these steps:

1. You assign a CSS value type to a property:
   ```csharp
   element.Style.MarginTop = Length.Rem(1.5);
   ```

2. The generated property setter calls `SetProperty("margin-top", value)`.

3. `JsObject.SetProperty` calls `ConvertToJs(value)`.

4. `ConvertToJs` pattern-matches on `ICssValue` and calls `ToCss()`:
   ```csharp
   ICssValue css => css.ToCss(),  // returns "1.5rem"
   ```

5. The string `"1.5rem"` is passed to the JavaScript backend, which sets `element.style.marginTop = "1.5rem"`.

This pipeline means every type that implements `ICssValue` works automatically with the interop layer. You get type safety in C# and valid CSS in the browser, with no manual string formatting.

## Testing CSS Values

Because all CSS value types are pure structs with no dependencies, testing is straightforward:

```csharp
// Direct assertion on ToCss()
Assert.Equal("1.5rem", Length.Rem(1.5).ToCss());
Assert.Equal("rgb(255, 128, 0)", CssColor.Rgb(255, 128, 0).ToCss());

// Equality comparison
Length fromInt = 16;  // implicit conversion to Length
Assert.Equal(Length.Px(16), fromInt);
Assert.NotEqual(Length.Px(16), Length.Rem(1));

// Calc expressions
var calc = Length.Percent(100) - Length.Px(20);
Assert.Equal("calc(100% - 20px)", calc.ToCss());
```

No browser, no JavaScript runtime, no mocking -- just pure unit tests.
