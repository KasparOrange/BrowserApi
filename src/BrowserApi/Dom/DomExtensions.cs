using System.Collections.Concurrent;
using System.Reflection;
using BrowserApi.Common;

namespace BrowserApi.Dom;

public static class DomExtensions {
    // Typed querySelector
    public static T? QuerySelector<T>(this Element element, string selectors) where T : Element, new() {
        var raw = JsObject.Backend.Invoke<object?>(element.Handle, "querySelector", [selectors]);
        return JsObject.ConvertFromJs<T?>(raw);
    }

    public static T? QuerySelector<T>(this Document document, string selectors) where T : Element, new() {
        var raw = JsObject.Backend.Invoke<object?>(document.Handle, "querySelector", [selectors]);
        return JsObject.ConvertFromJs<T?>(raw);
    }

    // Typed createElement
    public static T CreateElement<T>(this Document document) where T : Element, new() {
        var tagName = GetTagName<T>();
        var raw = JsObject.Backend.Invoke<object?>(document.Handle, "createElement", [tagName]);
        return JsObject.ConvertFromJs<T>(raw);
    }

    private static readonly ConcurrentDictionary<Type, string> TagNameCache = new();

    private static string GetTagName<T>() where T : Element {
        return TagNameCache.GetOrAdd(typeof(T), type => {
            var jsName = type.GetCustomAttribute<JsNameAttribute>()?.Name;
            if (jsName is not null)
                return DeriveTagFromJsName(jsName);
            return DeriveTagFromJsName(type.Name);
        });
    }

    private static string DeriveTagFromJsName(string jsName) {
        // HTMLInputElement → input, HTMLDivElement → div, SVGSVGElement → svg
        var name = jsName;
        if (name.StartsWith("HTML", StringComparison.Ordinal))
            name = name[4..];
        else if (name.StartsWith("SVG", StringComparison.Ordinal))
            name = name[3..];
        if (name.EndsWith("Element", StringComparison.Ordinal))
            name = name[..^7];
        return name.ToLowerInvariant();
    }
}
