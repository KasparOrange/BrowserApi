using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BrowserApi.SourceGen;

[Generator(LanguageNames.CSharp)]
public sealed class JsModuleGenerator : IIncrementalGenerator {
    private const string AttributeFullName = "BrowserApi.SourceGen.JsModuleAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Emit marker attribute + path resolver interface
        context.RegisterPostInitializationOutput(static ctx => {
            ctx.AddSource("JsModuleAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
            ctx.AddSource("IJsModulePathResolver.g.cs", SourceText.From(PathResolverSource, Encoding.UTF8));
        });

        // Get all .js and .d.ts AdditionalFiles
        var scriptFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".js") || file.Path.EndsWith(".d.ts") || file.Path.EndsWith(".ts"));

        // Get explicit [JsModule] declarations
        var explicitDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetExplicitClassInfo(ctx, ct))
            .Where(static info => info is not null);

        var combined = scriptFiles.Collect().Combine(explicitDeclarations.Collect());

        context.RegisterSourceOutput(combined, static (spc, pair) => {
            var (allFiles, explicitClasses) = pair;
            GenerateAll(spc, allFiles, explicitClasses!);
        });
    }

    private static ExplicitClassInfo? GetExplicitClassInfo(GeneratorAttributeSyntaxContext ctx, CancellationToken ct) {
        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol) return null;

        var attr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttributeFullName);
        if (attr is null) return null;

        var path = attr.ConstructorArguments.FirstOrDefault().Value as string;
        if (path is null) return null;

        return new ExplicitClassInfo(
            classSymbol.Name,
            classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString(),
            path,
            GetAccessibility(classSymbol));
    }

    private static string GetAccessibility(INamedTypeSymbol symbol) {
        return symbol.DeclaredAccessibility switch {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            _ => "public"
        };
    }

    private static void GenerateAll(
        SourceProductionContext context,
        ImmutableArray<AdditionalText> allFiles,
        ImmutableArray<ExplicitClassInfo?> explicitClasses) {

        if (allFiles.IsEmpty) return;

        // Separate files by type
        var jsFiles = allFiles.Where(f => f.Path.EndsWith(".js") && !f.Path.EndsWith(".d.ts")).ToList();
        var dtsFiles = allFiles.Where(f => f.Path.EndsWith(".d.ts")).ToList();
        var tsFiles = allFiles.Where(f => f.Path.EndsWith(".ts") && !f.Path.EndsWith(".d.ts")).ToList();

        var explicitPaths = new System.Collections.Generic.HashSet<string>();
        foreach (var ec in explicitClasses) {
            if (ec is not null)
                explicitPaths.Add(NormalizePath(ec.JsPath));
        }

        var generatedClasses = new System.Collections.Generic.List<GeneratedClassInfo>();
        var emittedTypes = new System.Collections.Generic.HashSet<string>();

        // 1. Process explicit [JsModule] declarations
        foreach (var ec in explicitClasses) {
            if (ec is null) continue;
            var stem = GetStem(ec.JsPath);

            // Try .d.ts first, then .js
            var dtsFile = dtsFiles.FirstOrDefault(f => GetStem(f.Path) == stem);
            var jsFile = jsFiles.FirstOrDefault(f => GetStem(f.Path) == stem)
                ?? tsFiles.FirstOrDefault(f => GetStem(f.Path) == stem);

            if (dtsFile is not null) {
                var dtsSource = dtsFile.GetText(context.CancellationToken)?.ToString();
                if (dtsSource is not null) {
                    var parsed = ParseAndReport(dtsSource, context);
                    if (parsed.Functions.Count > 0 || parsed.Interfaces.Count > 0) {
                        EmitFromTsResult(context, ec.ClassName, ec.Namespace, ec.JsPath, ec.Accessibility, parsed, emittedTypes);
                        generatedClasses.Add(new GeneratedClassInfo(ec.ClassName, ec.Namespace));
                        continue;
                    }
                }
            }

            // Fallback to .js + JSDoc
            var sourceFile = jsFile ?? FindFile(allFiles, ec.JsPath);
            if (sourceFile is null) {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("BAPI001", "JS file not found",
                        "Could not find AdditionalFile matching '{0}'.",
                        "BrowserApi.SourceGen", DiagnosticSeverity.Warning, true),
                    Location.None, ec.JsPath));
                continue;
            }

            var jsSource = sourceFile.GetText(context.CancellationToken)?.ToString();
            if (jsSource is null) continue;
            var functions = JsDocParser.Parse(jsSource);
            if (functions.Count == 0) continue;
            EmitModuleClass(context, ec.ClassName, ec.Namespace, ec.JsPath, ec.Accessibility, functions);
            generatedClasses.Add(new GeneratedClassInfo(ec.ClassName, ec.Namespace));
        }

        // 2. Auto-discover files without explicit declarations
        var processedStems = new System.Collections.Generic.HashSet<string>();

        // First pass: .d.ts files
        foreach (var dtsFile in dtsFiles) {
            var stem = GetStem(dtsFile.Path);
            if (explicitPaths.Any(ep => ep.Contains(stem))) continue;
            if (!processedStems.Add(stem)) continue;

            var dtsSource = dtsFile.GetText(context.CancellationToken)?.ToString();
            if (dtsSource is null) continue;

            var parsed = ParseAndReport(dtsSource, context);
            if (parsed.Functions.Count == 0 && parsed.Interfaces.Count == 0) continue;

            var className = JsDocParser.SanitizeIdentifier(JsDocParser.ToPascalCase(stem)) + "Module";
            var jsPath = DeriveImportPath(dtsFile.Path);
            var ns = "JsModules";

            EmitFromTsResult(context, className, ns, jsPath, "public", parsed, emittedTypes);
            generatedClasses.Add(new GeneratedClassInfo(className, ns));
        }

        // Second pass: .js files without a .d.ts
        foreach (var jsFile in jsFiles.Concat(tsFiles)) {
            var stem = GetStem(jsFile.Path);
            if (explicitPaths.Any(ep => ep.Contains(stem))) continue;
            if (!processedStems.Add(stem)) continue;

            var jsSource = jsFile.GetText(context.CancellationToken)?.ToString();
            if (jsSource is null) continue;

            var functions = JsDocParser.Parse(jsSource);
            if (functions.Count == 0) continue;

            var className = JsDocParser.SanitizeIdentifier(JsDocParser.ToPascalCase(stem)) + "Module";
            var jsPath = DeriveImportPath(jsFile.Path);
            var ns = "JsModules";

            EmitModuleClass(context, className, ns, jsPath, "public", functions);
            generatedClasses.Add(new GeneratedClassInfo(className, ns));
        }

        // 3. Generate AddJsModules()
        if (generatedClasses.Count > 0)
            EmitServiceRegistration(context, generatedClasses);
    }

    private static TsParseResult ParseAndReport(string dtsSource, SourceProductionContext context) {
        var parsed = TsDeclarationParser.Parse(dtsSource);
        foreach (var fb in parsed.UnknownTypeFallbacks) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("BAPI002",
                    "Unknown TypeScript type mapped to object",
                    "Unknown TypeScript type '{0}' at {1} — falling back to 'object'. Complex generics, intersection types, and unresolved references aren't supported; declare an interface or use a supported shape.",
                    "BrowserApi.SourceGen", DiagnosticSeverity.Warning, true),
                Location.None, fb.TsType, fb.Context));
        }
        return parsed;
    }

    // ─── Emit from .d.ts parse result (interfaces + enums + functions) ───────

    private static void EmitFromTsResult(
        SourceProductionContext context, string className, string? ns,
        string jsPath, string accessibility, TsParseResult parsed,
        System.Collections.Generic.HashSet<string> emittedTypes) {

        // Emit enum files (skip duplicates across files)
        foreach (var enumInfo in parsed.Enums) {
            var key = (ns ?? "") + "." + enumInfo.CSharpName;
            if (!emittedTypes.Add(key)) continue;
            EmitEnum(context, ns, accessibility, enumInfo);
        }

        // Emit record files (skip duplicates across files)
        foreach (var iface in parsed.Interfaces) {
            var key = (ns ?? "") + "." + iface.CSharpName;
            if (!emittedTypes.Add(key)) continue;
            EmitRecord(context, ns, accessibility, iface);
        }

        // Emit module class
        if (parsed.Functions.Count > 0)
            EmitModuleClass(context, className, ns, jsPath, accessibility, parsed.Functions);
    }

    private static void EmitEnum(SourceProductionContext context, string? ns,
        string accessibility, TsEnumInfo enumInfo) {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        if (ns is not null) {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"/// <summary>Generated from TypeScript string literal union.</summary>");
        sb.AppendLine($"[JsonConverter(typeof(JsonStringEnumConverter<{enumInfo.CSharpName}>))]");
        sb.AppendLine($"{accessibility} enum {enumInfo.CSharpName} {{");

        for (var i = 0; i < enumInfo.Members.Count; i++) {
            var m = enumInfo.Members[i];
            var comma = i < enumInfo.Members.Count - 1 ? "," : "";
            // Emit JsonPropertyName for camelCase serialization
            sb.AppendLine($"    [JsonStringEnumMemberName(\"{m.JsValue}\")]");
            sb.AppendLine($"    {m.CSharpName}{comma}");
        }

        sb.AppendLine("}");
        context.AddSource($"{enumInfo.CSharpName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitRecord(SourceProductionContext context, string? ns,
        string accessibility, TsInterfaceInfo iface) {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        if (ns is not null) {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"/// <summary>Generated from TypeScript interface <c>{iface.TsName}</c>.</summary>");
        sb.AppendLine($"{accessibility} sealed class {iface.CSharpName} {{");

        foreach (var prop in iface.Properties) {
            sb.AppendLine($"    /// <summary>Maps to TypeScript property <c>{prop.TsName}</c>.</summary>");
            sb.AppendLine($"    [JsonPropertyName(\"{prop.TsName}\")]");
            if (prop.IsRequired)
                sb.AppendLine($"    public required {prop.CSharpType} {prop.CSharpName} {{ get; init; }}");
            else
                sb.AppendLine($"    public {prop.CSharpType} {prop.CSharpName} {{ get; init; }}");
        }

        sb.AppendLine("}");
        context.AddSource($"{iface.CSharpName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // ─── Emit module class (shared by .js and .d.ts paths) ──────────────────

    private static void EmitModuleClass(
        SourceProductionContext context, string className, string? ns,
        string jsPath, string accessibility,
        System.Collections.Generic.List<JsFunctionInfo> functions) {

        var moduleName = GetStem(jsPath);
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.JSInterop;");
        sb.AppendLine();

        if (ns is not null) {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"/// <summary>Typed C# wrapper for the <c>{jsPath}</c> JavaScript module.</summary>");
        sb.AppendLine($"/// <remarks>");
        sb.AppendLine($"/// Register via <c>builder.Services.AddJsModules();</c>");
        sb.AppendLine($"/// </remarks>");
        sb.AppendLine($"{accessibility} partial class {className} : System.IAsyncDisposable {{");
        sb.AppendLine($"    private readonly IJSRuntime _js;");
        sb.AppendLine($"    private readonly string _modulePath;");
        sb.AppendLine($"    private IJSObjectReference? _module;");
        sb.AppendLine();

        // Constructor with optional path resolver
        sb.AppendLine($"    /// <summary>Creates a new <see cref=\"{className}\"/>.</summary>");
        sb.AppendLine($"    /// <param name=\"js\">The Blazor JS runtime.</param>");
        sb.AppendLine($"    /// <param name=\"pathResolver\">Optional path resolver for Vite/hashed module paths.</param>");
        sb.AppendLine($"    public {className}(IJSRuntime js, BrowserApi.SourceGen.IJsModulePathResolver? pathResolver = null) {{");
        sb.AppendLine($"        _js = js;");
        sb.AppendLine($"        _modulePath = pathResolver?.Resolve(\"{moduleName}\") ?? \"{jsPath}\";");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Module loader
        sb.AppendLine($"    private async System.Threading.Tasks.Task<IJSObjectReference> GetModuleAsync() {{");
        sb.AppendLine($"        return _module ??= await _js.InvokeAsync<IJSObjectReference>(\"import\", _modulePath);");
        sb.AppendLine($"    }}");

        // Emit methods (skip "dispose" — handled by DisposeAsync below)
        var hasJsDispose = functions.Any(f => f.JsName == "dispose");
        foreach (var func in functions) {
            if (func.JsName == "dispose") continue; // avoid duplicate
            sb.AppendLine();
            EmitFunction(sb, func);
        }

        // DisposeAsync — calls JS dispose() if it exists, then releases the module
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Calls the module's dispose() function (if any) and releases the JS module reference.</summary>");
        sb.AppendLine($"    public async System.Threading.Tasks.ValueTask DisposeAsync() {{");
        sb.AppendLine($"        if (_module is not null) {{");
        if (hasJsDispose) {
            sb.AppendLine($"            await _module.InvokeVoidAsync(\"dispose\");");
        }
        sb.AppendLine($"            await _module.DisposeAsync();");
        sb.AppendLine($"            _module = null;");
        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");

        sb.AppendLine("}");
        context.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitServiceRegistration(
        SourceProductionContext context,
        System.Collections.Generic.List<GeneratedClassInfo> classes) {

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>Registers all generated JS module wrapper classes.</summary>");
        sb.AppendLine("public static class JsModuleServiceCollectionExtensions {");
        sb.AppendLine("    /// <summary>Registers all generated JS module wrappers as scoped services.</summary>");
        sb.AppendLine("    public static IServiceCollection AddJsModules(this IServiceCollection services) {");

        foreach (var c in classes) {
            var fullName = c.Namespace is not null ? $"{c.Namespace}.{c.ClassName}" : c.ClassName;
            sb.AppendLine($"        services.AddScoped<{fullName}>();");
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("JsModuleServiceCollectionExtensions.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitFunction(StringBuilder sb, JsFunctionInfo func) {
        if (func.Summary is not null) {
            var summary = func.Summary.Length > 200
                ? func.Summary.Substring(0, 197) + "..."
                : func.Summary;
            sb.AppendLine($"    /// <summary>{EscapeXml(summary)}</summary>");
        }

        // Assign a fresh generic type-parameter name (TDotNetRef, TDotNetRef1, ...) to each
        // DotNetObjectReference-flagged parameter. The method becomes generic over all of them,
        // and the caller's concrete type is inferred at the call site.
        var dotNetRefTypeParams = new System.Collections.Generic.List<string>();
        var paramTypes = new string[func.Params.Count];
        for (var i = 0; i < func.Params.Count; i++) {
            var p = func.Params[i];
            if (p.IsDotNetObjectRef) {
                var tp = dotNetRefTypeParams.Count == 0 ? "TDotNetRef" : $"TDotNetRef{dotNetRefTypeParams.Count}";
                dotNetRefTypeParams.Add(tp);
                var refType = $"Microsoft.JSInterop.DotNetObjectReference<{tp}>";
                paramTypes[i] = p.IsOptional ? refType + "?" : refType;
            } else {
                paramTypes[i] = p.CSharpType;
            }
        }

        // XML doc for the generic type parameters. Makes IntelliSense useful.
        foreach (var tp in dotNetRefTypeParams) {
            sb.AppendLine($"    /// <typeparam name=\"{tp}\">The .NET type wrapped by the DotNetObjectReference the caller passes.</typeparam>");
        }

        foreach (var p in func.Params) {
            if (p.Description is not null)
                sb.AppendLine($"    /// <param name=\"{p.CSharpName}\">{EscapeXml(p.Description)}</param>");
        }

        if (func.ReturnsDoc is not null)
            sb.AppendLine($"    /// <returns>{EscapeXml(func.ReturnsDoc)}</returns>");

        var genericDecl = dotNetRefTypeParams.Count > 0
            ? "<" + string.Join(", ", dotNetRefTypeParams) + ">"
            : "";

        // Constraint clause: `where T : class` matches DotNetObjectReference<TValue>'s own
        // constraint. DynamicallyAccessedMembers(PublicMethods) propagates the trim/AOT
        // annotation from DotNetObjectReference<TValue> to our generated TDotNetRef, avoiding
        // IL2091 warnings in trimmed Blazor WebAssembly builds.
        var whereClauses = dotNetRefTypeParams.Count > 0
            ? " " + string.Join(" ", dotNetRefTypeParams.Select(tp =>
                $"where {tp} : class"))
            : "";

        // If any type parameters were added, decorate each with [DynamicallyAccessedMembers].
        // The attribute is applied at the type-parameter site (T-level), not after `where`.
        var decoratedGenericDecl = dotNetRefTypeParams.Count > 0
            ? "<" + string.Join(", ", dotNetRefTypeParams.Select(tp =>
                $"[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] {tp}")) + ">"
            : "";

        var paramList = func.Params.Count > 0
            ? string.Join(", ", func.Params.Select((p, i) => $"{paramTypes[i]} {p.CSharpName}"))
            : "";

        var argList = func.Params.Count > 0
            ? ", " + string.Join(", ", func.Params.Select(p => p.CSharpName))
            : "";

        if (func.ReturnType is null || func.ReturnType == "void") {
            sb.AppendLine($"    public async System.Threading.Tasks.Task {func.CSharpName}{decoratedGenericDecl}({paramList}){whereClauses} {{");
            sb.AppendLine($"        var module = await GetModuleAsync();");
            sb.AppendLine($"        await module.InvokeVoidAsync(\"{func.JsName}\"{argList});");
            sb.AppendLine($"    }}");
        } else {
            var taskType = $"System.Threading.Tasks.Task<{func.ReturnType}>";
            sb.AppendLine($"    public async {taskType} {func.CSharpName}{decoratedGenericDecl}({paramList}){whereClauses} {{");
            sb.AppendLine($"        var module = await GetModuleAsync();");
            sb.AppendLine($"        return await module.InvokeAsync<{func.ReturnType}>(\"{func.JsName}\"{argList});");
            sb.AppendLine($"    }}");
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static AdditionalText? FindFile(ImmutableArray<AdditionalText> files, string path) {
        var normalized = NormalizePath(path);
        return files.FirstOrDefault(f => NormalizePath(f.Path).EndsWith(normalized));
    }

    private static string NormalizePath(string path) =>
        path.Replace("\\", "/").TrimStart('.', '/');

    private static string GetStem(string path) {
        var name = Path.GetFileName(path);
        // Strip .d.ts, .ts, .js
        if (name.EndsWith(".d.ts")) return name.Substring(0, name.Length - 5);
        if (name.EndsWith(".ts")) return name.Substring(0, name.Length - 3);
        if (name.EndsWith(".js")) return name.Substring(0, name.Length - 3);
        return name;
    }

    private static string DeriveImportPath(string filePath) {
        var normalized = filePath.Replace("\\", "/");
        // Always use .js extension for the import path (browsers don't load .d.ts or .ts)
        var stem = GetStem(filePath);
        var dir = "";
        var wwwrootIndex = normalized.LastIndexOf("wwwroot/");
        if (wwwrootIndex >= 0) {
            var relative = normalized.Substring(wwwrootIndex + "wwwroot/".Length);
            var lastSlash = relative.LastIndexOf('/');
            dir = lastSlash >= 0 ? relative.Substring(0, lastSlash + 1) : "";
        }
        return "./" + dir + stem + ".js";
    }

    // ─── Source constants ────────────────────────────────────────────────────

    private const string AttributeSource = @"// <auto-generated/>
namespace BrowserApi.SourceGen;

/// <summary>
/// Marks a partial class as a typed wrapper for a JavaScript ES module.
/// For zero-config auto-discovery, just add JS/TS files as AdditionalFiles
/// and call <c>builder.Services.AddJsModules();</c>.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class JsModuleAttribute : System.Attribute {
    /// <summary>Gets the module path.</summary>
    public string Path { get; }
    /// <summary>Creates a new JsModuleAttribute.</summary>
    public JsModuleAttribute(string path) => Path = path;
}
";

    private const string PathResolverSource = @"// <auto-generated/>
namespace BrowserApi.SourceGen;

/// <summary>
/// Resolves JS module names to their actual import paths.
/// Implement this to integrate with build tools like Vite that produce
/// content-hashed filenames (e.g., ""mw-dnd"" → ""/js/dist/mw-dnd.a1b2c3.mjs"").
/// </summary>
/// <remarks>
/// Register your implementation in DI:
/// <code>builder.Services.AddSingleton&lt;IJsModulePathResolver, MyViteResolver&gt;();</code>
/// Generated module classes accept it via constructor injection automatically.
/// If not registered, modules fall back to their raw file paths.
/// </remarks>
public interface IJsModulePathResolver {
    /// <summary>
    /// Resolves a module name (e.g., ""mw-dnd"") to its importable path
    /// (e.g., ""/js/dist/mw-dnd.a1b2c3.mjs"").
    /// </summary>
    /// <param name=""moduleName"">The module name without extension.</param>
    /// <returns>The full path suitable for JavaScript dynamic import().</returns>
    string Resolve(string moduleName);
}
";

    // ─── Internal types ─────────────────────────────────────────────────────

    private sealed class ExplicitClassInfo {
        public string ClassName { get; }
        public string? Namespace { get; }
        public string JsPath { get; }
        public string Accessibility { get; }
        public ExplicitClassInfo(string className, string? ns, string jsPath, string accessibility) {
            ClassName = className; Namespace = ns; JsPath = jsPath; Accessibility = accessibility;
        }
    }

    private sealed class GeneratedClassInfo {
        public string ClassName { get; }
        public string? Namespace { get; }
        public GeneratedClassInfo(string className, string? ns) {
            ClassName = className; Namespace = ns;
        }
    }
}
