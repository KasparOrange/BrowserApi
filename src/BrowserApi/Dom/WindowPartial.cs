namespace BrowserApi.Dom;

/// <summary>
/// Hand-written extensions to the generated <see cref="Window"/> class, providing convenient
/// accessors for browser globals.
/// </summary>
public partial class Window {
    /// <summary>
    /// Gets the browser's <see cref="BrowserApi.Console.Console"/> object, used for
    /// logging diagnostic information to the developer console.
    /// </summary>
    /// <remarks>
    /// This property maps to <c>window.console</c> in JavaScript. The returned
    /// <see cref="BrowserApi.Console.Console"/> instance provides methods such as
    /// <c>Log</c>, <c>Warn</c>, <c>Error</c>, and <c>Info</c>.
    /// </remarks>
    public BrowserApi.Console.Console Console => GetProperty<BrowserApi.Console.Console>("console");
}
