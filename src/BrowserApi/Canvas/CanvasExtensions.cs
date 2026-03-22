using BrowserApi.Css;
using BrowserApi.Dom;

namespace BrowserApi.Canvas;

/// <summary>
/// Provides fluent extension methods for <see cref="CanvasRenderingContext2D"/>, including
/// typed fill/stroke setters, shadow and line style helpers, a fluent path builder, and
/// gradient builder entry points.
/// </summary>
/// <remarks>
/// <para>
/// All setter methods return the context to enable method chaining:
/// </para>
/// <code>
/// ctx.SetFill(CssColor.Rgb(255, 0, 0))
///    .SetLineStyle(2, CanvasLineCap.Round)
///    .SetShadow(CssColor.Named("black"), 5, 2, 2);
/// </code>
/// <para>
/// The <see cref="Path"/> method begins a new path and returns a <see cref="PathBuilder"/>
/// for fluent path construction, terminated by <c>Fill()</c>, <c>Stroke()</c>, or <c>Clip()</c>.
/// </para>
/// </remarks>
/// <seealso cref="CanvasStateScope"/>
/// <seealso cref="PathBuilder"/>
/// <seealso cref="GradientBuilder"/>
public static class CanvasExtensions {
    /// <summary>
    /// Saves the current canvas state and returns a <see cref="CanvasStateScope"/> that restores
    /// it when disposed.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <returns>
    /// A <see cref="CanvasStateScope"/> that calls <see cref="CanvasRenderingContext2D.Restore"/>
    /// on disposal.
    /// </returns>
    /// <example>
    /// <code>
    /// using (ctx.SaveState()) {
    ///     ctx.GlobalAlpha = 0.5;
    ///     ctx.FillRect(0, 0, 100, 100);
    /// }
    /// // GlobalAlpha is restored to its previous value here.
    /// </code>
    /// </example>
    public static CanvasStateScope SaveState(this CanvasRenderingContext2D ctx) =>
        new(ctx);

    // ── Typed fill style setters ────────────────────────────────────────

