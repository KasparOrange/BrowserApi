using BrowserApi.Common;
using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime;

public sealed class JintBackend : IBrowserBackend {
    private readonly VirtualDocument _document;
    private readonly VirtualConsole _console;

    public JintBackend(VirtualDocument document, VirtualConsole console) {
        _document = document;
        _console = console;
    }

    public T GetProperty<T>(JsHandle target, string propertyName) {
        if (target.Value is IVirtualNode node) {
            var result = node.GetJsProperty(propertyName);
            return ConvertResult<T>(result);
        }
        return default!;
    }

    public void SetProperty(JsHandle target, string propertyName, object? value) {
        if (target.Value is IVirtualNode node)
            node.SetJsProperty(propertyName, value);
    }

    public void InvokeVoid(JsHandle target, string methodName, object?[] args) {
        if (target.Value is IVirtualNode node)
            node.InvokeJsMethod(methodName, UnwrapArgs(args));
    }

    public T Invoke<T>(JsHandle target, string methodName, object?[] args) {
        if (target.Value is IVirtualNode node) {
            var result = node.InvokeJsMethod(methodName, UnwrapArgs(args));
            return ConvertResult<T>(result);
        }
        return default!;
    }

    public Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args) {
        InvokeVoid(target, methodName, args);
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args) {
        return Task.FromResult(Invoke<T>(target, methodName, args));
    }

    public JsHandle Construct(string jsClassName, object?[] args) {
        return new JsHandle(new object());
    }

    public JsHandle GetGlobal(string name) {
        return name switch {
            "window" or "document" => new JsHandle(_document),
            "console" => new JsHandle(_console),
            "browserApi" => new JsHandle(new VirtualBrowserApi(this)),
            _ => new JsHandle(null)
        };
    }

    public ValueTask DisposeHandle(JsHandle handle) => ValueTask.CompletedTask;

    public JsHandle AddEventListener(JsHandle target, string eventName, System.Action<JsHandle> callback) {
        return new JsHandle(new object());
    }

    public void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId) { }

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
