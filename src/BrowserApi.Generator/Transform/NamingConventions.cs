using System.Text;
using System.Text.RegularExpressions;

namespace BrowserApi.Generator.Transform;

public static partial class NamingConventions {
    private static readonly HashSet<string> CSharpKeywords = [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
        "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    ];

    private static readonly Dictionary<string, string> AcronymMap = new(StringComparer.OrdinalIgnoreCase) {
        ["HTML"] = "Html",
        ["CSS"] = "Css",
        ["DOM"] = "Dom",
        ["XML"] = "Xml",
        ["SVG"] = "Svg",
        ["URL"] = "Url",
        ["URI"] = "Uri",
        ["HTTP"] = "Http",
        ["HTTPS"] = "Https",
        ["XHR"] = "Xhr",
        ["IDB"] = "Idb",
        ["API"] = "Api",
        ["GPU"] = "Gpu",
        ["GL"] = "Gl",
        ["WEBGL"] = "WebGl",
        ["RTCRtp"] = "RtcRtp",
        ["USB"] = "Usb",
        ["HID"] = "Hid",
        ["NFC"] = "Nfc",
        ["MIDI"] = "Midi",
        ["XR"] = "Xr",
        ["AI"] = "Ai",
        ["JS"] = "Js",
    };

    public static string ToPascalCase(string input) {
        if (string.IsNullOrEmpty(input))
            return input;

        // Normalize known acronyms in the input
        var normalized = NormalizeAcronyms(input);

        // Split on non-alphanumeric, underscores, and case transitions
        var words = SplitIntoWords(normalized);

        var sb = new StringBuilder();
        foreach (var word in words) {
            if (word.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1)
                sb.Append(word[1..].ToLowerInvariant());
        }

        return sb.Length > 0 ? sb.ToString() : input;
    }

    public static string ToEnumMemberName(string value) {
        if (string.IsNullOrEmpty(value))
            return "Empty";

        var pascal = ToPascalCase(value);
        if (pascal.Length > 0 && char.IsDigit(pascal[0]))
            pascal = "_" + pascal;

        if (CSharpKeywords.Contains(pascal.ToLowerInvariant()))
            pascal = "@" + pascal;

        return pascal;
    }

    public static string ToParameterName(string input) {
        if (string.IsNullOrEmpty(input))
            return input;

        var pascal = ToPascalCase(input);
        if (pascal.Length == 0) return input;

        var result = char.ToLowerInvariant(pascal[0]) + pascal[1..];

        if (CSharpKeywords.Contains(result))
            result = "@" + result;

        return result;
    }

    public static string EscapeIfKeyword(string name) {
        return CSharpKeywords.Contains(name) ? "@" + name : name;
    }

    private static string NormalizeAcronyms(string input) {
        // Replace known all-caps acronyms at word boundaries with title case
        foreach (var (acronym, replacement) in AcronymMap) {
            var idx = 0;
            while ((idx = input.IndexOf(acronym, idx, StringComparison.Ordinal)) >= 0) {
                // Only replace if the match is at a boundary (not in the middle of lowercase text)
                var afterEnd = idx + acronym.Length;
                var isAtBoundary = afterEnd >= input.Length ||
                                   char.IsUpper(input[afterEnd]) ||
                                   !char.IsLetter(input[afterEnd]);

                if (acronym.All(char.IsUpper) && isAtBoundary) {
                    input = input[..idx] + replacement + input[afterEnd..];
                    idx += replacement.Length;
                } else {
                    idx += acronym.Length;
                }
            }
        }
        return input;
    }

    private static List<string> SplitIntoWords(string input) {
        var words = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < input.Length; i++) {
            var c = input[i];

            if (!char.IsLetterOrDigit(c)) {
                if (current.Length > 0) {
                    words.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            if (i > 0 && char.IsUpper(c) && !char.IsUpper(input[i - 1])) {
                if (current.Length > 0) {
                    words.Add(current.ToString());
                    current.Clear();
                }
            }

            current.Append(c);
        }

        if (current.Length > 0)
            words.Add(current.ToString());

        return words;
    }
}
