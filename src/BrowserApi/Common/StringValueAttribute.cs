namespace BrowserApi.Common;

/// <summary>
/// Specifies the original string value from a WebIDL enum definition for a C# enum field.
/// </summary>
/// <remarks>
/// <para>
/// WebIDL enums are defined as sets of string values (e.g., <c>enum ScrollBehavior { "auto", "smooth" }</c>).
/// When the code generator maps these to C# enums, it creates PascalCase field names and
/// attaches this attribute to preserve the original string value.
/// </para>
/// <para>
/// The interop layer uses this attribute in two directions:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>C# to JS:</b> When an enum value is passed to JavaScript, <see cref="JsObject.ConvertToJs"/>
///       reads this attribute to emit the correct string (e.g., <c>"smooth"</c> instead of <c>"Smooth"</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>JS to C#:</b> When a string comes back from JavaScript, <see cref="JsObject.ConvertFromJs{T}"/>
///       matches it against the <see cref="Value"/> of each field to find the correct enum member.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public enum ScrollBehavior {
///     [StringValue("auto")]
///     Auto,
///
///     [StringValue("smooth")]
///     Smooth,
/// }
///
/// // Retrieving the string value:
/// var css = ScrollBehavior.Smooth.ToStringValue(); // "smooth"
/// </code>
/// </example>
/// <seealso cref="StringValueExtensions"/>
/// <seealso cref="JsObject"/>
[AttributeUsage(AttributeTargets.Field)]
public sealed class StringValueAttribute : Attribute {
    /// <summary>
    /// Gets the original WebIDL string value associated with this enum field.
    /// </summary>
    /// <value>
    /// The exact string as it appears in the WebIDL specification
    /// (e.g., <c>"flex-start"</c>, <c>"no-repeat"</c>, <c>"smooth"</c>).
    /// </value>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringValueAttribute"/> class
    /// with the specified WebIDL string value.
    /// </summary>
    /// <param name="value">
    /// The original string value from the WebIDL enum definition.
    /// This value is used for serialization to and deserialization from JavaScript.
    /// </param>
    public StringValueAttribute(string value) {
        Value = value;
    }
}
