namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Represents a text node in the virtual DOM, holding a string of character data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VirtualTextNode"/> models a DOM text node (nodeType = 3). It contains raw text
/// content via the <see cref="Data"/> property and has no child nodes. It is created by
/// <see cref="VirtualDocument.CreateTextNode"/> or by setting <see cref="VirtualNode.TextContent"/>
/// on a parent element.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var doc = new VirtualDocument();
/// var text = doc.CreateTextNode("Hello, world!");
/// doc.Body.AppendChild(text);
///
/// Console.WriteLine(doc.Body.TextContent); // "Hello, world!"
/// </code>
/// </example>
/// <seealso cref="VirtualNode"/>
/// <seealso cref="VirtualElement"/>
/// <seealso cref="VirtualDocument.CreateTextNode"/>
public class VirtualTextNode : VirtualNode {
    /// <summary>
    /// Gets or sets the character data (text content) of this text node.
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// Gets the DOM node type constant for text nodes (<c>3</c>).
    /// </summary>
    public override int NodeType => 3;

    /// <summary>
    /// Gets the node name for text nodes (<c>"#text"</c>).
    /// </summary>
    public override string NodeName => "#text";

    /// <summary>
    /// Gets or sets the text content. For text nodes, this is equivalent to <see cref="Data"/>.
    /// </summary>
    public override string TextContent {
        get => Data;
        set => Data = value;
    }

    /// <summary>
    /// Initializes a new <see cref="VirtualTextNode"/> with the specified text data.
    /// </summary>
    /// <param name="data">The initial text content of the node.</param>
    public VirtualTextNode(string data) {
        Data = data;
    }

    /// <inheritdoc/>
    public override object? GetJsProperty(string jsName) {
        return jsName switch {
            "data" => Data,
            "length" => Data.Length,
            _ => base.GetJsProperty(jsName)
        };
    }

    /// <inheritdoc/>
    public override void SetJsProperty(string jsName, object? value) {
        switch (jsName) {
            case "data": Data = value?.ToString() ?? ""; break;
            default: base.SetJsProperty(jsName, value); break;
        }
    }
}
