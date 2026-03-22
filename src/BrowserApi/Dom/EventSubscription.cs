using BrowserApi.Common;

namespace BrowserApi.Dom;

public sealed class EventSubscription : IDisposable {
    private readonly JsHandle _targetHandle;
    private readonly string _eventName;
    private readonly JsHandle _listenerId;
    private bool _disposed;

    internal EventSubscription(JsHandle targetHandle, string eventName, JsHandle listenerId) {
        _targetHandle = targetHandle;
        _eventName = eventName;
        _listenerId = listenerId;
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        JsObject.Backend.RemoveEventListener(_targetHandle, _eventName, _listenerId);
    }
}
