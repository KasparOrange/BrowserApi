# Canvas 2D Cookbook

This article provides practical recipes for working with the HTML Canvas 2D API through BrowserApi. Every example uses real types and methods from the library.

## Getting a Typed Context

Start by obtaining a `CanvasRenderingContext2D` from an `HtmlCanvasElement`:

```csharp
var canvas = Document.QuerySelector<HtmlCanvasElement>("#my-canvas");
var ctx = canvas.GetContext2D();
```

All subsequent drawing operations use this `ctx` instance.

## Basic Shapes

The canvas context provides three rectangle operations directly:

```csharp
// Fill a solid rectangle
ctx.SetFill(CssColor.Blue);
ctx.FillRect(10, 10, 200, 100);

// Stroke (outline) a rectangle
ctx.SetStroke(CssColor.Red);
ctx.StrokeRect(10, 10, 200, 100);

// Clear a rectangular area (erase to transparent)
ctx.ClearRect(50, 30, 100, 40);
```

The `SetFill` and `SetStroke` extension methods accept `CssColor`, `CanvasGradient`, or `CanvasPattern`, and return the context for method chaining:

```csharp
ctx.SetFill(CssColor.Rgba(0, 128, 255, 0.8))
   .SetStroke(CssColor.Black)
   .FillRect(0, 0, 300, 200);
```

## The PathBuilder

For anything beyond rectangles, use the fluent `PathBuilder` returned by `ctx.Path()`. The builder calls `BeginPath()` automatically, provides chainable path operations, and terminates with `Fill()`, `Stroke()`, or `Clip()`.

### Triangle

```csharp
ctx.SetFill(CssColor.Rgb(255, 165, 0));
ctx.Path()
   .MoveTo(150, 20)
   .LineTo(250, 180)
   .LineTo(50, 180)
   .ClosePath()
   .Fill();
```

### Circle

Use `Arc` with a full sweep (0 to 2*PI):

```csharp
ctx.SetFill(CssColor.Hsl(200, 70, 50));
ctx.Path()
   .Arc(150, 150, 80, 0, 2 * Math.PI)
   .Fill();
```

### Semi-circle

```csharp
ctx.SetStroke(CssColor.Purple);
ctx.SetLineStyle(3);
ctx.Path()
   .Arc(150, 150, 60, 0, Math.PI)
   .Stroke();
```

### Ellipse

```csharp
ctx.SetFill(CssColor.Rgba(255, 0, 128, 0.5));
ctx.Path()
   .Ellipse(200, 150, 120, 60, 0, 0, 2 * Math.PI)
   .Fill();
```

### Rounded Rectangle

```csharp
ctx.SetFill(CssColor.Hex("#4a90d9"));
ctx.Path()
   .RoundRect(20, 20, 260, 120, 15)
   .Fill();
```

### Quadratic Bezier Curve

```csharp
ctx.SetStroke(CssColor.Green);
ctx.SetLineStyle(2);
ctx.Path()
   .MoveTo(50, 200)
   .QuadraticCurveTo(150, 20, 250, 200)
   .Stroke();
```

### Cubic Bezier Curve

```csharp
ctx.SetStroke(CssColor.Red);
ctx.SetLineStyle(2);
ctx.Path()
   .MoveTo(30, 150)
   .BezierCurveTo(80, 10, 220, 290, 270, 150)
   .Stroke();
```

### Star Shape (Complex Path)

```csharp
void DrawStar(CanvasRenderingContext2D ctx, double cx, double cy, int points, double outerR, double innerR) {
    var path = ctx.Path();
    for (int i = 0; i < points * 2; i++) {
        double angle = (Math.PI / points) * i - Math.PI / 2;
        double r = (i % 2 == 0) ? outerR : innerR;
        double x = cx + Math.Cos(angle) * r;
        double y = cy + Math.Sin(angle) * r;
        if (i == 0)
            path.MoveTo(x, y);
        else
            path.LineTo(x, y);
    }
    path.ClosePath().Fill();
}

ctx.SetFill(CssColor.Rgb(255, 215, 0));
DrawStar(ctx, 150, 150, 5, 80, 35);
```

## Colors

### Setting Fill and Stroke Colors

```csharp
// Named colors
ctx.SetFill(CssColor.Red);
ctx.SetStroke(CssColor.Black);

// RGB
ctx.SetFill(CssColor.Rgb(100, 200, 50));

// RGBA (semi-transparent)
ctx.SetFill(CssColor.Rgba(0, 0, 255, 0.3));

// HSL
ctx.SetFill(CssColor.Hsl(280, 60, 50));

// Hex
ctx.SetFill(CssColor.Hex("#ff6b35"));
```

All these go through `CssColor.ToCss()` and are assigned to `ctx.FillStyle` or `ctx.StrokeStyle` as a CSS color string.

