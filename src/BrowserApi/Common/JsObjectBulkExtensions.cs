namespace BrowserApi.Common;

public static class JsObjectBulkExtensions {
    // Get multiple properties in 1 interop call instead of N
    public static async Task<Dictionary<string, object?>> GetPropertiesAsync(this JsObject target, params string[] propertyNames) {
        var browserApi = JsObject.Backend.GetGlobal("browserApi");
        var result = await JsObject.Backend.InvokeAsync<object?>(browserApi, "getProperties", [target.Handle.Value, propertyNames]);
        if (result is IDictionary<string, object?> dict)
            return new Dictionary<string, object?>(dict);
        return new Dictionary<string, object?>();
    }
}
