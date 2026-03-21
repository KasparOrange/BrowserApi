using BrowserApi.Canvas;
using BrowserApi.Common;
using BrowserApi.Css;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Canvas;

[Collection("JsObject")]
public class GradientBuilderTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly CanvasRenderingContext2D _ctx;

    public GradientBuilderTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _ctx = new CanvasRenderingContext2D { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void LinearGradient_creates_builder() {
        var builder = _ctx.LinearGradient(0, 0, 200, 0);

        Assert.NotNull(builder);
        Assert.Contains(_mock.Calls, c => c.Name == "createLinearGradient");
    }

    [Fact]
    public void AddStop_with_CssColor_delegates_to_gradient() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        _ctx.LinearGradient(0, 0, 200, 0)
            .AddStop(0, CssColor.Red)
            .AddStop(1, CssColor.Blue);

        var addColorStopCalls = _mock.Calls.Where(c => c.Name == "addColorStop").ToList();
        Assert.Equal(2, addColorStopCalls.Count);
    }

    [Fact]
    public void AddStop_with_string_delegates_to_gradient() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        _ctx.LinearGradient(0, 0, 200, 0)
            .AddStop(0.5, "#ff0000");

        Assert.Contains(_mock.Calls, c => c.Name == "addColorStop");
    }

    [Fact]
    public void Build_returns_gradient() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        var gradient = _ctx.LinearGradient(0, 0, 200, 0)
            .AddStop(0, CssColor.Red)
            .Build();

        Assert.NotNull(gradient);
        Assert.IsType<CanvasGradient>(gradient);
    }

    [Fact]
    public void Implicit_conversion_to_CanvasGradient() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        CanvasGradient gradient = _ctx.LinearGradient(0, 0, 200, 0)
            .AddStop(0, CssColor.Red);

        Assert.NotNull(gradient);
    }

    [Fact]
    public void RadialGradient_creates_builder() {
        _ctx.RadialGradient(50, 50, 10, 50, 50, 100);
        Assert.Contains(_mock.Calls, c => c.Name == "createRadialGradient");
    }

    [Fact]
    public void ConicGradient_creates_builder() {
        _ctx.ConicGradient(0, 100, 100);
        Assert.Contains(_mock.Calls, c => c.Name == "createConicGradient");
    }
}
