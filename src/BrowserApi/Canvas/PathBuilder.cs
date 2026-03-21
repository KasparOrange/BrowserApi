using BrowserApi.Dom;

namespace BrowserApi.Canvas;

public sealed class PathBuilder {
    private readonly CanvasRenderingContext2D _ctx;

    internal PathBuilder(CanvasRenderingContext2D ctx) {
        _ctx = ctx;
    }

    public PathBuilder MoveTo(double x, double y) {
        _ctx.MoveTo(x, y);
        return this;
    }

    public PathBuilder LineTo(double x, double y) {
        _ctx.LineTo(x, y);
        return this;
    }

    public PathBuilder ClosePath() {
        _ctx.ClosePath();
        return this;
    }

    public PathBuilder QuadraticCurveTo(double cpx, double cpy, double x, double y) {
        _ctx.QuadraticCurveTo(cpx, cpy, x, y);
        return this;
    }

    public PathBuilder BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y) {
        _ctx.BezierCurveTo(cp1x, cp1y, cp2x, cp2y, x, y);
        return this;
    }

    public PathBuilder ArcTo(double x1, double y1, double x2, double y2, double radius) {
        _ctx.ArcTo(x1, y1, x2, y2, radius);
        return this;
    }

    public PathBuilder Arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise = false) {
        _ctx.Arc(x, y, radius, startAngle, endAngle, counterclockwise);
        return this;
    }

    public PathBuilder Ellipse(double x, double y, double radiusX, double radiusY, double rotation, double startAngle, double endAngle, bool counterclockwise = false) {
        _ctx.Ellipse(x, y, radiusX, radiusY, rotation, startAngle, endAngle, counterclockwise);
        return this;
    }

    public PathBuilder Rect(double x, double y, double w, double h) {
        _ctx.Rect(x, y, w, h);
        return this;
    }

    public PathBuilder RoundRect(double x, double y, double w, double h, object? radii = null) {
        _ctx.RoundRect(x, y, w, h, radii);
        return this;
    }

    // Terminal operations
    public void Fill(CanvasFillRule? fillRule = null) => _ctx.Fill(fillRule);
    public void Stroke() => _ctx.Stroke();
    public void Clip(CanvasFillRule? fillRule = null) => _ctx.Clip(fillRule);
}
