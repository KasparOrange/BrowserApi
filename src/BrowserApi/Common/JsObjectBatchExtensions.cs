namespace BrowserApi.Common;

public static class JsObjectBatchExtensions {
    public static async Task BatchAsync(this JsObject target, System.Action<JsBatchScope> action) {
        var batch = new JsBatch();
        var scope = new JsBatchScope(batch, target);
        action(scope);
        await batch.ExecuteAsync();
    }
}
