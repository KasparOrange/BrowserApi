namespace BrowserApi.Runtime.VirtualDom;

public static class SimpleSelector {
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
