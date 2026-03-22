namespace BrowserApi.Common;

/// <summary>
/// Represents a WebIDL type that can be serialized to a JavaScript-compatible object
/// for transmission across the interop boundary.
/// </summary>
/// <remarks>
/// <para>
/// Types such as WebIDL dictionaries (mapped to C# record classes) implement this
/// interface so that the interop layer can convert them into plain objects that
/// JavaScript understands. When a <see cref="JsObject"/> method argument implements
/// <see cref="IWebIdlSerializable"/>, the <see cref="JsObject.ConvertToJs"/> method
/// calls <see cref="ToJs"/> to produce the serialized form.
/// </para>
/// <para>
/// Unlike <see cref="ICssValue"/>, which serializes to a CSS string, this interface
/// serializes to a structured object (typically an anonymous object or dictionary)
/// that maps directly to a JavaScript object literal.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record class RequestInit : IWebIdlSerializable {
///     public string? Method { get; set; }
///     public string? Body { get; set; }
///
///     public object ToJs() => new { method = Method, body = Body };
/// }
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="JsObject"/>
public interface IWebIdlSerializable {
    /// <summary>
    /// Converts this instance to a JavaScript-compatible object representation.
    /// </summary>
    /// <returns>
    /// A plain .NET object (typically an anonymous type, dictionary, or array) that
    /// will be passed directly to JavaScript through the interop layer. The returned
    /// object must be JSON-serializable by the underlying JS runtime.
    /// </returns>
    object ToJs();
}
