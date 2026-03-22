namespace BrowserApi.Runtime.VirtualDom;

/// <summary>
/// Provides simple CSS selector matching against <see cref="VirtualElement"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This is a lightweight selector engine that supports:
/// </para>
/// <list type="bullet">
///   <item><description>Tag name selectors (e.g., <c>"div"</c>, <c>"span"</c>).</description></item>
///   <item><description>ID selectors (e.g., <c>"#main"</c>).</description></item>
///   <item><description>Class selectors (e.g., <c>".active"</c>).</description></item>
///   <item><description>Compound selectors combining tag, ID, and class (e.g., <c>"div.active#main"</c>).</description></item>
///   <item><description>Comma-separated selector lists (e.g., <c>"div, span.highlight"</c>).</description></item>
/// </list>
/// <para>
/// Combinators (<c>&gt;</c>, <c>+</c>, <c>~</c>, descendant) and pseudo-classes/pseudo-elements
/// are <b>not</b> supported. This engine is designed for testing scenarios where full CSS selector
/// support is not required.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var el = new VirtualElement("div");
/// el.Id = "main";
/// el.ClassName = "container active";
///
/// SimpleSelector.Matches(el, "div");           // true
/// SimpleSelector.Matches(el, "#main");         // true
/// SimpleSelector.Matches(el, ".active");       // true
/// SimpleSelector.Matches(el, "div.container"); // true
/// SimpleSelector.Matches(el, "span");          // false
/// SimpleSelector.Matches(el, "div, span");     // true (comma list)
/// </code>
/// </example>
/// <seealso cref="VirtualElement.QuerySelector"/>
/// <seealso cref="VirtualElement.QuerySelectorAll"/>
/// <seealso cref="VirtualDocument.QuerySelector"/>
public static class SimpleSelector {
    /// <summary>
    /// Determines whether the given element matches the specified CSS selector.
    /// </summary>
    /// <param name="element">The virtual element to test.</param>
    /// <param name="selector">
    /// A CSS selector string. Comma-separated selectors are supported (any match succeeds).
    /// Compound selectors (e.g., <c>"div.active#main"</c>) require all parts to match.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the element matches the selector; otherwise <see langword="false"/>.
    /// </returns>
    public static bool Matches(VirtualElement element, string selector) {
        // Comma-separated: any match succeeds
        if (selector.Contains(',')) {
            return selector.Split(',', StringSplitOptions.TrimEntries)
                .Any(part => Matches(element, part));
        }

        // Compound selector: "div.active#main"
        var parts = ParseCompound(selector.Trim());
        return parts.All(part => MatchesSingle(element, part));
    }

    private static bool MatchesSingle(VirtualElement element, SelectorPart part) {
        return part.Type switch {
            SelectorPartType.Tag => string.Equals(element.TagName, part.Value, StringComparison.OrdinalIgnoreCase),
            SelectorPartType.Id => element.Id == part.Value,
            SelectorPartType.Class => HasClass(element.ClassName, part.Value),
            _ => false
        };
    }

    private static bool HasClass(string className, string target) {
        if (string.IsNullOrEmpty(className)) return false;
        foreach (var cls in className.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
            if (cls == target) return true;
        }
        return false;
    }

    private static List<SelectorPart> ParseCompound(string selector) {
        var parts = new List<SelectorPart>();
        var i = 0;
        while (i < selector.Length) {
            if (selector[i] == '#') {
                var start = ++i;
                while (i < selector.Length && selector[i] != '.' && selector[i] != '#' && selector[i] != '[')
                    i++;
                parts.Add(new SelectorPart(SelectorPartType.Id, selector[start..i]));
            } else if (selector[i] == '.') {
                var start = ++i;
                while (i < selector.Length && selector[i] != '.' && selector[i] != '#' && selector[i] != '[')
                    i++;
                parts.Add(new SelectorPart(SelectorPartType.Class, selector[start..i]));
            } else {
                var start = i;
                while (i < selector.Length && selector[i] != '.' && selector[i] != '#' && selector[i] != '[')
                    i++;
                if (i > start)
                    parts.Add(new SelectorPart(SelectorPartType.Tag, selector[start..i]));
            }
        }
        return parts;
    }

    private record SelectorPart(SelectorPartType Type, string Value);
    private enum SelectorPartType { Tag, Id, Class }
}