## Gradients

Use the `GradientBuilder` to create canvas gradients with fluent color stop configuration.

### Linear Gradient

```csharp
var gradient = ctx.LinearGradient(0, 0, 300, 0)
    .AddStop(0, CssColor.Red)
    .AddStop(0.5, CssColor.White)
    .AddStop(1, CssColor.Blue)
    .Build();

ctx.SetFill(gradient).FillRect(0, 0, 300, 150);
```

### Vertical Gradient

```csharp
var vGradient = ctx.LinearGradient(0, 0, 0, 200)
    .AddStop(0, CssColor.Hex("#667eea"))
    .AddStop(1, CssColor.Hex("#764ba2"))
    .Build();

ctx.SetFill(vGradient).FillRect(0, 0, 300, 200);
```

### Radial Gradient

```csharp
var radial = ctx.RadialGradient(150, 100, 10, 150, 100, 100)
    .AddStop(0, CssColor.White)
    .AddStop(0.5, CssColor.Cyan)
    .AddStop(1, CssColor.Blue)
    .Build();

ctx.SetFill(radial).FillRect(0, 0, 300, 200);
```

### Conic Gradient

```csharp
var conic = ctx.ConicGradient(0, 150, 150)
    .AddStop(0, CssColor.Red)
    .AddStop(0.25, CssColor.Yellow)
    .AddStop(0.5, CssColor.Green)
    .AddStop(0.75, CssColor.Blue)
    .AddStop(1, CssColor.Red)
    .Build();

ctx.SetFill(conic);
ctx.Path()
   .Arc(150, 150, 100, 0, 2 * Math.PI)
   .Fill();
```

Note: `GradientBuilder` supports implicit conversion to `CanvasGradient`, so you can omit `.Build()` when assigning to a variable of type `CanvasGradient`:

```csharp
CanvasGradient g = ctx.LinearGradient(0, 0, 100, 0)
    .AddStop(0, "red")
    .AddStop(1, "blue");
```

Color stops also accept raw CSS color strings via the `AddStop(double, string)` overload.

## Fonts and Text

### CanvasFont

`CanvasFont` builds CSS font shorthand strings with a fluent API:

```csharp
// Basic font
var font = CanvasFont.Of(16, "Arial");
ctx.Font = font; // "16px Arial"

// Bold
var bold = CanvasFont.Of(20, "Helvetica").Bold();
ctx.Font = bold; // "bold 20px Helvetica"

// Bold italic
var styled = CanvasFont.Of(24, "Georgia").Bold().Italic();
ctx.Font = styled; // "italic bold 24px Georgia"

// Custom weight (thin, light, etc.)
var light = CanvasFont.Of(14, "Roboto").WithWeight("300");
ctx.Font = light; // "300 14px Roboto"

// Derived fonts (immutable -- returns new instance)
var baseFont = CanvasFont.Of(16, "sans-serif");
var bigger = baseFont.WithSize(24);       // "24px sans-serif"
var serif = baseFont.WithFamily("serif");  // "16px serif"
```

`CanvasFont` has an implicit conversion to `string`, so you can assign it directly to `ctx.Font`.

### Drawing Text

```csharp
ctx.Font = CanvasFont.Of(32, "Arial").Bold();
ctx.SetFill(CssColor.Black);
ctx.FillText("Hello, Canvas!", 50, 100);

ctx.SetStroke(CssColor.Blue);
ctx.StrokeText("Outlined", 50, 150);
```

### Measuring Text

```csharp
ctx.Font = CanvasFont.Of(16, "Arial");
var metrics = ctx.MeasureText("Sample text");
double textWidth = metrics.Width;
```

## Shadows

The `SetShadow` extension configures all four shadow properties in one call:

```csharp
ctx.SetShadow(CssColor.Rgba(0, 0, 0, 0.5), blur: 10, offsetX: 3, offsetY: 3)
   .SetFill(CssColor.Hex("#4a90d9"))
   .FillRect(50, 50, 200, 100);
```

Parameters:
- `color` -- the shadow color (a `CssColor`)
- `blur` -- blur radius in pixels (larger = softer)
- `offsetX` -- horizontal offset (default 0)
- `offsetY` -- vertical offset (default 0)

To remove the shadow effect, set blur and offsets back to zero:

```csharp
ctx.SetShadow(CssColor.Transparent, blur: 0);
```

## Line Styles

The `SetLineStyle` extension configures line width, cap, and join in one call:

```csharp
// Width only
ctx.SetLineStyle(3);

// Width + cap
ctx.SetLineStyle(5, cap: CanvasLineCap.Round);

// Width + cap + join
ctx.SetLineStyle(4, cap: CanvasLineCap.Butt, join: CanvasLineJoin.Bevel);
```

Example -- drawing with different line styles:

