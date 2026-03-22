namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Represents a virtual DOM document node, providing the root of a virtual DOM tree with
/// standard document-level operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VirtualDocument"/> models the browser <c>document</c> object. It automatically
/// creates a standard HTML structure (<c>&lt;html&gt;</c>, <c>&lt;head&gt;</c>, <c>&lt;body&gt;</c>)
/// on construction.
/// </para>
/// <para>
/// It provides factory methods for creating elements and text nodes, as well as query methods
/// (<see cref="GetElementById"/>, <see cref="QuerySelector"/>, <see cref="QuerySelectorAll"/>).
/// These are also accessible through the <see cref="IVirtualNode"/> interface for use by
/// <see cref="JintBackend"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var doc = new VirtualDocument();
///
/// var div = doc.CreateElement("div");
/// div.Id = "app";
/// div.TextContent = "Hello, Virtual DOM!";
/// doc.Body.AppendChild(div);
///
/// var found = doc.GetElementById("app");
/// Console.WriteLine(found?.TextContent); // "Hello, Virtual DOM!"
/// </code>
/// </example>
/// <seealso cref="VirtualElement"/>
/// <seealso cref="VirtualNode"/>
/// <seealso cref="BrowserEngine"/>
public class VirtualDocument : VirtualNode {
    /// <summary>
    /// Gets the root <c>&lt;html&gt;</c> element of the document.
    /// </summary>
    public VirtualElement DocumentElement { get; }

    /// <summary>
    /// Gets the <c>&lt;head&gt;</c> element of the document.
    /// </summary>
    public VirtualElement Head { get; }

    /// <summary>
    /// Gets the <c>&lt;body&gt;</c> element of the document.
    /// </summary>
    public VirtualElement Body { get; }

    /// <summary>
    /// Gets the DOM node type constant for document nodes (<c>9</c>).
    /// </summary>
    public override int NodeType => 9;

    /// <summary>
    /// Gets the node name for document nodes (<c>"#document"</c>).
    /// </summary>
    public override string NodeName => "#document";

    /// <summary>
    /// Initializes a new <see cref="VirtualDocument"/> with a default HTML structure
    /// consisting of <c>&lt;html&gt;</c>, <c>&lt;head&gt;</c>, and <c>&lt;body&gt;</c> elements.
    /// </summary>
    public VirtualDocument() {
        DocumentElement = new VirtualElement("html");
        Head = new VirtualElement("head");
        Body = new VirtualElement("body");
        DocumentElement.AppendChild(Head);
        DocumentElement.AppendChild(Body);
        AppendChild(DocumentElement);
    }

    /// <summary>
    /// Creates a new <see cref="VirtualElement"/> with the specified tag name, not yet attached
    /// to the document tree.
    /// </summary>
    /// <param name="tagName">The HTML tag name (e.g., <c>"div"</c>, <c>"span"</c>).</param>
    /// <returns>A new, detached <see cref="VirtualElement"/>.</returns>
    public VirtualElement CreateElement(string tagName) => new(tagName);

    /// <summary>
    /// Creates a new <see cref="VirtualTextNode"/> with the specified text data, not yet attached
    /// to the document tree.
    /// </summary>
    /// <param name="data">The text content of the node.</param>
    /// <returns>A new, detached <see cref="VirtualTextNode"/>.</returns>
    public VirtualTextNode CreateTextNode(string data) => new(data);

    /// <summary>
    /// Finds the first element in the document tree with the specified <c>id</c> attribute value.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>
    /// The matching <see cref="VirtualElement"/>, or <see langword="null"/> if no element has the given ID.
    /// </returns>
    public VirtualElement? GetElementById(string id) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .FirstOrDefault(el => el.Id == id);
    }

    /// <summary>
    /// Finds the first element in the document tree matching the given CSS selector.
    /// </summary>
    /// <param name="selector">
    /// A simple CSS selector string (tag, <c>#id</c>, <c>.class</c>, or compound combinations).
    /// </param>
    /// <returns>
    /// The first matching <see cref="VirtualElement"/>, or <see langword="null"/> if no match is found.
    /// </returns>
    public VirtualElement? QuerySelector(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .FirstOrDefault(el => SimpleSelector.Matches(el, selector));
    }

    /// <summary>
    /// Finds all elements in the document tree matching the given CSS selector.
    /// </summary>
    /// <param name="selector">A simple CSS selector string.</param>
    /// <returns>A list of all matching <see cref="VirtualElement"/> instances in document order.</returns>
    public List<VirtualElement> QuerySelectorAll(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .Where(el => SimpleSelector.Matches(el, selector))
            .ToList();
    }

    /// <inheritdoc/>
    public override object? GetJsProperty(string jsName) {
        return jsName switch {
            "documentElement" => DocumentElement,
            "body" => Body,
            "head" => Head,
            _ => base.GetJsProperty(jsName)
        };
    }

    /// <inheritdoc/>
    public override object? InvokeJsMethod(string jsName, object?[] args) {
        return jsName switch {
            "createElement" => CreateElement((string)args[0]!),
            "createTextNode" => CreateTextNode((string)args[0]!),
            "getElementById" => GetElementById((string)args[0]!),
            "querySelector" => QuerySelector((string)args[0]!),
            "querySelectorAll" => QuerySelectorAll((string)args[0]!),
            _ => base.InvokeJsMethod(jsName, args)
        };
    }
}
