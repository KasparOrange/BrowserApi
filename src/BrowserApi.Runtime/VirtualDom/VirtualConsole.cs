namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// A virtual implementation of the browser <c>console</c> object that captures log messages
/// in memory for testing and inspection.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VirtualConsole"/> records all calls to <see cref="Log"/>, <see cref="Error"/>,
/// <see cref="Warn"/>, and <see cref="Info"/> as <see cref="ConsoleMessage"/> records in the
/// <see cref="Messages"/> list. This allows tests to assert on console output without
/// requiring a real browser.
/// </para>
/// <para>
/// It implements <see cref="IVirtualNode"/> so the <see cref="JintBackend"/> can route
/// JavaScript <c>console.log(...)</c> calls to this object.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var console = new VirtualConsole();
/// console.Log("Hello", "world");
/// console.Error("Something went wrong");
///
/// Assert.Equal(2, console.Messages.Count);
/// Assert.Equal("log", console.Messages[0].Level);
/// Assert.Equal("Hello world", console.Messages[0].Text);
/// Assert.Equal("error", console.Messages[1].Level);
/// </code>
/// </example>
/// <seealso cref="BrowserEngine"/>
/// <seealso cref="JintBackend"/>
public class VirtualConsole : IVirtualNode {
    /// <summary>
    /// Gets the list of all console messages recorded so far, in chronological order.
    /// </summary>
    public List<ConsoleMessage> Messages { get; } = [];

    /// <summary>
    /// Records a <c>log</c>-level message with the given data arguments joined by spaces.
    /// </summary>
    /// <param name="data">The values to log. Each value is converted to a string; <see langword="null"/> values become <c>"undefined"</c>.</param>
    public void Log(params object?[] data) =>
        Messages.Add(new ConsoleMessage("log", FormatArgs(data)));

    /// <summary>
    /// Records an <c>error</c>-level message with the given data arguments joined by spaces.
    /// </summary>
    /// <param name="data">The values to log.</param>
    public void Error(params object?[] data) =>
        Messages.Add(new ConsoleMessage("error", FormatArgs(data)));

    /// <summary>
    /// Records a <c>warn</c>-level message with the given data arguments joined by spaces.
    /// </summary>
    /// <param name="data">The values to log.</param>
    public void Warn(params object?[] data) =>
        Messages.Add(new ConsoleMessage("warn", FormatArgs(data)));

    /// <summary>
    /// Records an <c>info</c>-level message with the given data arguments joined by spaces.
    /// </summary>
    /// <param name="data">The values to log.</param>
    public void Info(params object?[] data) =>
        Messages.Add(new ConsoleMessage("info", FormatArgs(data)));

    /// <summary>
    /// Clears all recorded messages.
    /// </summary>
    public void Clear() => Messages.Clear();

    private static string FormatArgs(object?[] data) =>
        string.Join(" ", data.Select(d => d?.ToString() ?? "undefined"));

    /// <inheritdoc/>
    public object? GetJsProperty(string jsName) => null;

    /// <inheritdoc/>
    public void SetJsProperty(string jsName, object? value) { }

    /// <inheritdoc/>
    public object? InvokeJsMethod(string jsName, object?[] args) {
        switch (jsName) {
            case "log": Log(args); break;
            case "error": Error(args); break;
            case "warn": Warn(args); break;
            case "info": Info(args); break;
            case "clear": Clear(); break;
        }
        return null;
    }

    /// <summary>
    /// Represents a single console message with its severity level and formatted text.
    /// </summary>
    /// <param name="Level">
    /// The console method that produced this message: <c>"log"</c>, <c>"error"</c>,
    /// <c>"warn"</c>, or <c>"info"</c>.
    /// </param>
    /// <param name="Text">The space-joined string representation of the logged arguments.</param>
    public record ConsoleMessage(string Level, string Text);
}
