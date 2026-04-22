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
    // Match: interface Foo (with or without `export`). Non-exported interfaces
    // are fine because TS `export` only controls `import` visibility, not the
    // JSON shape crossing the JS/C# boundary — a private helper shape in a .d.ts
    // maps to a valid C# record the same way an exported one does.
    // `\b` avoids matching within identifiers like `myinterface`.
    private static readonly Regex InterfaceStartRegex = new(
        @"\b(?:export\s+)?interface\s+(\w+)\s*\{",
        RegexOptions.Compiled);

    // Match an exported function. Handles both forms:
    //   `.d.ts`: `export function foo(x: string): number;`          ← declaration, ends `;`
    //   `.ts`:   `export async function foo(x: string): number { … }` ← implementation, body starts `{`
    //
    // Capture groups:
    //   1. function name
    //   2. raw parameter list text (name:type, name?:type, …)
    //   3. return type (TypeScript syntax, mapped downstream)
    //   4. terminator — `;` for a declaration, `{` for an implementation (body gets skipped)
    //
    // `async` is optional (common in `.ts`); `declare` is optional (common in `.d.ts`).
    // A return-type annotation is still required — TypeScript inference would force us
    // to run the full type system, which we don't. Consumers writing `.ts` should
    // annotate return types on exported functions anyway for a stable public surface.
    private static readonly Regex FunctionRegex = new(
        @"export\s+(?:declare\s+)?(?:async\s+)?function\s+(\w+)\s*\(([^)]*)\)\s*:\s*([^;{]+?)\s*([;{])",
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

    // Blazor interop type names that are pre-declared by Microsoft.JSInterop.
    // If a consumer declares these as stub `interface X {}` in a .d.ts (usually just
    // to satisfy the TypeScript compiler), we ignore the declaration entirely — no
    // TypeMap entry, no emitted C# record. Emitting a C# class for the stub would
    // collide with the real Microsoft.JSInterop.DotNetObjectReference at consumer
    // call sites. References in method signatures are handled by MapTsType.
    private static readonly HashSet<string> BlazorInteropTypeNames = new() {
        "DotNetObjectReference",
    };

    public static TsParseResult Parse(string dtsSource) {
        var result = new TsParseResult();

        // Pass 1: Collect all interface names into the type map (handles forward references).
        // Skip Blazor interop type names — their real counterparts come from Microsoft.JSInterop.
        foreach (Match match in InterfaceStartRegex.Matches(dtsSource)) {
            var ifaceName = match.Groups[1].Value;
            if (BlazorInteropTypeNames.Contains(ifaceName)) continue;
            result.TypeMap[ifaceName] = JsDocParser.ToPascalCase(ifaceName);
        }

        // Pass 2: Extract interfaces.
        //
        // For every interface we capture three things:
        //   - the interface-level JSDoc summary (if any) that sits above the declaration
        //   - each property's name / type / optionality
        //   - each property's JSDoc summary (if any) that sits directly above the line
        //
        // Summaries flow through to the emitted C# record as `<summary>` XML docs, so
        // IntelliSense on the consumer side shows whatever the TypeScript author wrote.
        foreach (Match match in InterfaceStartRegex.Matches(dtsSource)) {
            var ifaceName = match.Groups[1].Value;
            if (BlazorInteropTypeNames.Contains(ifaceName)) continue;
            var rawBody = ExtractBraceBody(dtsSource, match.Index + match.Length - 1);

            var iface = new TsInterfaceInfo {
                TsName = ifaceName,
                CSharpName = JsDocParser.ToPascalCase(ifaceName),
                // Interface-level JSDoc is whatever `/** ... */` block sits immediately
                // above `interface Foo {` in the raw source. Null when absent.
                Summary = FindJsDocImmediatelyBefore(dtsSource, match.Index)
            };

            // Walk the body stateful: each property is paired with whatever JSDoc
            // preceded it (property-level summary), or null if none was written.
            foreach (var (propSummary, propLine) in IterInterfacePropertiesWithJsDoc(rawBody)) {
                var propMatch = PropertyRegex.Match(propLine);
                if (!propMatch.Success) continue;

                var propName = propMatch.Groups[1].Value;
                var isOptional = propMatch.Groups[2].Value == "?";
                var tsType = propMatch.Groups[3].Value.Trim();

                // Inline string-literal union → synthesize an enum and use it as the type.
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
                        IsRequired = !isOptional,
                        Summary = propSummary
                    });
                } else {
                    var csType = MapTsType(tsType, result.TypeMap,
                        result.UnknownTypeFallbacks, $"{ifaceName}.{propName}");
                    if (isOptional && !csType.EndsWith("?") && !csType.EndsWith("[]"))
                        csType += "?";

                    iface.Properties.Add(new TsPropertyInfo {
                        TsName = propName,
                        CSharpName = JsDocParser.ToPascalCase(propName),
                        CSharpType = csType,
                        IsOptional = isOptional,
                        IsRequired = !isOptional,
                        Summary = propSummary
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
                ReturnType = MapTsReturnType(returnTypeStr, result.TypeMap,
                    result.UnknownTypeFallbacks, $"{funcName} return type")
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

                        // Path C: if the parameter's top-level type is DotNetObjectReference
                        // (with or without a type argument), flag it. The generator will
                        // promote the method to generic and emit a real typed
                        // `DotNetObjectReference<TDotNetRefN>` — no call to MapTsType needed.
                        // Nested occurrences (e.g. `DotNetObjectReference[]`) still go through
                        // MapTsType and fall back to `object` — they're documented fallbacks.
                        if (pType == "DotNetObjectReference" || pType.StartsWith("DotNetObjectReference<")) {
                            func.Params.Add(new JsParamInfo {
                                Name = pName,
                                CSharpName = ToCamelCase(pName),
                                CSharpType = "", // ignored when IsDotNetObjectRef is true
                                IsDotNetObjectRef = true,
                                IsOptional = pOptional
                            });
                            continue;
                        }

                        var csPType = MapTsType(pType, result.TypeMap,
                            result.UnknownTypeFallbacks, $"{funcName}({pName})");
                        if (pOptional && !csPType.EndsWith("?"))
                            csPType += "?";

                        func.Params.Add(new JsParamInfo {
                            Name = pName,
                            CSharpName = ToCamelCase(pName),
                            CSharpType = csPType,
                            IsOptional = pOptional
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

    /// <summary>
    /// Extract the leading "summary text" from a raw JSDoc body — everything up to the
    /// first <c>@tag</c> line. The caller passes the *content* of a <c>/** ... */</c>
    /// block (delimiters already stripped). Lines that begin with the canonical <c>*</c>
    /// prefix are cleaned; blank lines are collapsed into spaces.
    /// </summary>
    /// <remarks>
    /// JSDoc convention: the first paragraph before any <c>@param</c>, <c>@returns</c>,
    /// <c>@example</c>, etc. is the human-readable description. That's the text that
    /// belongs in the C# <c>&lt;summary&gt;</c>; tagged content is extracted separately
    /// (see <see cref="AttachJsDocSummaries"/> for function <c>@param</c> handling).
    /// </remarks>
    private static string ExtractJsDocSummary(string rawBody) {
        var summaryLines = new List<string>();
        foreach (var raw in rawBody.Split('\n')) {
            var trimmed = raw.Trim();
            // Canonical JSDoc lines start with "*" — strip it and any following space.
            if (trimmed.StartsWith("*")) trimmed = trimmed.Substring(1).Trim();
            if (trimmed.Length == 0) continue;
            // `@tag` marks the end of the summary section.
            if (trimmed.StartsWith("@")) break;
            summaryLines.Add(trimmed);
        }
        return string.Join(" ", summaryLines).Trim();
    }

    /// <summary>
    /// Look for a <c>/** ... */</c> JSDoc block that sits immediately before
    /// <paramref name="declPosition"/>, separated only by whitespace. Returns the
    /// extracted summary text (via <see cref="ExtractJsDocSummary"/>) or <c>null</c>
    /// if no JSDoc is attached to the declaration.
    /// </summary>
    /// <remarks>
    /// "Immediately before" is strict: any non-whitespace character between the JSDoc
    /// close and the declaration disqualifies the attachment. That prevents us from
    /// accidentally picking up a JSDoc that belongs to a previous, unrelated declaration.
    /// </remarks>
    private static string? FindJsDocImmediatelyBefore(string source, int declPosition) {
        var i = declPosition - 1;
        while (i >= 0 && char.IsWhiteSpace(source[i])) i--;
        // Need "*/" at positions [i-1, i].
        if (i < 1 || source[i] != '/' || source[i - 1] != '*') return null;
        var closeAt = i - 1; // index of '*' in the closing "*/"
        // Walk back to the matching "/**". Use a simple LastIndexOf — JSDoc blocks
        // don't nest, so the nearest "/**" before the close is the opening delimiter.
        var openIdx = source.LastIndexOf("/**", closeAt, StringComparison.Ordinal);
        if (openIdx < 0) return null;
        var inner = source.Substring(openIdx + 3, closeAt - openIdx - 3);
        var summary = ExtractJsDocSummary(inner);
        return summary.Length > 0 ? summary : null;
    }

    /// <summary>
    /// Walk through an interface body (the raw text between <c>{</c> and <c>}</c>) and
    /// yield every property declaration together with the JSDoc summary that precedes it.
    /// </summary>
    /// <remarks>
    /// Why a stateful walk instead of regex-on-stripped-body: the property loop needs
    /// to pair each declaration with the JSDoc that sits immediately above it. Running
    /// <see cref="StripComments"/> first loses that positional relationship, so we
    /// process the raw body directly. The walk skips block/line comments, remembers the
    /// most recent <c>/** ... */</c> block as "pending JSDoc", and emits
    /// <c>(summary, propertyLine)</c> pairs at each <c>;</c> terminator.
    /// </remarks>
    private static IEnumerable<(string? Summary, string PropertyLine)> IterInterfacePropertiesWithJsDoc(string rawBody) {
        var i = 0;
        string? pending = null;
        while (i < rawBody.Length) {
            // Skip leading whitespace between declarations.
            while (i < rawBody.Length && char.IsWhiteSpace(rawBody[i])) i++;
            if (i >= rawBody.Length) break;

            // /** JSDoc */ — save as pending, attach to the next property we emit.
            if (i + 2 < rawBody.Length && rawBody[i] == '/' && rawBody[i + 1] == '*' && rawBody[i + 2] == '*') {
                var end = rawBody.IndexOf("*/", i + 3, StringComparison.Ordinal);
                if (end < 0) break; // malformed block comment — bail
                var content = rawBody.Substring(i + 3, end - i - 3);
                var summary = ExtractJsDocSummary(content);
                pending = summary.Length > 0 ? summary : null;
                i = end + 2;
                continue;
            }

            // /* non-JSDoc block comment */ — skip, do not touch pending (a non-doc
            // comment between JSDoc and the property shouldn't drop the JSDoc).
            if (i + 1 < rawBody.Length && rawBody[i] == '/' && rawBody[i + 1] == '*') {
                var end = rawBody.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? rawBody.Length : end + 2;
                continue;
            }

            // // line comment — same rationale as block comments above.
            if (i + 1 < rawBody.Length && rawBody[i] == '/' && rawBody[i + 1] == '/') {
                var nl = rawBody.IndexOf('\n', i + 2);
                i = nl < 0 ? rawBody.Length : nl + 1;
                continue;
            }

            // Property declaration — walk to the next `;` outside of string literals.
            var semi = FindNextSemicolonOutsideStrings(rawBody, i);
            if (semi < 0) break;
            var line = rawBody.Substring(i, semi - i + 1);
            yield return (pending, line);
            pending = null;
            i = semi + 1;
        }
    }

    /// <summary>
    /// Find the next <c>;</c> character in <paramref name="body"/> starting at
    /// <paramref name="start"/>, ignoring semicolons inside string literals.
    /// Returns <c>-1</c> if none is found.
    /// </summary>
    private static int FindNextSemicolonOutsideStrings(string body, int start) {
        var inString = false;
        var stringChar = ' ';
        for (var i = start; i < body.Length; i++) {
            var c = body[i];
            if (inString) {
                if (c == stringChar && (i == 0 || body[i - 1] != '\\')) inString = false;
                continue;
            }
            if (c == '\'' || c == '"' || c == '`') { inString = true; stringChar = c; continue; }
            if (c == ';') return i;
        }
        return -1;
    }

    /// <summary>Extract the body between matched braces, handling nested braces, strings, and comments.</summary>
    private static string ExtractBraceBody(string source, int openBraceIndex) {
        var depth = 0;
        var inString = false;
        var stringChar = ' ';
        for (var i = openBraceIndex; i < source.Length; i++) {
            var c = source[i];

            // Inside a string literal — wait for the matching unescaped quote
            if (inString) {
                if (c == stringChar && (i == 0 || source[i - 1] != '\\'))
                    inString = false;
                continue;
            }

            // Line comment — skip to end of line
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/') {
                var nl = source.IndexOf('\n', i + 2);
                i = nl >= 0 ? nl : source.Length - 1;
                continue;
            }

            // Block comment — skip to */
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*') {
                var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end >= 0 ? end + 1 : source.Length - 1;
                continue;
            }

            // Enter string literal
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

    /// <summary>Strip // and /* */ comments from a body string so PropertyRegex doesn't match inside them.</summary>
    private static string StripComments(string body) {
        var sb = new System.Text.StringBuilder(body.Length);
        var inString = false;
        var stringChar = ' ';
        for (var i = 0; i < body.Length; i++) {
            var c = body[i];

            if (inString) {
                sb.Append(c);
                if (c == stringChar && (i == 0 || body[i - 1] != '\\'))
                    inString = false;
                continue;
            }

            // Line comment — skip to end of line, keep the newline
            if (c == '/' && i + 1 < body.Length && body[i + 1] == '/') {
                var nl = body.IndexOf('\n', i + 2);
                if (nl >= 0) {
                    sb.Append('\n');
                    i = nl;
                } else {
                    break;
                }
                continue;
            }

            // Block comment — skip to */
            if (c == '/' && i + 1 < body.Length && body[i + 1] == '*') {
                var end = body.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (end >= 0)
                    i = end + 1;
                else
                    break;
                continue;
            }

            if (c == '\'' || c == '"' || c == '`') {
                inString = true;
                stringChar = c;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    internal static string MapTsType(string tsType, Dictionary<string, string> typeMap,
        List<TsTypeFallback>? fallbacks = null, string? context = null) {
        var trimmed = tsType.Trim();

        // Primitives (intentional mappings — no fallback reported)
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

        // Blazor interop types. `DotNetObjectReference<T>` is generic and there is no
        // non-generic parameterizable base — `Microsoft.JSInterop.DotNetObjectReference`
        // is a static factory class (CS0721 if used as a parameter type). We can't
        // resolve `<T>` from the .d.ts side, so `object` is the only safe mapping.
        if (trimmed == "DotNetObjectReference" || trimmed.StartsWith("DotNetObjectReference<"))
            return "object";

        // Array<T>
        if (trimmed.StartsWith("Array<") && trimmed.EndsWith(">")) {
            var inner = trimmed.Substring(6, trimmed.Length - 7);
            return MapTsType(inner, typeMap, fallbacks, context) + "[]";
        }

        // T[]
        if (trimmed.EndsWith("[]")) {
            var inner = trimmed.Substring(0, trimmed.Length - 2);
            return MapTsType(inner, typeMap, fallbacks, context) + "[]";
        }

        // Promise<T> → unwrap
        if (trimmed.StartsWith("Promise<") && trimmed.EndsWith(">")) {
            var inner = trimmed.Substring(8, trimmed.Length - 9);
            return MapTsType(inner, typeMap, fallbacks, context);
        }

        // Record<string, T> → Dictionary<string, T>
        var recordMatch = RecordTypeRegex.Match(trimmed);
        if (recordMatch.Success) {
            var valueType = MapTsType(recordMatch.Groups[1].Value, typeMap, fallbacks, context);
            return $"System.Collections.Generic.Dictionary<string, {valueType}>";
        }

        // String literal union inline
        if (IsStringLiteralUnion(trimmed)) {
            if (typeMap.TryGetValue(trimmed, out var enumName))
                return enumName;
            return "string"; // Fallback if not pre-registered
        }

        // Known interface name → use generated C# record name
        if (typeMap.TryGetValue(trimmed, out var mapped))
            return mapped;

        // Unknown → object (silent degradation — record for diagnostic)
        if (fallbacks is not null && context is not null)
            fallbacks.Add(new TsTypeFallback { TsType = trimmed, Context = context });
        return "object";
    }

    private static string? MapTsReturnType(string tsType, Dictionary<string, string> typeMap,
        List<TsTypeFallback>? fallbacks = null, string? context = null) {
        var mapped = MapTsType(tsType, typeMap, fallbacks, context);
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
        // Find JSDoc blocks immediately before an `export [declare] [async] function Name`.
        //
        // Regex detail: the JSDoc body uses `(?:(?!\*/)[\s\S])*` — "any characters NOT
        // starting a `*/` sequence". Plain `[\s\S]*?` (lazy) would be wrong: it can still
        // match across intermediate `*/` boundaries when no closer match exists earlier,
        // which would let the capture eat through an `export interface Config { ... }`
        // between two JSDoc blocks and glue unrelated content into the function summary.
        //
        // The `async` keyword is optional because `.ts` source commonly uses it; `declare`
        // is optional because `.d.ts` uses it. Accepting both means the same extraction
        // works uniformly across both file formats (preview.7).
        var pattern = new Regex(
            @"/\*\*((?:(?!\*/)[\s\S])*)\*/\s*export\s+(?:declare\s+)?(?:async\s+)?function\s+(\w+)",
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
