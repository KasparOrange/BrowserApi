namespace BrowserApi.Common;

/// <summary>
/// Represents a CSS value that can be serialized to its CSS string representation.
/// </summary>
/// <remarks>
/// <para>
/// All CSS value types (e.g., <c>Length</c>, <c>CssColor</c>, <c>Percentage</c>) implement
/// this interface so they can be rendered into valid CSS syntax. The generated
/// <c>CssStyleDeclaration</c> properties accept <see cref="ICssValue"/> implementations,
/// and the interop layer calls <see cref="ToCss"/> automatically when passing values
/// across the JS boundary (see <see cref="JsObject.ConvertToJs"/>).
/// </para>
/// <para>
/// Implement this interface on hand-written or generated CSS value structs. The returned
/// string must be a valid CSS component value (e.g., <c>"1.5rem"</c>, <c>"rgb(255, 0, 0)"</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public readonly struct Length : ICssValue {
///     private readonly double _value;
///     private readonly string _unit;
///
///     public string ToCss() => $"{_value}{_unit}";
/// }
///
/// // Usage:
/// Assert.Equal("1.5rem", Length.Rem(1.5).ToCss());
/// </code>
/// </example>
/// <seealso cref="IWebIdlSerializable"/>
/// <seealso cref="JsObject"/>
public interface ICssValue {
    /// <summary>
    /// Serializes this value to its CSS string representation.
    /// </summary>
    /// <returns>
    /// A valid CSS component value string, such as <c>"16px"</c>, <c>"#ff0000"</c>,
    /// or <c>"calc(100% - 2rem)"</c>.
    /// </returns>
    string ToCss();
}