    /// <summary>
    /// Sets the fill style to a <see cref="CssColor"/>, serialized to its CSS string representation.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="color">The fill color.</param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetFill(this CanvasRenderingContext2D ctx, CssColor color) {
        ctx.FillStyle = color.ToCss();
        return ctx;
    }

    /// <summary>
    /// Sets the fill style to a <see cref="CanvasGradient"/>.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="gradient">The gradient to use as the fill style.</param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetFill(this CanvasRenderingContext2D ctx, CanvasGradient gradient) {
        ctx.FillStyle = gradient;
        return ctx;
    }

    /// <summary>
    /// Sets the fill style to a <see cref="CanvasPattern"/>.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="pattern">The pattern to use as the fill style.</param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetFill(this CanvasRenderingContext2D ctx, CanvasPattern pattern) {
        ctx.FillStyle = pattern;
        return ctx;
    }

    // ── Typed stroke style setters ──────────────────────────────────────

    /// <summary>
    /// Sets the stroke style to a <see cref="CssColor"/>, serialized to its CSS string representation.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="color">The stroke color.</param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetStroke(this CanvasRenderingContext2D ctx, CssColor color) {
        ctx.StrokeStyle = color.ToCss();
        return ctx;
    }

    /// <summary>
    /// Sets the stroke style to a <see cref="CanvasGradient"/>.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="gradient">The gradient to use as the stroke style.</param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetStroke(this CanvasRenderingContext2D ctx, CanvasGradient gradient) {
        ctx.StrokeStyle = gradient;
        return ctx;
    }

    /// <summary>
    /// Sets the stroke style to a <see cref="CanvasPattern"/>.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="pattern">The pattern to use as the stroke style.</param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetStroke(this CanvasRenderingContext2D ctx, CanvasPattern pattern) {
        ctx.StrokeStyle = pattern;
        return ctx;
    }

    // ── Shadow convenience ──────────────────────────────────────────────

    /// <summary>
    /// Configures the shadow effect for subsequent draw operations in a single call.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="color">The shadow color.</param>
    /// <param name="blur">The shadow blur radius in pixels. Larger values produce softer shadows.</param>
    /// <param name="offsetX">The horizontal offset of the shadow in pixels. Defaults to 0.</param>
    /// <param name="offsetY">The vertical offset of the shadow in pixels. Defaults to 0.</param>
    /// <returns>The same context for method chaining.</returns>
    /// <example>
    /// <code>
    /// ctx.SetShadow(CssColor.Rgba(0, 0, 0, 0.5), blur: 10, offsetX: 3, offsetY: 3)
    ///    .FillRect(50, 50, 200, 100);
    /// </code>
    /// </example>
    public static CanvasRenderingContext2D SetShadow(this CanvasRenderingContext2D ctx, CssColor color, double blur, double offsetX = 0, double offsetY = 0) {
        ctx.ShadowColor = color.ToCss();
        ctx.ShadowBlur = blur;
        ctx.ShadowOffsetX = offsetX;
        ctx.ShadowOffsetY = offsetY;
        return ctx;
    }

    // ── Line style convenience ──────────────────────────────────────────

    /// <summary>
    /// Configures line width, cap, and join styles for subsequent stroke operations in a single call.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="width">The line width in pixels.</param>
    /// <param name="cap">
    /// Optional line cap style (<c>Butt</c>, <c>Round</c>, or <c>Square</c>).
    /// When <see langword="null"/>, the current cap style is left unchanged.
    /// </param>
    /// <param name="join">
    /// Optional line join style (<c>Miter</c>, <c>Round</c>, or <c>Bevel</c>).
    /// When <see langword="null"/>, the current join style is left unchanged.
    /// </param>
    /// <returns>The same context for method chaining.</returns>
    public static CanvasRenderingContext2D SetLineStyle(this CanvasRenderingContext2D ctx, double width, CanvasLineCap? cap = null, CanvasLineJoin? join = null) {
        ctx.LineWidth = width;
        if (cap.HasValue) ctx.LineCap = cap.Value;
        if (join.HasValue) ctx.LineJoin = join.Value;
        return ctx;
    }

    // ── Fluent path entry point ─────────────────────────────────────────

    /// <summary>
    /// Begins a new path and returns a <see cref="PathBuilder"/> for fluent path construction.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <returns>A <see cref="PathBuilder"/> that wraps the context's path operations.</returns>
    /// <remarks>
    /// This calls <see cref="CanvasRenderingContext2D.BeginPath"/> internally. Use the returned
    /// builder's fluent methods to define the path, then call <c>Fill()</c>, <c>Stroke()</c>,
    /// or <c>Clip()</c> to finalize.
    /// </remarks>
    /// <example>
    /// <code>
    /// ctx.Path()
    ///    .MoveTo(10, 10)
    ///    .LineTo(100, 10)
    ///    .LineTo(55, 80)
    ///    .ClosePath()
    ///    .Fill();
    /// </code>
    /// </example>
    public static PathBuilder Path(this CanvasRenderingContext2D ctx) {
        ctx.BeginPath();
        return new PathBuilder(ctx);
    }

    // ── Gradient builder entry points ───────────────────────────────────

    /// <summary>
    /// Creates a linear gradient and returns a <see cref="GradientBuilder"/> for adding color stops.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="x0">The x-coordinate of the gradient start point.</param>
    /// <param name="y0">The y-coordinate of the gradient start point.</param>
    /// <param name="x1">The x-coordinate of the gradient end point.</param>
    /// <param name="y1">The y-coordinate of the gradient end point.</param>
    /// <returns>A <see cref="GradientBuilder"/> for fluent color stop configuration.</returns>
    /// <example>
    /// <code>
    /// var gradient = ctx.LinearGradient(0, 0, 200, 0)
    ///     .AddStop(0, CssColor.Named("red"))
    ///     .AddStop(1, CssColor.Named("blue"))
    ///     .Build();
    /// ctx.SetFill(gradient).FillRect(0, 0, 200, 100);
    /// </code>
    /// </example>
    public static GradientBuilder LinearGradient(this CanvasRenderingContext2D ctx, double x0, double y0, double x1, double y1) =>
        new(ctx.CreateLinearGradient(x0, y0, x1, y1));

    /// <summary>
    /// Creates a radial gradient and returns a <see cref="GradientBuilder"/> for adding color stops.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="x0">The x-coordinate of the start circle center.</param>
    /// <param name="y0">The y-coordinate of the start circle center.</param>
    /// <param name="r0">The radius of the start circle.</param>
    /// <param name="x1">The x-coordinate of the end circle center.</param>
    /// <param name="y1">The y-coordinate of the end circle center.</param>
    /// <param name="r1">The radius of the end circle.</param>
    /// <returns>A <see cref="GradientBuilder"/> for fluent color stop configuration.</returns>
    public static GradientBuilder RadialGradient(this CanvasRenderingContext2D ctx, double x0, double y0, double r0, double x1, double y1, double r1) =>
        new(ctx.CreateRadialGradient(x0, y0, r0, x1, y1, r1));

    /// <summary>
    /// Creates a conic gradient and returns a <see cref="GradientBuilder"/> for adding color stops.
    /// </summary>
    /// <param name="ctx">The 2D rendering context.</param>
    /// <param name="startAngle">The angle (in radians) at which the gradient begins.</param>
    /// <param name="x">The x-coordinate of the gradient center.</param>
    /// <param name="y">The y-coordinate of the gradient center.</param>
    /// <returns>A <see cref="GradientBuilder"/> for fluent color stop configuration.</returns>
    public static GradientBuilder ConicGradient(this CanvasRenderingContext2D ctx, double startAngle, double x, double y) =>
        new(ctx.CreateConicGradient(startAngle, x, y));
}