```csharp
// Thick rounded strokes
ctx.SetStroke(CssColor.Red)
   .SetLineStyle(8, CanvasLineCap.Round, CanvasLineJoin.Round);
ctx.Path()
   .MoveTo(50, 50)
   .LineTo(150, 100)
   .LineTo(250, 50)
   .Stroke();
```

## Save/Restore with Disposable Scope

Canvas state (transforms, clips, styles) is managed as a stack. The `SaveState()` extension returns a disposable `CanvasStateScope` that saves on construction and restores on disposal:

```csharp
// Manual (without scope)
ctx.Save();
ctx.GlobalAlpha = 0.5;
ctx.FillRect(0, 0, 100, 100);
ctx.Restore();

// With disposable scope (recommended)
using (ctx.SaveState()) {
    ctx.GlobalAlpha = 0.5;
    ctx.Translate(50, 50);
    ctx.Rotate(Math.PI / 4);
    ctx.FillRect(0, 0, 100, 100);
}
// State automatically restored -- alpha, translation, rotation all reverted
```

This pattern prevents mismatched `Save`/`Restore` calls, which can corrupt the canvas state stack.

Scopes nest naturally:

```csharp
using (ctx.SaveState()) {
    ctx.Translate(200, 200);

    using (ctx.SaveState()) {
        ctx.Rotate(Math.PI / 6);
        ctx.SetFill(CssColor.Blue);
        ctx.FillRect(-25, -25, 50, 50);
    }
    // Only rotation and fill are restored; translation persists

    ctx.SetFill(CssColor.Red);
    ctx.FillRect(-25, -25, 50, 50);
}
// Everything restored
```

## Transformations

```csharp
// Translate (move the origin)
ctx.Translate(100, 50);

// Rotate (radians)
ctx.Rotate(Math.PI / 4); // 45 degrees

// Scale
ctx.Scale(2, 2); // double size
ctx.Scale(1, -1); // flip vertically

// Reset to identity
ctx.SetTransform(1, 0, 0, 1, 0, 0);
```

Always wrap transformations in a `SaveState()` scope to avoid accumulating transforms:

```csharp
using (ctx.SaveState()) {
    ctx.Translate(150, 150);
    ctx.Rotate(angle);
    ctx.FillRect(-50, -50, 100, 100); // draws centered at (150, 150)
}
```

## Recipe: Bar Chart

```csharp
void DrawBarChart(CanvasRenderingContext2D ctx, double[] values, string[] labels, int width, int height) {
    int barCount = values.Length;
    double maxValue = values.Max();
    double barWidth = (double)width / barCount * 0.8;
    double gap = (double)width / barCount * 0.2;

    // Background
    ctx.SetFill(CssColor.White);
    ctx.FillRect(0, 0, width, height);

    // Colors for bars
    CssColor[] colors = {
        CssColor.Hex("#4a90d9"), CssColor.Hex("#e74c3c"),
        CssColor.Hex("#2ecc71"), CssColor.Hex("#f39c12"),
        CssColor.Hex("#9b59b6"), CssColor.Hex("#1abc9c"),
    };

    for (int i = 0; i < barCount; i++) {
        double barHeight = (values[i] / maxValue) * (height - 60);
        double x = i * (barWidth + gap) + gap;
        double y = height - barHeight - 30;

        // Bar
        ctx.SetFill(colors[i % colors.Length]);
        ctx.FillRect(x, y, barWidth, barHeight);

        // Label
        ctx.Font = CanvasFont.Of(12, "Arial");
        ctx.SetFill(CssColor.Black);
        ctx.TextAlign = CanvasTextAlign.Center;
        ctx.FillText(labels[i], x + barWidth / 2, height - 10);

        // Value on top of bar
        ctx.Font = CanvasFont.Of(11, "Arial").Bold();
        ctx.FillText(values[i].ToString("F0"), x + barWidth / 2, y - 5);
    }
}

// Usage
double[] data = { 45, 72, 38, 91, 55 };
string[] labels = { "Mon", "Tue", "Wed", "Thu", "Fri" };
DrawBarChart(ctx, data, labels, 400, 300);
```

## Recipe: Pie Chart

