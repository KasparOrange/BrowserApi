namespace BrowserApi.Runtime.VirtualDom;

public class VirtualStyle : IVirtualNode {
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);

    public string this[string name] {
        get => _properties.TryGetValue(name, out var v) ? v : "";
        set {
            if (string.IsNullOrEmpty(value))
                _properties.Remove(name);
            else
                _properties[name] = value;
        }
    }

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

    public int Count => _properties.Count;

    // IVirtualNode — all CSS properties are accessed by JS name directly
    public object? GetJsProperty(string jsName) {
        if (jsName == "cssText") return CssText;
        if (jsName == "length") return Count;
        // Convert camelCase to kebab-case for lookup
        var cssName = CamelToKebab(jsName);
        return _properties.TryGetValue(cssName, out var v) ? v : "";
    }

    public void SetJsProperty(string jsName, object? value) {
        if (jsName == "cssText") { CssText = value?.ToString() ?? ""; return; }
        var cssName = CamelToKebab(jsName);
        if (string.IsNullOrEmpty(value?.ToString()))
            _properties.Remove(cssName);
        else
            _properties[cssName] = value!.ToString()!;
    }

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
