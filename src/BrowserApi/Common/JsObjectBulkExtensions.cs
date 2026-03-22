namespace BrowserApi.Common;

/// <summary>
/// Provides extension methods for reading multiple properties from a <see cref="JsObject"/>
/// in a single interop call.
/// </summary>
/// <remarks>
/// <para>
/// While <see cref="JsBatch"/> optimizes <i>write</i> operations (property sets and void
/// method calls), this class optimizes <i>read</i> operations by fetching multiple property
/// values in one round-trip to JavaScript via the <c>browserApi.getProperties</c> helper.
/// </para>
/// <para>
/// This is particularly useful when you need to read several properties from the same
/// element (e.g., reading <c>offsetWidth</c>, <c>offsetHeight</c>, <c>scrollTop</c>,
/// and <c>scrollLeft</c> at once) and want to avoid N separate interop calls.
/// </para>
/// </remarks>
/// <seealso cref="JsObjectBatchExtensions"/>
/// <seealso cref="JsBatch"/>
/// <seealso cref="JsObject"/>
public static class JsObjectBulkExtensions {
    /// <summary>
    /// Reads multiple property values from the target JavaScript object in a single
    /// interop call and returns them as a dictionary.
    /// </summary>
    /// <param name="target">The JavaScript object to read properties from.</param>
    /// <param name="propertyNames">
    /// The JavaScript property names (camelCase) to read. For example:
    /// <c>"offsetWidth"</c>, <c>"offsetHeight"</c>, <c>"scrollTop"</c>.
    /// </param>
    /// <returns>
    /// A task whose result is a dictionary mapping each property name to its value.
    /// Values are returned as raw <see cref="object"/> instances; callers should cast
    /// or convert as needed. If a property does not exist on the JavaScript object,
    /// its value will be <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method calls the JavaScript <c>browserApi.getProperties</c> helper, which
    /// reads all requested properties in a single invocation and returns them as a
    /// plain object. This is significantly faster than calling
    /// <see cref="IBrowserBackend.GetProperty{T}"/> once per property.
    /// </remarks>
    /// <example>
    /// <code>
    /// var props = await element.GetPropertiesAsync("offsetWidth", "offsetHeight", "scrollTop");
    /// var width = (double)props["offsetWidth"]!;
    /// var height = (double)props["offsetHeight"]!;
    /// var scrollTop = (double)props["scrollTop"]!;
    /// </code>
    /// </example>
    // Get multiple properties in 1 interop call instead of N
    public static async Task<Dictionary<string, object?>> GetPropertiesAsync(this JsObject target, params string[] propertyNames) {
        var browserApi = JsObject.Backend.GetGlobal("browserApi");
        var result = await JsObject.Backend.InvokeAsync<object?>(browserApi, "getProperties", [target.Handle.Value, propertyNames]);
        if (result is IDictionary<string, object?> dict)
            return new Dictionary<string, object?>(dict);
        return new Dictionary<string, object?>();
    }
}
