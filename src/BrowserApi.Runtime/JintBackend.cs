using BrowserApi.Common;
using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime;

/// <summary>
/// An <see cref="IBrowserBackend"/> implementation backed by the virtual DOM, enabling
/// BrowserApi types to work without a real browser.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JintBackend"/> is the bridge between BrowserApi's generated C# types (which
/// call <see cref="JsObject.Backend"/> to read/write properties and invoke methods) and the
/// in-memory <see cref="VirtualDocument"/> and <see cref="VirtualConsole"/>.
/// </para>
/// <para>
/// Property reads and writes are forwarded to <see cref="IVirtualNode.GetJsProperty"/> and
/// <see cref="IVirtualNode.SetJsProperty"/>. Method calls are forwarded to
/// <see cref="IVirtualNode.InvokeJsMethod"/>. This allows BrowserApi code to manipulate
/// a virtual DOM tree in tests without any browser or JS interop dependency.
/// </para>
/// <para>
/// Global lookups (<c>"window"</c>, <c>"document"</c>, <c>"console"</c>) are resolved to
/// the virtual DOM and console instances provided at construction time.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var doc = new VirtualDocument();
/// var console = new VirtualConsole();
/// var backend = new JintBackend(doc, console);
///
/// JsObject.Backend = backend;
///
/// // Now BrowserApi types operate against the virtual DOM
/// var document = new Document { Handle = new JsHandle(doc) };
/// var div = document.CreateElement("div");
/// </code>
/// </example>
/// <seealso cref="BrowserEngine"/>
/// <seealso cref="IBrowserBackend"/>
/// <seealso cref="VirtualDocument"/>
public sealed class JintBackend : IBrowserBackend {
    private readonly VirtualDocument _document;
    private readonly VirtualConsole _console;

    /// <summary>
    /// Initializes a new <see cref="JintBackend"/> with the specified virtual document and console.
    /// </summary>
    /// <param name="document">The virtual DOM document to serve as the backing store.</param>
    /// <param name="console">The virtual console to capture log output.</param>
    public JintBackend(VirtualDocument document, VirtualConsole console) {
        _document = document;
        _console = console;
    }

    /// <inheritdoc/>
    public T GetProperty<T>(JsHandle target, string propertyName) {
        if (target.Value is IVirtualNode node) {
            var result = node.GetJsProperty(propertyName);
            return ConvertResult<T>(result);
        }
        return default!;
    }

    /// <inheritdoc/>
    public void SetProperty(JsHandle target, string propertyName, object? value) {
        if (target.Value is IVirtualNode node)
            node.SetJsProperty(propertyName, value);
    }

    /// <inheritdoc/>
    public void InvokeVoid(JsHandle target, string methodName, object?[] args) {
        if (target.Value is IVirtualNode node)
            node.InvokeJsMethod(methodName, UnwrapArgs(args));
    }

    /// <inheritdoc/>
    public T Invoke<T>(JsHandle target, string methodName, object?[] args) {
        if (target.Value is IVirtualNode node) {
            var result = node.InvokeJsMethod(methodName, UnwrapArgs(args));
            return ConvertResult<T>(result);
        }
        return default!;
    }

    /// <inheritdoc/>
    public Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args) {
        InvokeVoid(target, methodName, args);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args) {
        return Task.FromResult(Invoke<T>(target, methodName, args));
    }

    /// <inheritdoc/>
    public JsHandle Construct(string jsClassName, object?[] args) {
        return new JsHandle(new object());
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Resolves the following global names:
    /// <list type="bullet">
    ///   <item><description><c>"window"</c> and <c>"document"</c> -- both resolve to the <see cref="VirtualDocument"/>.</description></item>
    ///   <item><description><c>"console"</c> -- resolves to the <see cref="VirtualConsole"/>.</description></item>
    ///   <item><description><c>"browserApi"</c> -- resolves to an internal virtual node for batch queries.</description></item>
    /// </list>
    /// </remarks>
    public JsHandle GetGlobal(string name) {
        return name switch {
            "window" or "document" => new JsHandle(_document),
            "console" => new JsHandle(_console),
            "browserApi" => new JsHandle(new VirtualBrowserApi(this)),
            _ => new JsHandle(null)
        };
    }

    /// <inheritdoc/>
    public ValueTask DisposeHandle(JsHandle handle) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// Event listeners are recorded but not actually triggered in the virtual DOM backend.
    /// This is a no-op implementation suitable for testing.
    /// </remarks>
    public JsHandle AddEventListener(JsHandle target, string eventName, System.Action<JsHandle> callback) {
        return new JsHandle(new object());
    }

    /// <inheritdoc/>
    public void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId) { }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static T ConvertResult<T>(object? result) {
        if (result is null) return default!;

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        // Virtual DOM nodes → wrap in JsHandle for JsObject consumption
        if (typeof(JsObject).IsAssignableFrom(targetType) && result is IVirtualNode) {
            var handle = new JsHandle(result);
            var instance = (JsObject)Activator.CreateInstance(targetType)!;
            // Use reflection to set internal Handle
            typeof(JsObject).GetProperty("Handle")!.SetValue(instance, handle);
            return (T)(object)instance;
        }

        if (result is IConvertible && typeof(IConvertible).IsAssignableFrom(targetType))
            return (T)Convert.ChangeType(result, targetType);

        if (result is T t) return t;
        return default!;
    }

    private static object?[] UnwrapArgs(object?[] args) {
        var unwrapped = new object?[args.Length];
        for (var i = 0; i < args.Length; i++) {
            unwrapped[i] = args[i] switch {
                JsHandle h => h.Value,
                _ => args[i]
            };
        }
        return unwrapped;
    }

    // Internal virtual node for "browserApi" global (batch/query support)
    private sealed class VirtualBrowserApi : IVirtualNode {
        private readonly JintBackend _backend;
        public VirtualBrowserApi(JintBackend backend) => _backend = backend;
        public object? GetJsProperty(string jsName) => null;
        public void SetJsProperty(string jsName, object? value) { }
        public object? InvokeJsMethod(string jsName, object?[] args) => null;
    }
}
