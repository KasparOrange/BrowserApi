using System.Text.RegularExpressions;

namespace BrowserApi.Generator.Css;

public static partial class CssValueTypeMapper {
    private static readonly Dictionary<string, string> CssTypeMap = new() {
        ["<length>"] = "Length",
        ["<length-percentage>"] = "Length",
        ["<color>"] = "CssColor",
        ["<time>"] = "Duration",
        ["<angle>"] = "Angle",
        ["<percentage>"] = "Percentage",
        ["<number>"] = "double",
        ["<integer>"] = "int",
        ["<string>"] = "string",
        ["<url>"] = "string",
        ["<image>"] = "string",
        ["<position>"] = "string",
        ["<resolution>"] = "Resolution",
        ["<ratio>"] = "string",
        ["<flex>"] = "Flex",
        ["<frequency>"] = "double",
        ["<custom-ident>"] = "string",
        ["<ident>"] = "string",
        ["<dashed-ident>"] = "string",
    };

    private static readonly Dictionary<string, string> PropertyNameOverrides = new() {
        // Composite types
        ["transform"] = "Transform",
        ["box-shadow"] = "Shadow",
        ["text-shadow"] = "Shadow",
        ["transition"] = "Transition",
        ["transition-duration"] = "Duration",
        ["transition-delay"] = "Duration",
        ["transition-timing-function"] = "Easing",
        ["animation-duration"] = "Duration",
        ["animation-delay"] = "Duration",
        ["animation-timing-function"] = "Easing",
        ["perspective"] = "Length",
        // Box model sides
        ["margin-top"] = "Length",
        ["margin-right"] = "Length",
        ["margin-bottom"] = "Length",
        ["margin-left"] = "Length",
        ["padding-top"] = "Length",
        ["padding-right"] = "Length",
        ["padding-bottom"] = "Length",
        ["padding-left"] = "Length",
        // Gap
        ["row-gap"] = "Length",
        ["column-gap"] = "Length",
        ["gap"] = "Length",
        // Border
        ["border-width"] = "Length",
    };

    public static string MapToCSharpType(string valueGrammar, string? propertyName = null) {
        if (propertyName is not null && PropertyNameOverrides.TryGetValue(propertyName, out var overrideType))
            return overrideType;

        if (string.IsNullOrWhiteSpace(valueGrammar))
            return "string";

        // Pure keywords: no < or ( characters, just words separated by |
        if (IsPureKeywordGrammar(valueGrammar))
            return "enum";

        // Single known CSS data type
        foreach (var (cssType, csType) in CssTypeMap) {
            if (valueGrammar.Trim() == cssType)
                return csType;
        }

        // Check if grammar contains a known type reference
        foreach (var (cssType, csType) in CssTypeMap) {
            if (valueGrammar.Contains(cssType))
                return csType;
        }

        // Complex grammar → string fallback
        return "string";
    }

    public static bool IsPureKeywordGrammar(string grammar) {
        if (string.IsNullOrWhiteSpace(grammar))
            return false;

        // Must not contain < (type references) or ( (grouping)
        if (grammar.Contains('<') || grammar.Contains('('))
            return false;

        // Split by | and check each part is a simple word/identifier
        var parts = grammar.Split('|', StringSplitOptions.TrimEntries);
        return parts.All(p => KeywordPattern().IsMatch(p));
    }

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9-]*$")]
    private static partial Regex KeywordPattern();
}
