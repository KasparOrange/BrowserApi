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
        ["<resolution>"] = "double",
        ["<ratio>"] = "string",
        ["<flex>"] = "double",
        ["<frequency>"] = "double",
        ["<custom-ident>"] = "string",
        ["<ident>"] = "string",
        ["<dashed-ident>"] = "string",
    };

    public static string MapToCSharpType(string valueGrammar) {
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
