using BrowserApi.Dom;

namespace BrowserApi.Canvas;

/// <summary>
/// A fluent builder for constructing canvas 2D paths using method chaining.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PathBuilder"/> wraps a <see cref="CanvasRenderingContext2D"/> and exposes
/// all standard path operations (<see cref="MoveTo"/>, <see cref="LineTo"/>, <see cref="Arc"/>,
/// <see cref="BezierCurveTo"/>, etc.) as chainable methods. The path is finalized by calling
/// one of the terminal operations: <see cref="Fill"/>, <see cref="Stroke"/>, or <see cref="Clip"/>.
/// </para>
/// <para>
/// Create instances via <see cref="CanvasExtensions.Path"/>, which automatically calls
/// <see cref="CanvasRenderingContext2D.BeginPath"/> before returning the builder.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Draw a triangle
/// ctx.Path()
///    .MoveTo(150, 50)
///    .LineTo(250, 200)
///    .LineTo(50, 200)
///    .ClosePath()
///    .Stroke();
///
/// // Draw a rounded rectangle and fill it
/// ctx.Path()
///    .RoundRect(10, 10, 180, 80, 15)
///    .Fill();
/// </code>
/// </example>
/// <seealso cref="CanvasExtensions.Path"/>
/// <seealso cref="CanvasRenderingContext2D"/>
public sealed class PathBuilder {
    private readonly CanvasRenderingContext2D _ctx;

    internal PathBuilder(CanvasRenderingContext2D ctx) {
        _ctx = ctx;
    }

