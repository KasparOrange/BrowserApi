using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BrowserApi.SourceGen;

/// <summary>
/// Parses TypeScript .d.ts declaration files to extract interfaces, string literal unions,
/// and exported function signatures. Produces typed C# records, enums, and method stubs.
/// </summary>
internal static class TsDeclarationParser {
    // Match: export interface Foo — captures just the name, body extracted via brace counting
    private static readonly Regex InterfaceStartRegex = new(
        @"export\s+interface\s+(\w+)\s*\{",
        RegexOptions.Compiled);

    // Match: export function foo(params): returnType;
    private static readonly Regex FunctionRegex = new(
        @"export\s+(?:declare\s+)?function\s+(\w+)\s*\(([^)]*)\)\s*:\s*([^;]+);",
        RegexOptions.Compiled);

    // Match a property line inside an interface: name?: Type;  or  name: Type;
    private static readonly Regex PropertyRegex = new(
        @"(\w+)(\??):\s*(.+?)\s*;",
        RegexOptions.Compiled);

    // Match JSDoc comment block immediately before something
    private static readonly Regex JsDocBeforeRegex = new(
        @"/\*\*\s*([\s\S]*?)\*/\s*(?=export\s+(?:interface|function|declare))",
        RegexOptions.Compiled);

    // Match: 'value1' | 'value2' | 'value3' (string literal union)
    private static readonly Regex StringLiteralUnionRegex = new(
        @"^'([^']+)'(?:\s*\|\s*'([^']+)')+$",
        RegexOptions.Compiled);

    // Match individual quoted values in a union
    private static readonly Regex QuotedValueRegex = new(
        @"'([^']+)'",
        RegexOptions.Compiled);

    // Match: Record<string, T>
    private static readonly Regex RecordTypeRegex = new(
        @"^Record<\s*string\s*,\s*(.+?)\s*>$",
        RegexOptions.Compiled);

    // Match typed param: name: Type  or  name?: Type
    private static readonly Regex TypedParamRegex = new(
        @"(\w+)(\??):\s*(.+)",
        RegexOptions.Compiled);

    public static TsParseResult Parse(string dtsSource) {
        var result = new TsParseResult();

        // Pass 1: Collect all interface names into the type map (handles forward references)
        foreach (Match match in InterfaceStartRegex.Matches(dtsSource)) {
            var ifaceName = match.Groups[1].Value;
            result.TypeMap[ifaceName] = JsDocParser.ToPascalCase(ifaceName);
        }

        // Pass 2: Extract interfaces and detect inline string literal unions
        foreach (Match match in InterfaceStartRegex.Matches(dtsSource)) {
            var ifaceName = match.Groups[1].Value;
            var body = ExtractBraceBody(dtsSource, match.Index + match.Length - 1);

            var iface = new TsInterfaceInfo {
                TsName = ifaceName,
                CSharpName = JsDocParser.ToPascalCase(ifaceName)
            };

            foreach (Match propMatch in PropertyRegex.Matches(body)) {
                var propName = propMatch.Groups[1].Value;
                var isOptional = propMatch.Groups[2].Value == "?";
                var tsType = propMatch.Groups[3].Value.Trim();

                // Check if the type is a string literal union → generate enum
                if (IsStringLiteralUnion(tsType)) {
                    var enumName = iface.CSharpName + JsDocParser.ToPascalCase(propName);
                    var enumInfo = ParseStringLiteralUnion(enumName, tsType);
                    result.Enums.Add(enumInfo);
                    result.TypeMap[tsType] = enumName;

                    iface.Properties.Add(new TsPropertyInfo {
                        TsName = propName,
                        CSharpName = JsDocParser.ToPascalCase(propName),
                        CSharpType = isOptional ? enumName + "?" : enumName,
                        IsOptional = isOptional,
                        IsRequired = !isOptional
                    });
                } else {
                    var csType = MapTsType(tsType, result.TypeMap);
                    if (isOptional && !csType.EndsWith("?") && !csType.EndsWith("[]"))
                        csType += "?";

                    iface.Properties.Add(new TsPropertyInfo {
                        TsName = propName,
                        CSharpName = JsDocParser.ToPascalCase(propName),
                        CSharpType = csType,
                        IsOptional = isOptional,
                        IsRequired = !isOptional
                    });
                }
            }

            result.Interfaces.Add(iface);
        }

        // 2. Extract exported function signatures
        foreach (Match match in FunctionRegex.Matches(dtsSource)) {
            var funcName = match.Groups[1].Value;
            var paramStr = match.Groups[2].Value;
            var returnTypeStr = match.Groups[3].Value.Trim();

            var func = new JsFunctionInfo {
                JsName = funcName,
                CSharpName = JsDocParser.ToPascalCase(funcName) + "Async",
                ReturnType = MapTsReturnType(returnTypeStr, result.TypeMap)
            };

            // Parse typed parameters
            if (!string.IsNullOrWhiteSpace(paramStr)) {
                foreach (var paramPart in SplitParams(paramStr)) {
                    var trimmed = paramPart.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var typedMatch = TypedParamRegex.Match(trimmed);
                    if (typedMatch.Success) {
                        var pName = typedMatch.Groups[1].Value;
                        var pOptional = typedMatch.Groups[2].Value == "?";
                        var pType = typedMatch.Groups[3].Value.Trim();
                        var csPType = MapTsType(pType, result.TypeMap);
                        if (pOptional && !csPType.EndsWith("?"))
                            csPType += "?";

                        func.Params.Add(new JsParamInfo {
                            Name = pName,
                            CSharpName = ToCamelCase(pName),
                            CSharpType = csPType
                        });
                    } else {
                        func.Params.Add(new JsParamInfo {
                            Name = trimmed,
                            CSharpName = ToCamelCase(trimmed),
                            CSharpType = "object"
                        });
                    }
                }
            }

            result.Functions.Add(func);
        }

        // 3. Try to attach JSDoc summaries to functions
        AttachJsDocSummaries(dtsSource, result.Functions);

        return result;
    }

