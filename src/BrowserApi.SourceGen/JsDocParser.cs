using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BrowserApi.SourceGen;

internal static class JsDocParser {
    private static readonly Regex ExportFunctionRegex = new(
        @"export\s+function\s+(\w+)\s*\(([^)]*)\)",
        RegexOptions.Compiled);

    private static readonly Regex JsDocBlockRegex = new(
        @"/\*\*\s*([\s\S]*?)\*/\s*export\s+function\s+(\w+)\s*\(([^)]*)\)",
        RegexOptions.Compiled);

    private static readonly Regex ParamRegex = new(
        @"@param\s+\{([^}]*)\}\s+(\w+)(?:\s*-?\s*(.*))?",
        RegexOptions.Compiled);

    private static readonly Regex ReturnsRegex = new(
        @"@returns?\s+\{([^}]*)\}(?:\s*(.*))?",
        RegexOptions.Compiled);

    public static List<JsFunctionInfo> Parse(string jsSource) {
        var functions = new List<JsFunctionInfo>();

        // First pass: functions with JSDoc comments
        foreach (Match match in JsDocBlockRegex.Matches(jsSource)) {
            var docBlock = match.Groups[1].Value;
            var funcName = match.Groups[2].Value;
            var paramList = match.Groups[3].Value;

            var info = new JsFunctionInfo {
                JsName = funcName,
                CSharpName = ToPascalCase(funcName) + "Async"
            };

            ParseJsDoc(docBlock, info);
            ParseParams(paramList, info);

            functions.Add(info);
        }

        // Second pass: exported functions without JSDoc
        foreach (Match match in ExportFunctionRegex.Matches(jsSource)) {
            var funcName = match.Groups[1].Value;
            if (functions.Exists(f => f.JsName == funcName))
                continue;

            var info = new JsFunctionInfo {
                JsName = funcName,
                CSharpName = ToPascalCase(funcName) + "Async"
            };

            ParseParams(match.Groups[2].Value, info);
            functions.Add(info);
        }

        return functions;
    }

    private static void ParseJsDoc(string docBlock, JsFunctionInfo info) {
        var lines = docBlock.Split('\n');
        var summaryLines = new List<string>();
        var foundTag = false;

        foreach (var rawLine in lines) {
            var line = rawLine.Trim().TrimStart('*').Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("@")) {
                foundTag = true;

                var paramMatch = ParamRegex.Match(line);
                if (paramMatch.Success) {
                    var jsType = paramMatch.Groups[1].Value;
                    var paramName = paramMatch.Groups[2].Value;
                    var desc = paramMatch.Groups[3].Value.Trim();

                    // Update existing param or add new one
                    var existing = info.Params.Find(p => p.Name == paramName);
                    if (existing != null) {
                        existing.CSharpType = MapType(jsType);
                        existing.Description = string.IsNullOrEmpty(desc) ? null : desc;
                    } else {
                        info.Params.Add(new JsParamInfo {
                            Name = paramName,
                            CSharpName = ToCamelCase(paramName),
                            CSharpType = MapType(jsType),
                            Description = string.IsNullOrEmpty(desc) ? null : desc
                        });
                    }
                    continue;
                }

                var returnsMatch = ReturnsRegex.Match(line);
                if (returnsMatch.Success) {
                    var jsType = returnsMatch.Groups[1].Value;
                    var desc = returnsMatch.Groups[2].Value.Trim();
                    info.ReturnType = MapType(jsType);
                    info.ReturnsDoc = string.IsNullOrEmpty(desc) ? null : desc;
                    if (info.ReturnType == "void") info.ReturnType = null;
                }

                continue;
            }

            if (!foundTag && !line.StartsWith("@description")) {
                summaryLines.Add(line.StartsWith("@description ") ? line.Substring(13).Trim() : line);
            }
        }

        if (summaryLines.Count > 0)
            info.Summary = string.Join(" ", summaryLines);
    }

    private static void ParseParams(string paramList, JsFunctionInfo info) {
        if (string.IsNullOrWhiteSpace(paramList)) return;

        var paramNames = paramList.Split(',');
        foreach (var p in paramNames) {
            var raw = p.Trim();
            if (string.IsNullOrEmpty(raw)) continue;

            // Handle TS-style typed params: "name: Type" or "name?: Type"
            string name;
            string csType = "object";
            var colonIdx = raw.IndexOf(':');
            if (colonIdx > 0) {
                name = raw.Substring(0, colonIdx).TrimEnd('?', ' ');
                var tsType = raw.Substring(colonIdx + 1).Trim();
                csType = MapType(tsType);
                if (raw.Substring(0, colonIdx).TrimEnd().EndsWith("?") && !csType.EndsWith("?"))
                    csType += "?";
            } else {
                name = raw;
            }

            // Skip if already added from JSDoc
            if (info.Params.Exists(existing => existing.Name == name))
                continue;

            info.Params.Add(new JsParamInfo {
                Name = name,
                CSharpName = ToCamelCase(name),
                CSharpType = csType
            });
        }
    }

    internal static string MapType(string jsType) {
        var trimmed = jsType.Trim();
        switch (trimmed.ToLowerInvariant()) {
            case "number": return "double";
            case "string": return "string";
            case "boolean": case "bool": return "bool";
            case "void": return "void";
            case "any": case "object": return "object";
            case "undefined": return "void";
        }

        if (trimmed.StartsWith("Promise<") && trimmed.EndsWith(">")) {
            var inner = trimmed.Substring(8, trimmed.Length - 9);
            return MapType(inner);
        }

        if (trimmed.StartsWith("Array<") && trimmed.EndsWith(">")) {
            var inner = trimmed.Substring(6, trimmed.Length - 7);
            return MapType(inner) + "[]";
        }

        if (trimmed.EndsWith("[]")) {
            var inner = trimmed.Substring(0, trimmed.Length - 2);
            return MapType(inner) + "[]";
        }

        return "object";
    }

    internal static string ToPascalCase(string name) {
        if (string.IsNullOrEmpty(name)) return name;
        // Handle kebab-case (mw-dnd → MwDnd) and snake_case (my_func → MyFunc)
        var sb = new System.Text.StringBuilder();
        var capitalizeNext = true;
        foreach (var c in name) {
            if (c == '-' || c == '_') {
                capitalizeNext = true;
                continue;
            }
            sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
            capitalizeNext = false;
        }
        return sb.Length > 0 ? sb.ToString() : name;
    }

    /// <summary>Sanitize a string to be a valid C# identifier.</summary>
    internal static string SanitizeIdentifier(string name) {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        foreach (var c in name) {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
        }
        var result = sb.ToString();
        if (result.Length > 0 && char.IsDigit(result[0]))
            result = "_" + result;
        return result;
    }

    private static string ToCamelCase(string name) {
        if (string.IsNullOrEmpty(name)) return name;
        var pascal = ToPascalCase(name);
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }
}