    /// <summary>
    /// Moves the current point to the specified coordinates without drawing a line.
    /// </summary>
    /// <param name="x">The x-coordinate of the new position.</param>
    /// <param name="y">The y-coordinate of the new position.</param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder MoveTo(double x, double y) {
        _ctx.MoveTo(x, y);
        return this;
    }

    /// <summary>
    /// Draws a straight line from the current point to the specified coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate of the line endpoint.</param>
    /// <param name="y">The y-coordinate of the line endpoint.</param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder LineTo(double x, double y) {
        _ctx.LineTo(x, y);
        return this;
    }

    /// <summary>
    /// Closes the current sub-path by drawing a straight line back to its starting point.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder ClosePath() {
        _ctx.ClosePath();
        return this;
    }

    /// <summary>
    /// Draws a quadratic Bezier curve from the current point to (<paramref name="x"/>, <paramref name="y"/>)
    /// using (<paramref name="cpx"/>, <paramref name="cpy"/>) as the control point.
    /// </summary>
    /// <param name="cpx">The x-coordinate of the control point.</param>
    /// <param name="cpy">The y-coordinate of the control point.</param>
    /// <param name="x">The x-coordinate of the curve endpoint.</param>
    /// <param name="y">The y-coordinate of the curve endpoint.</param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder QuadraticCurveTo(double cpx, double cpy, double x, double y) {
        _ctx.QuadraticCurveTo(cpx, cpy, x, y);
        return this;
    }

    /// <summary>
    /// Draws a cubic Bezier curve from the current point to (<paramref name="x"/>, <paramref name="y"/>)
    /// using two control points.
    /// </summary>
    /// <param name="cp1x">The x-coordinate of the first control point.</param>
    /// <param name="cp1y">The y-coordinate of the first control point.</param>
    /// <param name="cp2x">The x-coordinate of the second control point.</param>
    /// <param name="cp2y">The y-coordinate of the second control point.</param>
    /// <param name="x">The x-coordinate of the curve endpoint.</param>
    /// <param name="y">The y-coordinate of the curve endpoint.</param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y) {
        _ctx.BezierCurveTo(cp1x, cp1y, cp2x, cp2y, x, y);
        return this;
    }

    /// <summary>
    /// Adds a circular arc to the path, connecting the current point to the arc via tangent lines.
    /// </summary>
    /// <param name="x1">The x-coordinate of the first tangent point.</param>
    /// <param name="y1">The y-coordinate of the first tangent point.</param>
    /// <param name="x2">The x-coordinate of the second tangent point.</param>
    /// <param name="y2">The y-coordinate of the second tangent point.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder ArcTo(double x1, double y1, double x2, double y2, double radius) {
        _ctx.ArcTo(x1, y1, x2, y2, radius);
        return this;
    }

    /// <summary>
    /// Adds a circular arc to the path centered at (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    /// <param name="x">The x-coordinate of the arc center.</param>
    /// <param name="y">The y-coordinate of the arc center.</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <param name="startAngle">The starting angle in radians, measured from the positive x-axis.</param>
    /// <param name="endAngle">The ending angle in radians.</param>
    /// <param name="counterclockwise">
    /// If <see langword="true"/>, the arc is drawn counterclockwise. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder Arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise = false) {
        _ctx.Arc(x, y, radius, startAngle, endAngle, counterclockwise);
        return this;
    }

    /// <summary>
    /// Adds an elliptical arc to the path.
    /// </summary>
    /// <param name="x">The x-coordinate of the ellipse center.</param>
    /// <param name="y">The y-coordinate of the ellipse center.</param>
    /// <param name="radiusX">The semi-major axis radius.</param>
    /// <param name="radiusY">The semi-minor axis radius.</param>
    /// <param name="rotation">The rotation of the ellipse in radians.</param>
    /// <param name="startAngle">The starting angle in radians.</param>
    /// <param name="endAngle">The ending angle in radians.</param>
    /// <param name="counterclockwise">
    /// If <see langword="true"/>, the arc is drawn counterclockwise. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder Ellipse(double x, double y, double radiusX, double radiusY, double rotation, double startAngle, double endAngle, bool counterclockwise = false) {
        _ctx.Ellipse(x, y, radiusX, radiusY, rotation, startAngle, endAngle, counterclockwise);
        return this;
    }

    /// <summary>
    /// Adds a rectangle sub-path to the current path.
    /// </summary>
    /// <param name="x">The x-coordinate of the rectangle's top-left corner.</param>
    /// <param name="y">The y-coordinate of the rectangle's top-left corner.</param>
    /// <param name="w">The width of the rectangle.</param>
    /// <param name="h">The height of the rectangle.</param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder Rect(double x, double y, double w, double h) {
        _ctx.Rect(x, y, w, h);
        return this;
    }

    /// <summary>
    /// Adds a rounded rectangle sub-path to the current path.
    /// </summary>
    /// <param name="x">The x-coordinate of the rectangle's top-left corner.</param>
    /// <param name="y">The y-coordinate of the rectangle's top-left corner.</param>
    /// <param name="w">The width of the rectangle.</param>
    /// <param name="h">The height of the rectangle.</param>
    /// <param name="radii">
    /// The corner radii. Can be a single number, an array of numbers (1, 2, 3, or 4 values
    /// following CSS <c>border-radius</c> shorthand), or <see langword="null"/> for sharp corners.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public PathBuilder RoundRect(double x, double y, double w, double h, object? radii = null) {
        _ctx.RoundRect(x, y, w, h, radii);
        return this;
    }

    // ── Terminal operations ─────────────────────────────────────────────

    /// <summary>
    /// Fills the current path using the current fill style.
    /// </summary>
    /// <param name="fillRule">
    /// Optional fill rule (<c>nonzero</c> or <c>evenodd</c>). When <see langword="null"/>,
    /// the default <c>nonzero</c> rule is used.
    /// </param>
    public void Fill(CanvasFillRule? fillRule = null) => _ctx.Fill(fillRule);

    /// <summary>
    /// Strokes (outlines) the current path using the current stroke style.
    /// </summary>
    public void Stroke() => _ctx.Stroke();

    /// <summary>
    /// Creates a clipping region from the current path. Subsequent draw operations are confined
    /// to this region.
    /// </summary>
    /// <param name="fillRule">
    /// Optional fill rule (<c>nonzero</c> or <c>evenodd</c>). When <see langword="null"/>,
    /// the default <c>nonzero</c> rule is used.
    /// </param>
    public void Clip(CanvasFillRule? fillRule = null) => _ctx.Clip(fillRule);
}
