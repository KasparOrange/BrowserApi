using BrowserApi.Common;
using BrowserApi.WebGl;

namespace BrowserApi.Dom;

/// <summary>
/// Provides extension methods on <see cref="HtmlCanvasElement"/> for obtaining typed rendering contexts.
/// </summary>
/// <remarks>
/// <para>
/// The HTML Canvas element supports multiple rendering back-ends. These extensions call the
/// underlying <c>getContext</c> method and return the result as the correct C# type, removing
/// the need for manual casting or string-based context identifiers.
/// </para>
/// <para>
/// Supported contexts:
/// <list type="bullet">
///   <item><description><see cref="CanvasRenderingContext2D"/> via <see cref="GetContext2D(HtmlCanvasElement)"/>.</description></item>
///   <item><description><see cref="WebGlRenderingContext"/> via <see cref="GetContextWebGL"/>.</description></item>
///   <item><description><see cref="WebGl2RenderingContext"/> via <see cref="GetContextWebGL2"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var canvas = document.QuerySelector&lt;HtmlCanvasElement&gt;("#myCanvas")!;
/// var ctx = canvas.GetContext2D();
/// ctx.FillRect(10, 10, 100, 50);
/// </code>
/// </example>
/// <seealso cref="BrowserApi.Canvas.CanvasExtensions"/>
/// <seealso cref="HtmlCanvasElement"/>
public static class CanvasExtensions {
    /// <summary>
    /// Gets the 2D rendering context for the canvas element using default settings.
    /// </summary>
    /// <param name="canvas">The canvas element to obtain the context from.</param>
    /// <returns>A <see cref="CanvasRenderingContext2D"/> bound to this canvas.</returns>
    /// <example>
    /// <code>
    /// var ctx = canvas.GetContext2D();
    /// ctx.FillStyle = "red";
    /// ctx.FillRect(0, 0, 200, 100);
    /// </code>
    /// </example>
    public static CanvasRenderingContext2D GetContext2D(this HtmlCanvasElement canvas) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["2d"]);
        return JsObject.ConvertFromJs<CanvasRenderingContext2D>(raw);
    }

    /// <summary>
    /// Gets the 2D rendering context for the canvas element with the specified settings.
    /// </summary>
    /// <param name="canvas">The canvas element to obtain the context from.</param>
    /// <param name="settings">
    /// Context creation settings such as <c>alpha</c>, <c>desynchronized</c>, and <c>colorSpace</c>.
    /// </param>
    /// <returns>A <see cref="CanvasRenderingContext2D"/> configured with the given settings.</returns>
    public static CanvasRenderingContext2D GetContext2D(this HtmlCanvasElement canvas, CanvasRenderingContext2Dsettings settings) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["2d", settings]);
        return JsObject.ConvertFromJs<CanvasRenderingContext2D>(raw);
    }

    /// <summary>
    /// Gets a WebGL 1.0 rendering context for the canvas element.
    /// </summary>
    /// <param name="canvas">The canvas element to obtain the context from.</param>
    /// <returns>A <see cref="WebGlRenderingContext"/> for performing WebGL 1.0 draw calls.</returns>
    public static WebGlRenderingContext GetContextWebGL(this HtmlCanvasElement canvas) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["webgl"]);
        return JsObject.ConvertFromJs<WebGlRenderingContext>(raw);
    }

    /// <summary>
    /// Gets a WebGL 2.0 rendering context for the canvas element.
    /// </summary>
    /// <param name="canvas">The canvas element to obtain the context from.</param>
    /// <returns>A <see cref="WebGl2RenderingContext"/> for performing WebGL 2.0 draw calls.</returns>
    public static WebGl2RenderingContext GetContextWebGL2(this HtmlCanvasElement canvas) {
        var raw = JsObject.Backend.Invoke<object?>(canvas.Handle, "getContext", ["webgl2"]);
        return JsObject.ConvertFromJs<WebGl2RenderingContext>(raw);
    }
}
