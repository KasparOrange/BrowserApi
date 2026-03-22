using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Dom;

[Collection("JsObject")]
public class CanvasExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly HtmlCanvasElement _canvas;

    public CanvasExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _canvas = new HtmlCanvasElement { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void GetContext2D_calls_getContext_with_2d() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        var ctx = _canvas.GetContext2D();

        Assert.NotNull(ctx);
        Assert.IsType<CanvasRenderingContext2D>(ctx);
        var call = Assert.Single(_mock.Calls, c => c.Name == "getContext");
        Assert.Equal("2d", call.Args[0]);
    }

    [Fact]
    public void GetContextWebGL_calls_getContext_with_webgl() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        var gl = _canvas.GetContextWebGL();

        Assert.NotNull(gl);
        var call = Assert.Single(_mock.Calls, c => c.Name == "getContext");
        Assert.Equal("webgl", call.Args[0]);
    }

    [Fact]
    public void GetContextWebGL2_calls_getContext_with_webgl2() {
        _mock.InvokeReturnValue = new JsHandle(new object());

        var gl2 = _canvas.GetContextWebGL2();

        Assert.NotNull(gl2);
        var call = Assert.Single(_mock.Calls, c => c.Name == "getContext");
        Assert.Equal("webgl2", call.Args[0]);
    }
}
