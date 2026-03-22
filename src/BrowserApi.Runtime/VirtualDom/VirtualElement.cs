using System.Text;

namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Represents an HTML element in the virtual DOM, with support for attributes, CSS styles,
/// class names, IDs, child elements, CSS selectors, and HTML serialization.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VirtualElement"/> models a real browser element node (nodeType = 1). It provides:
/// </para>
/// <list type="bullet">
///   <item><description>Attribute management via <see cref="GetAttribute"/>/<see cref="SetAttribute"/>/<see cref="RemoveAttribute"/>.</description></item>
///   <item><description>Inline styles via the <see cref="Style"/> property (a <see cref="VirtualStyle"/>).</description></item>
///   <item><description>CSS class and ID via <see cref="ClassName"/> and <see cref="Id"/>.</description></item>
///   <item><description>CSS selector querying via <see cref="QuerySelector"/> and <see cref="QuerySelectorAll"/> (using <see cref="SimpleSelector"/>).</description></item>
///   <item><description>HTML serialization via <see cref="InnerHtml"/> and <see cref="OuterHtml"/>.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var div = new VirtualElement("div");
/// div.Id = "main";
/// div.ClassName = "container active";
/// div.Style["color"] = "red";
/// div.SetAttribute("data-value", "42");
///
/// var child = new VirtualElement("span");
/// child.TextContent = "Hello";
/// div.AppendChild(child);
///
/// Console.WriteLine(div.OuterHtml);
/// // &lt;div id="main" class="container active" style="color: red"&gt;&lt;span&gt;Hello&lt;/span&gt;&lt;/div&gt;
/// </code>
/// </example>
/// <seealso cref="VirtualNode"/>
/// <seealso cref="VirtualDocument"/>
/// <seealso cref="VirtualStyle"/>
/// <seealso cref="SimpleSelector"/>
public class VirtualElement : VirtualNode {
    /// <summary>
    /// Gets the lowercase tag name of this element (e.g., <c>"div"</c>, <c>"span"</c>, <c>"input"</c>).
    /// </summary>
    public string TagName { get; }

    /// <summary>
    /// Gets or sets the <c>id</c> attribute of this element.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Gets or sets the space-separated class names of this element.
    /// </summary>
    public string ClassName { get; set; } = "";

    /// <summary>
    /// Gets the inline style object for this element, providing CSS property access.
    /// </summary>
    public VirtualStyle Style { get; } = new();

    /// <summary>
    /// Gets the dictionary of all attributes set on this element. Keys are case-insensitive.
    /// </summary>
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the DOM node type constant for element nodes (<c>1</c>).
    /// </summary>
    public override int NodeType => 1;

    /// <summary>
    /// Gets the uppercase tag name of this element, matching the DOM <c>nodeName</c> convention.
    /// </summary>
    public override string NodeName => TagName.ToUpperInvariant();

    /// <summary>
    /// Gets the child elements of this node (excluding text nodes and other non-element children).
    /// </summary>
    public IReadOnlyList<VirtualElement> Children =>
        ChildNodes.OfType<VirtualElement>().ToList();

