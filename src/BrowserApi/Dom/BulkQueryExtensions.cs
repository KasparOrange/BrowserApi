using BrowserApi.Common;

namespace BrowserApi.Dom;

/// <summary>
/// Provides bulk DOM query extension methods that retrieve data from multiple elements in a
/// single interop call, dramatically reducing round-trip overhead.
/// </summary>
/// <remarks>
/// <para>
/// Standard <c>querySelectorAll</c> followed by per-element property reads requires N+1 interop
/// calls (one for the query, one per element). The methods in this class batch the work into a
/// single call, making them ideal for scenarios such as reading all <c>textContent</c> values
/// from a list of elements or collecting form field values.
/// </para>
/// <para>
/// Three query shapes are supported:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="QueryValuesAsync{T}(Document, string, string)"/> -- retrieves a single
///       property from each matching element.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="QueryPropertiesAsync(Document, string, string[])"/> -- retrieves multiple
///       named properties from each matching element into a dictionary.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="QueryElementsAsync(Document, string)"/> -- returns live
///       <see cref="Element"/> references with JS handles, ready for further interaction.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Read all list-item text in one call
/// string[] texts = await document.QueryValuesAsync&lt;string&gt;("li.todo", "textContent");
///
/// // Read multiple properties from table rows
/// var rows = await document.QueryPropertiesAsync("tr.data-row", "id", "className", "textContent");
///
/// // Get live element handles
/// Element[] buttons = await document.QueryElementsAsync("button.action");
/// </code>
/// </example>
/// <seealso cref="DomExtensions"/>
public static class BulkQueryExtensions {
    /// <summary>
    /// Queries all elements matching <paramref name="selector"/> within the <see cref="Document"/>
    /// and returns the value of <paramref name="propertyName"/> from each element.
    /// </summary>
    /// <typeparam name="T">The expected property value type (e.g., <see cref="string"/>, <see cref="double"/>).</typeparam>
    /// <param name="document">The document to query within.</param>
    /// <param name="selector">A CSS selector string identifying the target elements.</param>
    /// <param name="propertyName">The JavaScript property name to read from each element (e.g., <c>"textContent"</c>).</param>
    /// <returns>An array of property values, one per matching element, in document order.</returns>
    public static async Task<T[]> QueryValuesAsync<T>(this Document document, string selector, string propertyName) {
        return await QueryValuesCore<T>(document.Handle, selector, propertyName);
    }

    /// <summary>
    /// Queries all elements matching <paramref name="selector"/> within the given <see cref="Element"/>
    /// and returns the value of <paramref name="propertyName"/> from each element.
    /// </summary>
    /// <typeparam name="T">The expected property value type.</typeparam>
    /// <param name="element">The root element to query within.</param>
    /// <param name="selector">A CSS selector string identifying the target elements.</param>
    /// <param name="propertyName">The JavaScript property name to read from each element.</param>
    /// <returns>An array of property values, one per matching element, in document order.</returns>
    public static async Task<T[]> QueryValuesAsync<T>(this Element element, string selector, string propertyName) {
        return await QueryValuesCore<T>(element.Handle, selector, propertyName);
    }

    /// <summary>
    /// Queries all elements matching <paramref name="selector"/> within the <see cref="Document"/>
    /// and returns a dictionary of the requested property values for each element.
    /// </summary>
    /// <param name="document">The document to query within.</param>
    /// <param name="selector">A CSS selector string identifying the target elements.</param>
    /// <param name="propertyNames">
    /// One or more JavaScript property names to read from each matching element
    /// (e.g., <c>"id"</c>, <c>"className"</c>, <c>"textContent"</c>).
    /// </param>
    /// <returns>
    /// An array of dictionaries (one per matching element), each mapping property names to their values.
    /// </returns>
    public static async Task<Dictionary<string, object?>[]> QueryPropertiesAsync(this Document document, string selector, params string[] propertyNames) {
        return await QueryPropertiesCore(document.Handle, selector, propertyNames);
    }

    /// <summary>
    /// Queries all elements matching <paramref name="selector"/> within the given <see cref="Element"/>
    /// and returns a dictionary of the requested property values for each element.
    /// </summary>
    /// <param name="element">The root element to query within.</param>
    /// <param name="selector">A CSS selector string identifying the target elements.</param>
    /// <param name="propertyNames">One or more JavaScript property names to read.</param>
    /// <returns>
    /// An array of dictionaries (one per matching element), each mapping property names to their values.
    /// </returns>
    public static async Task<Dictionary<string, object?>[]> QueryPropertiesAsync(this Element element, string selector, params string[] propertyNames) {
        return await QueryPropertiesCore(element.Handle, selector, propertyNames);
    }

    /// <summary>
    /// Queries all elements matching <paramref name="selector"/> within the <see cref="Document"/>
    /// and returns live <see cref="Element"/> references with valid JS handles.
    /// </summary>
    /// <param name="document">The document to query within.</param>
    /// <param name="selector">A CSS selector string identifying the target elements.</param>
    /// <returns>
    /// An array of <see cref="Element"/> instances with live handles, ready for further
    /// property access or method invocation.
    /// </returns>
    public static async Task<Element[]> QueryElementsAsync(this Document document, string selector) {
        return await QueryElementsCore(document.Handle, selector);
    }

    /// <summary>
    /// Queries all elements matching <paramref name="selector"/> within the given <see cref="Element"/>
    /// and returns live <see cref="Element"/> references with valid JS handles.
    /// </summary>
    /// <param name="element">The root element to query within.</param>
    /// <param name="selector">A CSS selector string identifying the target elements.</param>
    /// <returns>
    /// An array of <see cref="Element"/> instances with live handles, ready for further interaction.
    /// </returns>
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
