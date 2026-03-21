using Microsoft.JSInterop;

namespace BrowserApi.Tests.Blazor;

public sealed class MockJSInProcessRuntime : IJSInProcessRuntime {
    public List<CallRecord> Calls { get; } = [];
    public Dictionary<string, object?> ReturnValues { get; } = new();

    public TValue Invoke<TValue>(string identifier, params object?[]? args) {
        Calls.Add(new CallRecord(identifier, args ?? []));
        if (ReturnValues.TryGetValue(identifier, out var value))
            return value is TValue t ? t : default!;
        return default!;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) {
        Calls.Add(new CallRecord(identifier, args ?? []));
        if (ReturnValues.TryGetValue(identifier, out var value))
            return ValueTask.FromResult(value is TValue t ? t : default!);
        return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) {
        return InvokeAsync<TValue>(identifier, args);
    }

    public record CallRecord(string Identifier, object?[] Args);
}
