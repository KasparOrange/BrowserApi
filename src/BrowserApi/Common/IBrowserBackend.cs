namespace BrowserApi.Common;

public interface IBrowserBackend : IAsyncDisposable {
    T GetProperty<T>(JsHandle target, string propertyName);
    void SetProperty(JsHandle target, string propertyName, object? value);

    void InvokeVoid(JsHandle target, string methodName, object?[] args);
    T Invoke<T>(JsHandle target, string methodName, object?[] args);

    Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args);
    Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args);

    JsHandle Construct(string jsClassName, object?[] args);
    JsHandle GetGlobal(string name);

    ValueTask DisposeHandle(JsHandle handle);

    JsHandle AddEventListener(JsHandle target, string eventName, Action<JsHandle> callback);
    void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId);
}
