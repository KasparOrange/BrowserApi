using BrowserApi.Css;
using BrowserApi.Dom;

namespace BrowserApi.Canvas;

public static class CanvasExtensions {
    public static CanvasStateScope SaveState(this CanvasRenderingContext2D ctx) =>
        new(ctx);

    // Typed fill style setters
    public static CanvasRenderingContext2D SetFill(this CanvasRenderingContext2D ctx, CssColor color) {
        ctx.FillStyle = color.ToCss();
        return ctx;
    }

    public static CanvasRenderingContext2D SetFill(this CanvasRenderingContext2D ctx, CanvasGradient gradient) {
        ctx.FillStyle = gradient;
        return ctx;
    }

    public static CanvasRenderingContext2D SetFill(this CanvasRenderingContext2D ctx, CanvasPattern pattern) {
        ctx.FillStyle = pattern;
        return ctx;
    }

    // Typed stroke style setters
    public static CanvasRenderingContext2D SetStroke(this CanvasRenderingContext2D ctx, CssColor color) {
        ctx.StrokeStyle = color.ToCss();
        return ctx;
    }

    public static CanvasRenderingContext2D SetStroke(this CanvasRenderingContext2D ctx, CanvasGradient gradient) {
        ctx.StrokeStyle = gradient;
        return ctx;
    }

    public static CanvasRenderingContext2D SetStroke(this CanvasRenderingContext2D ctx, CanvasPattern pattern) {
        ctx.StrokeStyle = pattern;
        return ctx;
    }

    // Shadow convenience
    public static CanvasRenderingContext2D SetShadow(this CanvasRenderingContext2D ctx, CssColor color, double blur, double offsetX = 0, double offsetY = 0) {
        ctx.ShadowColor = color.ToCss();
        ctx.ShadowBlur = blur;
        ctx.ShadowOffsetX = offsetX;
        ctx.ShadowOffsetY = offsetY;
        return ctx;
    }

    // Line style convenience
    public static CanvasRenderingContext2D SetLineStyle(this CanvasRenderingContext2D ctx, double width, CanvasLineCap? cap = null, CanvasLineJoin? join = null) {
        ctx.LineWidth = width;
        if (cap.HasValue) ctx.LineCap = cap.Value;
        if (join.HasValue) ctx.LineJoin = join.Value;
        return ctx;
    }

    // Fluent path entry point
    public static PathBuilder Path(this CanvasRenderingContext2D ctx) {
        ctx.BeginPath();
        return new PathBuilder(ctx);
    }

    // Gradient builder entry points
    public static GradientBuilder LinearGradient(this CanvasRenderingContext2D ctx, double x0, double y0, double x1, double y1) =>
        new(ctx.CreateLinearGradient(x0, y0, x1, y1));

    public static GradientBuilder RadialGradient(this CanvasRenderingContext2D ctx, double x0, double y0, double r0, double x1, double y1, double r1) =>
        new(ctx.CreateRadialGradient(x0, y0, r0, x1, y1, r1));

    public static GradientBuilder ConicGradient(this CanvasRenderingContext2D ctx, double startAngle, double x, double y) =>
        new(ctx.CreateConicGradient(startAngle, x, y));
}
