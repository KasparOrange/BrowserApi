namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Represents inline CSS styles on a virtual DOM element, providing dictionary-style access
/// to individual CSS properties.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VirtualStyle"/> models the <c>element.style</c> object in the browser DOM.
/// CSS properties can be read and written using the string indexer with kebab-case names
/// (e.g., <c>"background-color"</c>) or through the <see cref="IVirtualNode"/> interface
/// with camelCase names (e.g., <c>"backgroundColor"</c>), which are automatically converted.
/// </para>
/// <para>
/// Setting a property to <see langword="null"/> or an empty string removes it. The
/// <see cref="CssText"/> property provides the full inline style as a semicolon-delimited
/// string (e.g., <c>"color: red; font-size: 16px"</c>), and can be set to parse a style string.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var style = new VirtualStyle();
/// style["color"] = "red";
/// style["font-size"] = "16px";
///
/// Console.WriteLine(style.CssText);  // "color: red; font-size: 16px"
/// Console.WriteLine(style.Count);    // 2
///
/// style["color"] = "";  // removes the property
/// Console.WriteLine(style.Count);    // 1
/// </code>
/// </example>
/// <seealso cref="VirtualElement"/>
/// <seealso cref="IVirtualNode"/>
public class VirtualStyle : IVirtualNode {
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a CSS property value by its kebab-case name.
    /// </summary>
    /// <param name="name">The CSS property name (e.g., <c>"color"</c>, <c>"background-color"</c>).</param>
    /// <returns>
    /// The property value, or an empty string if the property is not set.
    /// </returns>
    /// <remarks>
    /// Setting a value to <see langword="null"/> or empty string removes the property.
    /// </remarks>
    public string this[string name] {
        get => _properties.TryGetValue(name, out var v) ? v : "";
        set {
            if (string.IsNullOrEmpty(value))
                _properties.Remove(name);
            else
                _properties[name] = value;
        }
    }

    /// <summary>
    /// Gets or sets the full inline style as a CSS text string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When read, returns all properties joined with <c>"; "</c> separators (e.g.,
    /// <c>"color: red; font-size: 16px"</c>).
    /// </para>
    /// <para>
    /// When set, clears all existing properties and parses the given string into individual
    /// property-value pairs. Empty or <see langword="null"/> values clear all styles.
    /// </para>
    /// </remarks>
    public string CssText {
        get => string.Join("; ", _properties.Select(kv => $"{kv.Key}: {kv.Value}"));
        set {
            _properties.Clear();
            if (string.IsNullOrEmpty(value)) return;
            foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                var colon = part.IndexOf(':');
                if (colon > 0)
                    _properties[part[..colon].Trim()] = part[(colon + 1)..].Trim();
            }
        }
    }

    /// <summary>
    /// Gets the number of CSS properties currently set in this style object.
    /// </summary>
    public int Count => _properties.Count;

    /// <inheritdoc/>
    /// <remarks>
    /// Supports <c>"cssText"</c>, <c>"length"</c>, and camelCase CSS property names (e.g.,
    /// <c>"backgroundColor"</c> is converted to <c>"background-color"</c> for lookup).
    /// </remarks>
    public object? GetJsProperty(string jsName) {
        if (jsName == "cssText") return CssText;
        if (jsName == "length") return Count;
        // Convert camelCase to kebab-case for lookup
        var cssName = CamelToKebab(jsName);
        return _properties.TryGetValue(cssName, out var v) ? v : "";
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Supports <c>"cssText"</c> and camelCase CSS property names. Setting a property to
    /// <see langword="null"/> or empty removes it.
    /// </remarks>
    public void SetJsProperty(string jsName, object? value) {
        if (jsName == "cssText") { CssText = value?.ToString() ?? ""; return; }
        var cssName = CamelToKebab(jsName);
        if (string.IsNullOrEmpty(value?.ToString()))
            _properties.Remove(cssName);
        else
            _properties[cssName] = value!.ToString()!;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// No methods are currently implemented; always returns <see langword="null"/>.
    /// </remarks>
    public object? InvokeJsMethod(string jsName, object?[] args) => null;

    private static string CamelToKebab(string camelCase) {
        if (string.IsNullOrEmpty(camelCase)) return camelCase;
        var sb = new System.Text.StringBuilder();
        foreach (var c in camelCase) {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
