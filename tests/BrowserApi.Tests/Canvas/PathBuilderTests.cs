using BrowserApi.Canvas;
using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Canvas;

[Collection("JsObject")]
public class PathBuilderTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly CanvasRenderingContext2D _ctx;

    public PathBuilderTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _ctx = new CanvasRenderingContext2D { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void Fluent_chain_calls_correct_methods() {
        _ctx.Path()
            .MoveTo(10, 10)
            .LineTo(100, 10)
            .LineTo(100, 100)
            .ClosePath()
            .Fill();

        Assert.Contains(_mock.Calls, c => c.Name == "beginPath");
        Assert.Contains(_mock.Calls, c => c.Name == "moveTo");
        Assert.Contains(_mock.Calls, c => c.Name == "lineTo");
        Assert.Contains(_mock.Calls, c => c.Name == "closePath");
        Assert.Contains(_mock.Calls, c => c.Name == "fill");
    }

    [Fact]
    public void Stroke_terminal_calls_stroke() {
        _ctx.Path()
            .Rect(0, 0, 50, 50)
            .Stroke();

        Assert.Contains(_mock.Calls, c => c.Name == "rect");
        Assert.Contains(_mock.Calls, c => c.Name == "stroke");
    }

    [Fact]
    public void Clip_terminal_calls_clip() {
        _ctx.Path()
            .Arc(50, 50, 25, 0, Math.PI * 2)
            .Clip();

        Assert.Contains(_mock.Calls, c => c.Name == "arc");
        Assert.Contains(_mock.Calls, c => c.Name == "clip");
    }

    [Fact]
    public void QuadraticCurveTo_delegates_to_context() {
        _ctx.Path().QuadraticCurveTo(10, 20, 30, 40);
        Assert.Contains(_mock.Calls, c => c.Name == "quadraticCurveTo");
    }

    [Fact]
    public void BezierCurveTo_delegates_to_context() {
        _ctx.Path().BezierCurveTo(10, 20, 30, 40, 50, 60);
        Assert.Contains(_mock.Calls, c => c.Name == "bezierCurveTo");
    }

    [Fact]
    public void ArcTo_delegates_to_context() {
        _ctx.Path().ArcTo(10, 20, 30, 40, 5);
        Assert.Contains(_mock.Calls, c => c.Name == "arcTo");
    }

    [Fact]
    public void Ellipse_delegates_to_context() {
        _ctx.Path().Ellipse(50, 50, 30, 20, 0, 0, Math.PI * 2);
        Assert.Contains(_mock.Calls, c => c.Name == "ellipse");
    }
}
