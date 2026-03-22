namespace BrowserApi.Common;

/// <summary>
/// Specifies the original JavaScript name for a C# type, property, method, or event
/// that was renamed during code generation to follow C# naming conventions.
/// </summary>
/// <remarks>
/// <para>
/// WebIDL uses camelCase for properties and methods (e.g., <c>getElementById</c>,
/// <c>innerHTML</c>), while C# conventions require PascalCase. The code generator
/// renames all public members to PascalCase and attaches this attribute to record
/// the original JavaScript name.
/// </para>
/// <para>
/// This attribute serves multiple purposes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Interop dispatch:</b> Generated property getters/setters and method wrappers
///       pass the <see cref="Name"/> to the <see cref="IBrowserBackend"/> so the correct
///       JavaScript member is accessed at runtime.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Tooling and documentation:</b> Consumers can inspect this attribute via
///       reflection to discover the original JavaScript API name for debugging or
///       documentation purposes.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public partial class Document : JsObject {
///     [JsName("getElementById")]
///     public Element? GetElementById(string elementId) { ... }
///
///     [JsName("innerHTML")]
///     public string InnerHtml { get; set; }
/// }
/// </code>
/// </example>
/// <seealso cref="StringValueAttribute"/>
/// <seealso cref="JsObject"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event)]
public sealed class JsNameAttribute : Attribute {
    /// <summary>
    /// Gets the original JavaScript name of the annotated member.
    /// </summary>
    /// <value>
    /// The camelCase (or otherwise cased) name as it appears in the JavaScript API
    /// (e.g., <c>"getElementById"</c>, <c>"innerHTML"</c>, <c>"addEventListener"</c>).
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsNameAttribute"/> class
    /// with the specified JavaScript name.
    /// </summary>
    /// <param name="name">
    /// The original JavaScript name of the type or member. This must exactly match
    /// the name used in the browser's JavaScript API.
    /// </param>
    public JsNameAttribute(string name) {
        Name = name;
    }
}
