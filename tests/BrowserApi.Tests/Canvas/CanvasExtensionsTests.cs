using BrowserApi.Canvas;
using BrowserApi.Common;
using BrowserApi.Css;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Canvas;

[Collection("JsObject")]
public class CanvasExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly CanvasRenderingContext2D _ctx;

    public CanvasExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _ctx = new CanvasRenderingContext2D { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void SaveState_calls_save_and_restore_on_dispose() {
        using (_ctx.SaveState()) {
            // Save should have been called
            Assert.Contains(_mock.Calls, c => c.Method == "InvokeVoid" && c.Name == "save");
        }
        // Restore should have been called on dispose
        Assert.Contains(_mock.Calls, c => c.Method == "InvokeVoid" && c.Name == "restore");
    }

    [Fact]
    public void SetFill_with_CssColor_sets_fillStyle() {
        var result = _ctx.SetFill(CssColor.Red);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "fillStyle");
    }

    [Fact]
    public void SetStroke_with_CssColor_sets_strokeStyle() {
        var result = _ctx.SetStroke(CssColor.Blue);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "strokeStyle");
    }

    [Fact]
    public void SetShadow_sets_all_shadow_properties() {
        var result = _ctx.SetShadow(CssColor.Black, 5.0, 2.0, 3.0);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "shadowColor");
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "shadowBlur");
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "shadowOffsetX");
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "shadowOffsetY");
    }

    [Fact]
    public void SetLineStyle_sets_line_properties() {
        var result = _ctx.SetLineStyle(3.0, CanvasLineCap.Round, CanvasLineJoin.Bevel);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "lineWidth");
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "lineCap");
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "lineJoin");
    }

    [Fact]
    public void SetLineStyle_skips_optional_properties() {
        _ctx.SetLineStyle(2.0);

        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "lineWidth");
        Assert.DoesNotContain(_mock.Calls, c => c.Name == "lineCap");
        Assert.DoesNotContain(_mock.Calls, c => c.Name == "lineJoin");
    }

    [Fact]
    public void Path_calls_beginPath_and_returns_builder() {
        var builder = _ctx.Path();

        Assert.NotNull(builder);
        Assert.Contains(_mock.Calls, c => c.Method == "InvokeVoid" && c.Name == "beginPath");
    }

    // ── SetFill with gradient/pattern ──────────────────────────────────

    [Fact]
    public void SetFill_with_CanvasGradient_sets_fillStyle() {
        var gradient = new CanvasGradient { Handle = new JsHandle(new object()) };

        var result = _ctx.SetFill(gradient);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "fillStyle");
    }

    [Fact]
    public void SetFill_with_CanvasPattern_sets_fillStyle() {
        var pattern = new CanvasPattern { Handle = new JsHandle(new object()) };

        var result = _ctx.SetFill(pattern);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "fillStyle");
    }

    // ── SetStroke with gradient/pattern ─────────────────────────────────

    [Fact]
    public void SetStroke_with_CanvasGradient_sets_strokeStyle() {
        var gradient = new CanvasGradient { Handle = new JsHandle(new object()) };

        var result = _ctx.SetStroke(gradient);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "strokeStyle");
    }

    [Fact]
    public void SetStroke_with_CanvasPattern_sets_strokeStyle() {
        var pattern = new CanvasPattern { Handle = new JsHandle(new object()) };

        var result = _ctx.SetStroke(pattern);

        Assert.Same(_ctx, result);
        Assert.Contains(_mock.Calls, c => c.Method == "SetProperty" && c.Name == "strokeStyle");
    }

    // ── Gradient builder entry points ──────────────────────────────────

    [Fact]
    public void LinearGradient_calls_createLinearGradient_and_returns_builder() {
        var builder = _ctx.LinearGradient(0, 0, 200, 0);

        Assert.NotNull(builder);
        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "createLinearGradient");
    }

    [Fact]
    public void RadialGradient_calls_createRadialGradient_and_returns_builder() {
        var builder = _ctx.RadialGradient(100, 100, 0, 100, 100, 80);

        Assert.NotNull(builder);
        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "createRadialGradient");
    }

    [Fact]
    public void ConicGradient_calls_createConicGradient_and_returns_builder() {
        var builder = _ctx.ConicGradient(0, 100, 100);

        Assert.NotNull(builder);
        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "createConicGradient");
    }
}
