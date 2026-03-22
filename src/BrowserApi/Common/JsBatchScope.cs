namespace BrowserApi.Common;

public sealed class JsBatchScope {
    private readonly JsBatch _batch;
    private readonly JsObject _target;

    internal JsBatchScope(JsBatch batch, JsObject target) {
        _batch = batch;
        _target = target;
    }

    public JsBatchScope Set(string name, object? value) {
        _batch.SetProperty(_target, name, value);
        return this;
    }

    public JsBatchScope Call(string name, params object?[] args) {
        _batch.InvokeVoid(_target, name, args);
        return this;
    }
}
