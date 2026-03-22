using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Runtime.VirtualDom;
using Jint;

namespace BrowserApi.Runtime;

public sealed class BrowserEngine : IDisposable {
    private readonly Engine _jintEngine;

    public VirtualDocument VirtualDocument { get; }
    public VirtualConsole VirtualConsole { get; }
    public JintBackend Backend { get; }
    public Document Document { get; }

    public BrowserEngine() {
        VirtualDocument = new VirtualDocument();
        VirtualConsole = new VirtualConsole();
        Backend = new JintBackend(VirtualDocument, VirtualConsole);

        JsObject.Backend = Backend;

        Document = new Document { Handle = new JsHandle(VirtualDocument) };

        _jintEngine = new Engine();
        JintHostObjects.Register(_jintEngine, VirtualDocument, VirtualConsole);
    }

    public void Execute(string script) {
        _jintEngine.Execute(script);
    }

    public object? Evaluate(string expression) {
        return _jintEngine.Evaluate(expression).ToObject();
    }

    public T? Evaluate<T>(string expression) {
        var result = _jintEngine.Evaluate(expression).ToObject();
        if (result is T t) return t;
        if (result is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
            return (T)Convert.ChangeType(result, typeof(T));
        return default;
    }

    public void Dispose() {
        _jintEngine.Dispose();
    }
}
