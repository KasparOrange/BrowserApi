using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Runtime.VirtualDom;
using Jint;

namespace BrowserApi.Runtime;

/// <summary>
/// A self-contained, server-side browser engine that combines a Jint JavaScript runtime with a
/// virtual DOM, enabling BrowserApi code and JavaScript to run without a real browser.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BrowserEngine"/> is the primary entry point for the <c>BrowserApi.Runtime</c>
/// package. On construction it:
/// </para>
/// <list type="number">
///   <item><description>Creates a <see cref="VirtualDocument"/> with a standard HTML structure.</description></item>
///   <item><description>Creates a <see cref="VirtualConsole"/> for capturing log output.</description></item>
///   <item><description>Sets up a <see cref="JintBackend"/> as the active <see cref="JsObject.Backend"/>, so all BrowserApi types operate against the virtual DOM.</description></item>
///   <item><description>Initializes a Jint <see cref="Engine"/> with host objects for <c>document</c> and <c>console</c>.</description></item>
/// </list>
/// <para>
/// Use <see cref="Execute"/> to run JavaScript statements and <see cref="Evaluate{T}"/> to
/// evaluate expressions and retrieve results. The <see cref="Document"/> property provides a
/// BrowserApi <see cref="Dom.Document"/> instance wired to the virtual DOM, so C# and JavaScript
/// code see the same DOM tree.
/// </para>
/// <para>
/// This class implements <see cref="IDisposable"/> and should be disposed when no longer needed
/// to release the Jint engine resources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var engine = new BrowserEngine();
///
/// // Use JavaScript to manipulate the DOM
/// engine.Execute(@"
///     var div = document.createElement('div');
///     div.id = 'app';
///     div.textContent = 'Hello from JS!';
///     document.body.appendChild(div);
/// ");
///
/// // Read the result from C#
/// var app = engine.VirtualDocument.GetElementById("app");
/// Console.WriteLine(app?.TextContent); // "Hello from JS!"
///
/// // Or use BrowserApi C# types directly
/// var el = engine.Document.QuerySelector("#app");
///
/// // Check console output
/// engine.Execute("console.log('test message');");
/// Assert.Equal("test message", engine.VirtualConsole.Messages[0].Text);
///
/// // Evaluate expressions
/// var sum = engine.Evaluate&lt;double&gt;("2 + 2");
/// Console.WriteLine(sum); // 4
/// </code>
/// </example>
/// <seealso cref="JintBackend"/>
/// <seealso cref="VirtualDocument"/>
/// <seealso cref="VirtualConsole"/>
public sealed class BrowserEngine : IDisposable {
    private readonly Engine _jintEngine;

    /// <summary>
    /// Gets the virtual DOM document backing this engine. Use this for direct inspection
    /// of the DOM tree in tests.
    /// </summary>
    public VirtualDocument VirtualDocument { get; }

    /// <summary>
    /// Gets the virtual console that captures all <c>console.log</c>, <c>console.error</c>,
    /// <c>console.warn</c>, and <c>console.info</c> calls. Use this to assert on console output
    /// in tests.
    /// </summary>
    public VirtualConsole VirtualConsole { get; }

    /// <summary>
    /// Gets the <see cref="JintBackend"/> that bridges BrowserApi types to the virtual DOM.
    /// </summary>
    public JintBackend Backend { get; }

    /// <summary>
    /// Gets a BrowserApi <see cref="Dom.Document"/> instance wired to the virtual DOM, allowing
    /// standard DOM operations via the BrowserApi type system.
    /// </summary>
    public Document Document { get; }

    /// <summary>
    /// Initializes a new <see cref="BrowserEngine"/> with a fresh virtual DOM, virtual console,
    /// Jint JavaScript engine, and BrowserApi backend wiring.
    /// </summary>
    public BrowserEngine() {
        VirtualDocument = new VirtualDocument();
        VirtualConsole = new VirtualConsole();
        Backend = new JintBackend(VirtualDocument, VirtualConsole);

        JsObject.Backend = Backend;

        Document = new Document { Handle = new JsHandle(VirtualDocument) };

        _jintEngine = new Engine();
        JintHostObjects.Register(_jintEngine, VirtualDocument, VirtualConsole);
    }

    /// <summary>
    /// Executes JavaScript source code as statements (no return value).
    /// </summary>
    /// <param name="script">The JavaScript code to execute.</param>
    /// <remarks>
    /// The script runs in the Jint engine with <c>document</c> and <c>console</c> available as
    /// global objects. DOM modifications made in JavaScript are reflected in
    /// <see cref="VirtualDocument"/>.
    /// </remarks>
    public void Execute(string script) {
        _jintEngine.Execute(script);
    }

    /// <summary>
    /// Evaluates a JavaScript expression and returns the result as a CLR object.
    /// </summary>
    /// <param name="expression">The JavaScript expression to evaluate (e.g., <c>"2 + 2"</c>).</param>
    /// <returns>
    /// The evaluation result converted to a CLR object, or <see langword="null"/> if the
    /// expression evaluates to <c>undefined</c> or <c>null</c>.
    /// </returns>
    public object? Evaluate(string expression) {
        return _jintEngine.Evaluate(expression).ToObject();
    }

    /// <summary>
    /// Evaluates a JavaScript expression and returns the result cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type. Numeric conversions are handled automatically via <see cref="IConvertible"/>.</typeparam>
    /// <param name="expression">The JavaScript expression to evaluate.</param>
    /// <returns>
    /// The evaluation result as <typeparamref name="T"/>, or the default value of
    /// <typeparamref name="T"/> if the result cannot be converted.
    /// </returns>
    /// <example>
    /// <code>
    /// using var engine = new BrowserEngine();
    /// var result = engine.Evaluate&lt;double&gt;("Math.PI");
    /// Console.WriteLine(result); // 3.141592653589793
    /// </code>
    /// </example>
    public T? Evaluate<T>(string expression) {
        var result = _jintEngine.Evaluate(expression).ToObject();
        if (result is T t) return t;
        if (result is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
            return (T)Convert.ChangeType(result, typeof(T));
        return default;
    }

    /// <summary>
    /// Releases the Jint JavaScript engine resources.
    /// </summary>
    public void Dispose() {
        _jintEngine.Dispose();
    }
}