```csharp
void DrawPieChart(CanvasRenderingContext2D ctx, double[] values, CssColor[] colors, double cx, double cy, double radius) {
    double total = values.Sum();
    double currentAngle = -Math.PI / 2; // Start at top

    for (int i = 0; i < values.Length; i++) {
        double sliceAngle = (values[i] / total) * 2 * Math.PI;

        ctx.SetFill(colors[i]);
        ctx.Path()
           .MoveTo(cx, cy)
           .Arc(cx, cy, radius, currentAngle, currentAngle + sliceAngle)
           .ClosePath()
           .Fill();

        // Slice border
        ctx.SetStroke(CssColor.White);
        ctx.SetLineStyle(2);
        ctx.Path()
           .MoveTo(cx, cy)
           .Arc(cx, cy, radius, currentAngle, currentAngle + sliceAngle)
           .ClosePath()
           .Stroke();

        // Label
        double midAngle = currentAngle + sliceAngle / 2;
        double labelX = cx + Math.Cos(midAngle) * (radius * 0.65);
        double labelY = cy + Math.Sin(midAngle) * (radius * 0.65);
        double percentage = (values[i] / total) * 100;

        ctx.Font = CanvasFont.Of(13, "Arial").Bold();
        ctx.TextAlign = CanvasTextAlign.Center;
        ctx.TextBaseline = CanvasTextBaseline.Middle;
        ctx.SetFill(CssColor.White);
        ctx.FillText($"{percentage:F0}%", labelX, labelY);

        currentAngle += sliceAngle;
    }
}

// Usage
double[] slices = { 35, 25, 20, 15, 5 };
CssColor[] pieColors = {
    CssColor.Hex("#e74c3c"),
    CssColor.Hex("#3498db"),
    CssColor.Hex("#2ecc71"),
    CssColor.Hex("#f39c12"),
    CssColor.Hex("#9b59b6"),
};
DrawPieChart(ctx, slices, pieColors, 200, 200, 150);
```

## Recipe: Animated Sprite

For animations, use `requestAnimationFrame` via the `Window` object and redraw each frame:

```csharp
double spriteX = 50, spriteY = 150;
double velocityX = 3, velocityY = 0;
double gravity = 0.5;
int canvasWidth = 600, canvasHeight = 400;
int spriteSize = 30;

void DrawFrame() {
    // Physics
    velocityY += gravity;
    spriteX += velocityX;
    spriteY += velocityY;

    // Bounce off edges
    if (spriteX + spriteSize > canvasWidth || spriteX < 0) {
        velocityX = -velocityX;
        spriteX = Math.Clamp(spriteX, 0, canvasWidth - spriteSize);
    }
    if (spriteY + spriteSize > canvasHeight) {
        velocityY = -velocityY * 0.8; // energy loss
        spriteY = canvasHeight - spriteSize;
    }

    // Clear and redraw
    ctx.ClearRect(0, 0, canvasWidth, canvasHeight);

    // Background
    var bg = ctx.LinearGradient(0, 0, 0, canvasHeight)
        .AddStop(0, CssColor.Hex("#87ceeb"))
        .AddStop(1, CssColor.Hex("#e0f0ff"))
        .Build();
    ctx.SetFill(bg).FillRect(0, 0, canvasWidth, canvasHeight);

    // Ground
    ctx.SetFill(CssColor.Hex("#4a7c4a"));
    ctx.FillRect(0, canvasHeight - 5, canvasWidth, 5);

    // Sprite with shadow
    using (ctx.SaveState()) {
        ctx.SetShadow(CssColor.Rgba(0, 0, 0, 0.3), blur: 8, offsetX: 3, offsetY: 3);
        ctx.SetFill(CssColor.Hex("#e74c3c"));
        ctx.Path()
           .Arc(spriteX + spriteSize / 2, spriteY + spriteSize / 2,
                spriteSize / 2, 0, 2 * Math.PI)
           .Fill();
    }

    // Request next frame
    Window.RequestAnimationFrame(_ => DrawFrame());
}

// Start the animation
DrawFrame();
```

## Recipe: Drawing on a Grid

```csharp
void DrawGrid(CanvasRenderingContext2D ctx, int width, int height, int cellSize) {
    ctx.SetStroke(CssColor.Rgba(0, 0, 0, 0.1));
    ctx.SetLineStyle(1);

    // Vertical lines
    for (int x = 0; x <= width; x += cellSize) {
        ctx.Path()
           .MoveTo(x, 0)
           .LineTo(x, height)
           .Stroke();
    }

    // Horizontal lines
    for (int y = 0; y <= height; y += cellSize) {
        ctx.Path()
           .MoveTo(0, y)
           .LineTo(width, y)
           .Stroke();
    }
}

DrawGrid(ctx, 400, 400, 20);
```

## Clipping

Use `Clip()` as the terminal operation on a `PathBuilder` to restrict subsequent drawing to the clipped region:

```csharp
using (ctx.SaveState()) {
    // Define a circular clip region
    ctx.Path()
       .Arc(150, 150, 100, 0, 2 * Math.PI)
       .Clip();

    // Everything drawn here is clipped to the circle
    var gradient = ctx.LinearGradient(50, 50, 250, 250)
        .AddStop(0, CssColor.Red)
        .AddStop(1, CssColor.Blue)
        .Build();
    ctx.SetFill(gradient).FillRect(0, 0, 300, 300);
}
// Clip region is released when the scope ends
```
