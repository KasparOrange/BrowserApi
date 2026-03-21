using BrowserApi.Common;

namespace BrowserApi.Tests.Common;

public sealed class MockBrowserBackend : IBrowserBackend {
    public List<CallRecord> Calls { get; } = [];

    public Dictionary<string, object?> PropertyValues { get; } = new();
    public object? InvokeReturnValue { get; set; }
    public object? InvokeAsyncReturnValue { get; set; }

    public T GetProperty<T>(JsHandle target, string propertyName) {
        Calls.Add(new CallRecord("GetProperty", propertyName, []));
        if (PropertyValues.TryGetValue(propertyName, out var value))
            return value is T t ? t : default!;
        return default!;
    }

    public void SetProperty(JsHandle target, string propertyName, object? value) {
        Calls.Add(new CallRecord("SetProperty", propertyName, [value]));
        PropertyValues[propertyName] = value;
    }

    public void InvokeVoid(JsHandle target, string methodName, object?[] args) {
        Calls.Add(new CallRecord("InvokeVoid", methodName, args));
    }

    public T Invoke<T>(JsHandle target, string methodName, object?[] args) {
        Calls.Add(new CallRecord("Invoke", methodName, args));
        return InvokeReturnValue is T t ? t : default!;
    }

    public Task InvokeVoidAsync(JsHandle target, string methodName, object?[] args) {
        Calls.Add(new CallRecord("InvokeVoidAsync", methodName, args));
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(JsHandle target, string methodName, object?[] args) {
        Calls.Add(new CallRecord("InvokeAsync", methodName, args));
        return Task.FromResult(InvokeAsyncReturnValue is T t ? t : default!);
    }

    public JsHandle Construct(string jsClassName, object?[] args) {
        Calls.Add(new CallRecord("Construct", jsClassName, args));
        return new JsHandle(new object());
    }

    public JsHandle GetGlobal(string name) {
        Calls.Add(new CallRecord("GetGlobal", name, []));
        return new JsHandle(new object());
    }

    public ValueTask DisposeHandle(JsHandle handle) {
        Calls.Add(new CallRecord("DisposeHandle", "", []));
        return ValueTask.CompletedTask;
    }

    public JsHandle AddEventListener(JsHandle target, string eventName, Action<JsHandle> callback) {
        Calls.Add(new CallRecord("AddEventListener", eventName, [callback]));
        return new JsHandle(new object());
    }

    public void RemoveEventListener(JsHandle target, string eventName, JsHandle listenerId) {
        Calls.Add(new CallRecord("RemoveEventListener", eventName, [listenerId]));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public record CallRecord(string Method, string Name, object?[] Args);
}
