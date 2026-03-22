using System.Collections.Concurrent;
using System.Reflection;
using BrowserApi.Common;

namespace BrowserApi.Dom;

/// <summary>
/// Provides strongly-typed extension methods for DOM querying and element creation.
/// </summary>
/// <remarks>
/// <para>
/// These extensions wrap the standard DOM <c>querySelector</c> and <c>createElement</c> APIs,
/// adding generic type parameters so the caller receives the correct C# type without manual casting.
/// </para>
/// <para>
/// Tag names for <see cref="CreateElement{T}"/> are derived automatically from the C# class name
/// (or its <see cref="JsNameAttribute"/>) using the standard HTML/SVG prefix-stripping convention:
/// <c>HTMLInputElement</c> becomes <c>input</c>, <c>SVGSVGElement</c> becomes <c>svg</c>, and so on.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query for a specific element type
/// var input = document.QuerySelector&lt;HtmlInputElement&gt;("#username");
///
/// // Create a typed element without specifying a tag name string
/// var canvas = document.CreateElement&lt;HtmlCanvasElement&gt;();
/// </code>
/// </example>
/// <seealso cref="BulkQueryExtensions"/>
/// <seealso cref="Element"/>
/// <seealso cref="Document"/>
public static class DomExtensions {
    /// <summary>
    /// Executes a CSS selector query on the given <see cref="Element"/> and returns the first
    /// matching descendant cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The expected element type (e.g., <see cref="HtmlInputElement"/>, <see cref="HtmlDivElement"/>).
    /// Must derive from <see cref="Element"/> and have a parameterless constructor.
    /// </typeparam>
    /// <param name="element">The root element to query within.</param>
    /// <param name="selectors">A CSS selector string (e.g., <c>"div.active"</c>, <c>"#myId"</c>).</param>
    /// <returns>
    /// The first matching element cast to <typeparamref name="T"/>, or <see langword="null"/> if
    /// no element matches the selector.
    /// </returns>
    /// <example>
    /// <code>
    /// var link = container.QuerySelector&lt;HtmlAnchorElement&gt;("a.nav-link");
    /// if (link is not null)
    ///     Console.WriteLine(link.Href);
    /// </code>
    /// </example>
    public static T? QuerySelector<T>(this Element element, string selectors) where T : Element, new() {
        var raw = JsObject.Backend.Invoke<object?>(element.Handle, "querySelector", [selectors]);
        return JsObject.ConvertFromJs<T?>(raw);
    }

    /// <summary>
    /// Executes a CSS selector query on the given <see cref="Document"/> and returns the first
    /// matching element cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The expected element type. Must derive from <see cref="Element"/> and have a parameterless constructor.
    /// </typeparam>
    /// <param name="document">The document to query within.</param>
    /// <param name="selectors">A CSS selector string.</param>
    /// <returns>
    /// The first matching element cast to <typeparamref name="T"/>, or <see langword="null"/> if
    /// no element matches.
    /// </returns>
    /// <example>
    /// <code>
    /// var heading = document.QuerySelector&lt;HtmlHeadingElement&gt;("h1.title");
    /// </code>
    /// </example>
    public static T? QuerySelector<T>(this Document document, string selectors) where T : Element, new() {
        var raw = JsObject.Backend.Invoke<object?>(document.Handle, "querySelector", [selectors]);
        return JsObject.ConvertFromJs<T?>(raw);
    }

    /// <summary>
    /// Creates a new DOM element of the type represented by <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The element type to create. The HTML tag name is inferred from the class name or its
    /// <see cref="JsNameAttribute"/>. For example, <c>HtmlInputElement</c> creates an <c>&lt;input&gt;</c>.
    /// </typeparam>
    /// <param name="document">The document that will own the new element.</param>
    /// <returns>A new, unattached element of type <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// The tag name is derived by stripping the <c>HTML</c> or <c>SVG</c> prefix and the <c>Element</c>
    /// suffix, then lowercasing. Results are cached per type for performance.
    /// </remarks>
    /// <example>
    /// <code>
    /// var div = document.CreateElement&lt;HtmlDivElement&gt;();
    /// document.Body.AppendChild(div);
    /// </code>
    /// </example>
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
