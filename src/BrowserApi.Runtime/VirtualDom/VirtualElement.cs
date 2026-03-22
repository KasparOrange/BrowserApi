using System.Text;

namespace BrowserApi.Runtime.VirtualDom;

public class VirtualElement : VirtualNode {
    public string TagName { get; }
    public string Id { get; set; } = "";
    public string ClassName { get; set; } = "";
    public VirtualStyle Style { get; } = new();
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public override int NodeType => 1;
    public override string NodeName => TagName.ToUpperInvariant();

    public IReadOnlyList<VirtualElement> Children =>
        ChildNodes.OfType<VirtualElement>().ToList();

    public VirtualElement(string tagName) {
        TagName = tagName.ToLowerInvariant();
    }

    public string? GetAttribute(string name) =>
        Attributes.TryGetValue(name, out var v) ? v : null;

    public void SetAttribute(string name, string value) =>
        Attributes[name] = value;

    public void RemoveAttribute(string name) =>
        Attributes.Remove(name);

    public bool HasAttribute(string name) =>
        Attributes.ContainsKey(name);

    public VirtualElement? QuerySelector(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .Skip(1) // skip self
            .FirstOrDefault(el => SimpleSelector.Matches(el, selector));
    }

    public List<VirtualElement> QuerySelectorAll(string selector) {
        return DescendantsAndSelf()
            .OfType<VirtualElement>()
            .Skip(1)
            .Where(el => SimpleSelector.Matches(el, selector))
            .ToList();
    }

    public string InnerHtml {
        get {
            var sb = new StringBuilder();
            foreach (var child in ChildNodes)
                AppendHtml(sb, child);
            return sb.ToString();
        }
    }

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

    public override void SetJsProperty(string jsName, object? value) {
        switch (jsName) {
            case "id": Id = value?.ToString() ?? ""; break;
            case "className": ClassName = value?.ToString() ?? ""; break;
            case "innerHTML": SetInnerText(value?.ToString() ?? ""); break;
            default: base.SetJsProperty(jsName, value); break;
        }
    }

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