    /// <summary>
    /// Initializes a new <see cref="VirtualElement"/> with the specified tag name.
    /// </summary>
    /// <param name="tagName">
    /// The HTML tag name (e.g., <c>"div"</c>, <c>"span"</c>). Stored in lowercase.
    /// </param>
    public VirtualElement(string tagName) {
        TagName = tagName.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the value of the attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name (case-insensitive).</param>
    /// <returns>The attribute value, or <see langword="null"/> if the attribute does not exist.</returns>
    public string? GetAttribute(string name) =>
        Attributes.TryGetValue(name, out var v) ? v : null;

    /// <summary>
    /// Sets the attribute with the specified name to the given value.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public void SetAttribute(string name, string value) =>
        Attributes[name] = value;

    /// <summary>
    /// Removes the attribute with the specified name, if it exists.
    /// </summary>
    /// <param name="name">The attribute name to remove.</param>
    public void RemoveAttribute(string name) =>
        Attributes.Remove(name);

    /// <summary>
    /// Determines whether this element has an attribute with the specified name.
    /// </summary>
    /// <param name="name">The attribute name to check for.</param>
    /// <returns><see langword="true"/> if the attribute exists; otherwise <see langword="false"/>.</returns>
    public bool HasAttribute(string name) =>
        Attributes.ContainsKey(name);

    /// <summary>
    /// Finds the first descendant element matching the given CSS selector.
    /// </summary>
    /// <param name="selector">
    /// A simple CSS selector string (tag, <c>#id</c>, <c>.class</c>, or compound combinations).
    /// Comma-separated selectors are supported.
    /// </param>
    /// <returns>
    /// The first matching <see cref="VirtualElement"/>, or <see langword="null"/> if no match is found.
    /// </returns>
    /// <remarks>
    /// The search excludes the element itself and traverses descendants in document order.
    /// Only simple selectors are supported (no combinators like <c>&gt;</c>, <c>+</c>, or <c>~</c>).
    /// </remarks>
    public VirtualElement? QuerySelector(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .Skip(1) // skip self
            .FirstOrDefault(el => SimpleSelector.Matches(el, selector));
    }

    /// <summary>
    /// Finds all descendant elements matching the given CSS selector.
    /// </summary>
    /// <param name="selector">
    /// A simple CSS selector string. Comma-separated selectors are supported.
    /// </param>
    /// <returns>A list of all matching <see cref="VirtualElement"/> instances in document order.</returns>
    public List<VirtualElement> QuerySelectorAll(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .Skip(1)
            .Where(el => SimpleSelector.Matches(el, selector))
            .ToList();
    }

    /// <summary>
    /// Gets the HTML content of this element's children as a string.
    /// </summary>
    public string InnerHtml {
        get {
            var sb = new StringBuilder();
            foreach (var child in ChildNodes)
                AppendHtml(sb, child);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets the full HTML representation of this element, including its opening tag,
    /// children, and closing tag.
    /// </summary>
    public string OuterHtml {
        get {
            var sb = new StringBuilder();
            AppendHtml(sb, this);
            return sb.ToString();
        }
    }

    private static void AppendHtml(StringBuilder sb, VirtualNode node) {
        if (node is VirtualTextNode text) {
            sb.Append(text.Data);
            return;
        }
        if (node is VirtualElement el) {
            sb.Append('<').Append(el.TagName);
            if (!string.IsNullOrEmpty(el.Id))
                sb.Append($" id=\"{el.Id}\"");
            if (!string.IsNullOrEmpty(el.ClassName))
                sb.Append($" class=\"{el.ClassName}\"");
            var cssText = el.Style.CssText;
            if (!string.IsNullOrEmpty(cssText))
                sb.Append($" style=\"{cssText}\"");
            foreach (var (name, value) in el.Attributes.Where(a => a.Key != "id" && a.Key != "class" && a.Key != "style"))
                sb.Append($" {name}=\"{value}\"");
            sb.Append('>');
            foreach (var child in el.ChildNodes)
                AppendHtml(sb, child);
            sb.Append($"</{el.TagName}>");
        }
    }

    /// <inheritdoc/>
    public override object? GetJsProperty(string jsName) {
        return jsName switch {
            "tagName" => NodeName,
            "id" => Id,
            "className" => ClassName,
            "style" => Style,
            "innerHTML" => InnerHtml,
            "outerHTML" => OuterHtml,
            "children" => Children,
            "attributes" => Attributes,
            _ => base.GetJsProperty(jsName)
        };
    }

    /// <inheritdoc/>
    public override void SetJsProperty(string jsName, object? value) {
        switch (jsName) {
            case "id": Id = value?.ToString() ?? ""; break;
            case "className": ClassName = value?.ToString() ?? ""; break;
            case "innerHTML": SetInnerText(value?.ToString() ?? ""); break;
            default: base.SetJsProperty(jsName, value); break;
        }
    }

    /// <inheritdoc/>
    public override object? InvokeJsMethod(string jsName, object?[] args) {
        return jsName switch {
            "getAttribute" => GetAttribute((string)args[0]!),
            "setAttribute" => Do(() => SetAttribute((string)args[0]!, (string)args[1]!)),
            "removeAttribute" => Do(() => RemoveAttribute((string)args[0]!)),
            "hasAttribute" => HasAttribute((string)args[0]!),
            "querySelector" => QuerySelector((string)args[0]!),
            "querySelectorAll" => QuerySelectorAll((string)args[0]!),
            _ => base.InvokeJsMethod(jsName, args)
        };
    }

    private void SetInnerText(string text) {
        ChildNodes.Clear();
        if (!string.IsNullOrEmpty(text))
            AppendChild(new VirtualTextNode(text));
    }

    private static object? Do(System.Action action) { action(); return null; }
}
