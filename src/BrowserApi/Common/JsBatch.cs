namespace BrowserApi.Common;

public sealed class JsBatch {
    private readonly Dictionary<JsHandle, int> _targetIndices = new();
    private readonly List<object?> _targetHandles = [];
    private readonly List<object> _commands = [];

    public int Count => _commands.Count;

    public void SetProperty(JsObject target, string name, object? value) {
        var idx = GetOrAddTarget(target.Handle);
        _commands.Add(new BatchCommand(idx, 0, name, JsObject.ConvertToJs(value), null));
    }

    public void InvokeVoid(JsObject target, string name, params object?[] args) {
        var idx = GetOrAddTarget(target.Handle);
        var converted = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
            converted[i] = JsObject.ConvertToJs(args[i]);
        _commands.Add(new BatchCommand(idx, 1, name, null, converted));
    }

    public async Task ExecuteAsync() {
        if (_commands.Count == 0) return;

        var browserApiHandle = JsObject.Backend.GetGlobal("browserApi");
        var targets = _targetHandles.ToArray();
        var commands = _commands.ToArray();

        await JsObject.Backend.InvokeVoidAsync(browserApiHandle, "batch", [targets, commands]);

        _commands.Clear();
        _targetIndices.Clear();
        _targetHandles.Clear();
    }

    public static async Task RunAsync(System.Action<JsBatch> action) {
        var batch = new JsBatch();
        action(batch);
        await batch.ExecuteAsync();
    }

    private int GetOrAddTarget(JsHandle handle) {
        if (_targetIndices.TryGetValue(handle, out var idx))
            return idx;
        idx = _targetHandles.Count;
        _targetIndices[handle] = idx;
        _targetHandles.Add(handle.Value);
        return idx;
    }

    internal sealed record BatchCommand(int t, int o, string n, object? v, object?[]? a);
}
