using BrowserApi.Common;

namespace BrowserApi.Dom;

public static class BulkQueryExtensions {
    // Single-property bulk query: 1 interop call → T[]
    public static async Task<T[]> QueryValuesAsync<T>(this Document document, string selector, string propertyName) {
        return await QueryValuesCore<T>(document.Handle, selector, propertyName);
    }

    public static async Task<T[]> QueryValuesAsync<T>(this Element element, string selector, string propertyName) {
        return await QueryValuesCore<T>(element.Handle, selector, propertyName);
    }

    // Multi-property bulk query: 1 interop call → Dictionary[]
    public static async Task<Dictionary<string, object?>[]> QueryPropertiesAsync(this Document document, string selector, params string[] propertyNames) {
        return await QueryPropertiesCore(document.Handle, selector, propertyNames);
    }

    public static async Task<Dictionary<string, object?>[]> QueryPropertiesAsync(this Element element, string selector, params string[] propertyNames) {
        return await QueryPropertiesCore(element.Handle, selector, propertyNames);
    }

    // Element reference bulk query: 1 interop call → Element[] with live handles
    public static async Task<Element[]> QueryElementsAsync(this Document document, string selector) {
        return await QueryElementsCore(document.Handle, selector);
    }

    public static async Task<Element[]> QueryElementsAsync(this Element element, string selector) {
        return await QueryElementsCore(element.Handle, selector);
    }

    private static async Task<T[]> QueryValuesCore<T>(JsHandle rootHandle, string selector, string propertyName) {
        var browserApi = JsObject.Backend.GetGlobal("browserApi");
        var result = await JsObject.Backend.InvokeAsync<object?>(browserApi, "queryProperty", [rootHandle.Value, selector, propertyName]);
        if (result is object?[] arr)
            return arr.Select(v => JsObject.ConvertFromJs<T>(v)).ToArray();
        return [];
    }

    private static async Task<Dictionary<string, object?>[]> QueryPropertiesCore(JsHandle rootHandle, string selector, string[] propertyNames) {
        var browserApi = JsObject.Backend.GetGlobal("browserApi");
        var result = await JsObject.Backend.InvokeAsync<object?>(browserApi, "queryProperties", [rootHandle.Value, selector, propertyNames]);
        if (result is object?[] arr) {
            return arr.Select(item => {
                if (item is IDictionary<string, object?> dict)
                    return new Dictionary<string, object?>(dict);
                return new Dictionary<string, object?>();
            }).ToArray();
        }
        return [];
    }

    private static async Task<Element[]> QueryElementsCore(JsHandle rootHandle, string selector) {
        var browserApi = JsObject.Backend.GetGlobal("browserApi");
        var result = await JsObject.Backend.InvokeAsync<object?>(browserApi, "queryElements", [rootHandle.Value, selector]);
        if (result is object?[] arr) {
            return arr.Select(item => {
                var handle = item is JsHandle h ? h : new JsHandle(item);
                return new Element { Handle = handle };
            }).ToArray();
        }
        return [];
    }
}