    /// <summary>Extract the body between matched braces, handling nested braces and strings.</summary>
    private static string ExtractBraceBody(string source, int openBraceIndex) {
        var depth = 0;
        var inString = false;
        var stringChar = ' ';
        for (var i = openBraceIndex; i < source.Length; i++) {
            var c = source[i];
            if (inString) {
                if (c == stringChar && (i == 0 || source[i - 1] != '\\'))
                    inString = false;
                continue;
            }
            if (c == '\'' || c == '"' || c == '`') {
                inString = true;
                stringChar = c;
                continue;
            }
            if (c == '{') depth++;
            else if (c == '}') {
                depth--;
                if (depth == 0)
                    return source.Substring(openBraceIndex + 1, i - openBraceIndex - 1);
            }
        }
        return source.Substring(openBraceIndex + 1);
    }

    internal static string MapTsType(string tsType, Dictionary<string, string> typeMap) {
        var trimmed = tsType.Trim();

        // Primitives
        switch (trimmed) {
            case "number": return "double";
            case "string": return "string";
            case "boolean": return "bool";
            case "void": return "void";
            case "any": return "object";
            case "undefined": return "void";
            case "null": return "object";
            case "never": return "void";
        }

        // Known interop types
        if (trimmed == "DotNetObjectReference" || trimmed.StartsWith("DotNetObjectReference<"))
            return "object"; // Pass-through — C# side uses the real DotNetObjectReference

        // Array<T>
        if (trimmed.StartsWith("Array<") && trimmed.EndsWith(">")) {
            var inner = trimmed.Substring(6, trimmed.Length - 7);
            return MapTsType(inner, typeMap) + "[]";
        }

        // T[]
        if (trimmed.EndsWith("[]")) {
            var inner = trimmed.Substring(0, trimmed.Length - 2);
            return MapTsType(inner, typeMap) + "[]";
        }

        // Promise<T> → unwrap
        if (trimmed.StartsWith("Promise<") && trimmed.EndsWith(">")) {
            var inner = trimmed.Substring(8, trimmed.Length - 9);
            return MapTsType(inner, typeMap);
        }

        // Record<string, T> → Dictionary<string, T>
        var recordMatch = RecordTypeRegex.Match(trimmed);
        if (recordMatch.Success) {
            var valueType = MapTsType(recordMatch.Groups[1].Value, typeMap);
            return $"System.Collections.Generic.Dictionary<string, {valueType}>";
        }

        // String literal union inline
        if (IsStringLiteralUnion(trimmed)) {
            // Check if we already generated an enum for this exact union
            if (typeMap.TryGetValue(trimmed, out var enumName))
                return enumName;
            return "string"; // Fallback if not pre-registered
        }

        // Known interface name → use generated C# record name
        if (typeMap.TryGetValue(trimmed, out var mapped))
            return mapped;

        // Unknown → object
        return "object";
    }

