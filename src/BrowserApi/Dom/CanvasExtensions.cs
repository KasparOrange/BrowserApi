using BrowserApi.Common;
using BrowserApi.WebGl;

namespace BrowserApi.Dom;

public static class CanvasExtensions {
    public static CanvasRenderingContext2D GetContext2D(this HtmlCanvasElement canvas) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["2d"]);
        return JsObject.ConvertFromJs<CanvasRenderingContext2D>(raw);
    }

    public static CanvasRenderingContext2D GetContext2D(this HtmlCanvasElement canvas, CanvasRenderingContext2Dsettings settings) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["2d", settings]);
        return JsObject.ConvertFromJs<CanvasRenderingContext2D>(raw);
    }

    public static WebGlRenderingContext GetContextWebGL(this HtmlCanvasElement canvas) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["webgl"]);
        return JsObject.ConvertFromJs<WebGlRenderingContext>(raw);
    }

    public static WebGl2RenderingContext GetContextWebGL2(this HtmlCanvasElement canvas) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["webgl2"]);
        return JsObject.ConvertFromJs<WebGl2RenderingContext>(raw);
    }
}
