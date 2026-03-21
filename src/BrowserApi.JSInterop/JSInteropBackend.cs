using BrowserApi.Common;
using Microsoft.JSInterop;

namespace BrowserApi.JSInterop;

public sealed class JSInteropBackend : IBrowserBackend {
    private readonly IJSRuntime _jsRuntime;
    private readonly IJSInProcessRuntime? _syncRuntime;

    public JSInteropBackend(IJSRuntime jsRuntime) {
        _jsRuntime = jsRuntime;
        _syncRuntime = jsRuntime as IJSInProcessRuntime;
    }

    private IJSInProcessRuntime SyncRuntime =>
        _syncRuntime ?? throw new InvalidOperationException(
            "Synchronous JS interop requires IJSInProcessRuntime (WebAssembly). Use async methods for Blazor Server.");

    public T GetProperty<T>(JsHandle target, string propertyName) {
        return SyncRuntime.Invoke<T>("browserApi.getProperty", target.Value, propertyName);
    }

    public void SetProperty(JsHandle target, string propertyName, object? value) {
        SyncRuntime.InvokeVoid("browserApi.setProperty", target.Value, propertyName, value);
    }

    public void InvokeVoid(JsHandle target, string methodName, object?[] args) {
        SyncRuntime.InvokeVoid("browserApi.invoke", target.Value, methodName, args);
    }

    public T Invoke<T>(JsHandle target, string methodName, object?[] args) {
        return SyncRuntime.Invoke<T>("browserApi.invoke", target.Value, methodName, args);
    }

    public async Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args) {
        await _jsRuntime.InvokeVoidAsync("browserApi.invokeAsync", target.Value, methodName, args);
    }

    public async Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args) {
        return await _jsRuntime.InvokeAsync<T>("browserApi.invokeAsync", target.Value, methodName, args);
    }

    public JsHandle Construct(string jsClassName, object?[] args) {
        var reference = SyncRuntime.Invoke<IJSObjectReference>("browserApi.construct", jsClassName, args);
        return new JsHandle(reference);
    }

    public JsHandle GetGlobal(string name) {
        var reference = SyncRuntime.Invoke<IJSObjectReference>("browserApi.getGlobal", name);
        return new JsHandle(reference);
    }

    public async ValueTask DisposeHandle(JsHandle handle) {
        if (handle.Value is IJSObjectReference objRef)
            await objRef.DisposeAsync();
    }

    public JsHandle AddEventListener(JsHandle target, string eventName, Action<JsHandle> callback) {
        var adapter = new EventCallbackAdapter(callback);
        var dotNetRef = DotNetObjectReference.Create(adapter);
        var listenerId = SyncRuntime.Invoke<IJSObjectReference>(
            "browserApi.addEventListener", target.Value, eventName, dotNetRef);
        return new JsHandle(listenerId);
    }

    public void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId) {
        SyncRuntime.InvokeVoid("browserApi.removeEventListener", target.Value, eventName, listenerId.Value);
    }

    public async ValueTask DisposeAsync() {
        // Nothing to dispose at the backend level
        await ValueTask.CompletedTask;
    }

    private sealed class EventCallbackAdapter {
        private readonly Action<JsHandle> _callback;

        public EventCallbackAdapter(Action<JsHandle> callback) {
            _callback = callback;
        }

        [JSInvokable]
        public void OnEvent(IJSObjectReference eventRef) {
            _callback(new JsHandle(eventRef));
        }
    }
}