    private static string? MapTsReturnType(string tsType, Dictionary<string, string> typeMap) {
        var mapped = MapTsType(tsType, typeMap);
        return mapped == "void" ? null : mapped;
    }

    internal static bool IsStringLiteralUnion(string tsType) {
        var trimmed = tsType.Trim();
        if (!trimmed.Contains("'")) return false;
        // Must be: 'a' | 'b' | ... (all parts are quoted strings)
        var parts = trimmed.Split('|');
        return parts.Length >= 2 && parts.All(p => {
            var t = p.Trim();
            return t.StartsWith("'") && t.EndsWith("'");
        });
    }

    internal static TsEnumInfo ParseStringLiteralUnion(string enumName, string tsType) {
        var enumInfo = new TsEnumInfo { CSharpName = enumName };
        foreach (Match m in QuotedValueRegex.Matches(tsType)) {
            var jsValue = m.Groups[1].Value;
            enumInfo.Members.Add(new TsEnumMember {
                JsValue = jsValue,
                CSharpName = JsDocParser.ToPascalCase(jsValue.Replace("-", "_"))
            });
        }
        return enumInfo;
    }

    /// <summary>Split parameters handling nested generics (commas inside angle brackets).</summary>
    private static List<string> SplitParams(string paramStr) {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < paramStr.Length; i++) {
            switch (paramStr[i]) {
                case '<': depth++; break;
                case '>': depth--; break;
                case ',' when depth == 0:
                    parts.Add(paramStr.Substring(start, i - start));
                    start = i + 1;
                    break;
            }
        }
        if (start < paramStr.Length)
            parts.Add(paramStr.Substring(start));
        return parts;
    }

    private static void AttachJsDocSummaries(string source, List<JsFunctionInfo> functions) {
        // Simple approach: find JSDoc blocks before export function declarations
        var pattern = new Regex(
            @"/\*\*\s*([\s\S]*?)\*/\s*export\s+(?:declare\s+)?function\s+(\w+)",
            RegexOptions.Compiled);

        foreach (Match m in pattern.Matches(source)) {
            var docBlock = m.Groups[1].Value;
            var funcName = m.Groups[2].Value;

            var func = functions.Find(f => f.JsName == funcName);
            if (func == null) continue;

            // Extract summary (lines before first @tag)
            var summaryLines = new List<string>();
            foreach (var rawLine in docBlock.Split('\n')) {
                var line = rawLine.Trim().TrimStart('*').Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("@")) break;
                summaryLines.Add(line);
            }
            if (summaryLines.Count > 0)
                func.Summary = string.Join(" ", summaryLines);

            // Extract @param descriptions
            var paramPattern = new Regex(@"@param\s+(\w+)\s*(?:-\s*)?(.+)?");
            foreach (Match pm in paramPattern.Matches(docBlock)) {
                var pName = pm.Groups[1].Value;
                var pDesc = pm.Groups[2].Success ? pm.Groups[2].Value.Trim() : null;
                var param = func.Params.Find(p => p.Name == pName);
                if (param != null && !string.IsNullOrEmpty(pDesc))
                    param.Description = pDesc;
            }

            // Extract @returns description
            var returnsPattern = new Regex(@"@returns?\s+(.+)");
            var rm = returnsPattern.Match(docBlock);
            if (rm.Success)
                func.ReturnsDoc = rm.Groups[1].Value.Trim();
        }
    }

    private static string ToCamelCase(string name) {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
