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
        // Emit the JsModuleAttribute source
        context.RegisterPostInitializationOutput(static ctx => {
            ctx.AddSource("JsModuleAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
        });

        // Get all .js AdditionalFiles
        var jsFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".js"));

        // Get explicit [JsModule] declarations
        var explicitDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetExplicitClassInfo(ctx, ct))
            .Where(static info => info is not null);

        // Combine all JS files with explicit declarations
        var combined = jsFiles.Collect().Combine(explicitDeclarations.Collect());

        // Generate all sources
        context.RegisterSourceOutput(combined, static (spc, pair) => {
            var (allJsFiles, explicitClasses) = pair;
            GenerateAll(spc, allJsFiles, explicitClasses!);
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
        ImmutableArray<AdditionalText> jsFiles,
        ImmutableArray<ExplicitClassInfo?> explicitClasses) {

        if (jsFiles.IsEmpty) return;

        // Build a set of paths that have explicit [JsModule] declarations
        var explicitPaths = new System.Collections.Generic.HashSet<string>();
        foreach (var ec in explicitClasses) {
            if (ec is not null)
                explicitPaths.Add(NormalizePath(ec.JsPath));
        }

        var generatedClasses = new System.Collections.Generic.List<GeneratedClassInfo>();

        // 1. Process explicit [JsModule] declarations
        foreach (var ec in explicitClasses) {
            if (ec is null) continue;

            var jsFile = FindJsFile(jsFiles, ec.JsPath);
            if (jsFile is null) {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "BAPI001", "JS file not found",
                        "Could not find AdditionalFile matching '{0}'. Add <AdditionalFiles Include=\"{0}\" /> to your csproj.",
                        "BrowserApi.SourceGen", DiagnosticSeverity.Warning, true),
                    Location.None, ec.JsPath));
                continue;
            }

            var jsSource = jsFile.GetText(context.CancellationToken)?.ToString();
            if (jsSource is null) continue;

            var functions = JsDocParser.Parse(jsSource);
            if (functions.Count == 0) continue;

            EmitModuleClass(context, ec.ClassName, ec.Namespace, ec.JsPath, ec.Accessibility, functions);
            generatedClasses.Add(new GeneratedClassInfo(ec.ClassName, ec.Namespace));
        }

        // 2. Auto-discover JS files that don't have explicit declarations
        foreach (var jsFile in jsFiles) {
            var normalizedPath = NormalizePath(jsFile.Path);

            // Skip if there's an explicit [JsModule] for this file
            var hasExplicit = false;
            foreach (var ep in explicitPaths) {
                if (normalizedPath.EndsWith(ep)) {
                    hasExplicit = true;
                    break;
                }
            }
            if (hasExplicit) continue;

            var jsSource = jsFile.GetText(context.CancellationToken)?.ToString();
            if (jsSource is null) continue;

            var functions = JsDocParser.Parse(jsSource);
            if (functions.Count == 0) continue;

            // Derive class name from filename: utils.js → UtilsModule
            var fileName = Path.GetFileNameWithoutExtension(jsFile.Path);
            var className = JsDocParser.ToPascalCase(fileName) + "Module";

            // Derive JS import path from file path
            var jsPath = DeriveImportPath(jsFile.Path);

            EmitModuleClass(context, className, null, jsPath, "public", functions);
            generatedClasses.Add(new GeneratedClassInfo(className, null));
        }

        // 3. Generate AddJsModules() extension method
        if (generatedClasses.Count > 0)
            EmitServiceRegistration(context, generatedClasses);
    }

    private static void EmitModuleClass(
        SourceProductionContext context,
        string className,
        string? ns,
        string jsPath,
        string accessibility,
        System.Collections.Generic.List<JsFunctionInfo> functions) {

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

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Typed C# wrapper for the <c>{jsPath}</c> JavaScript module.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"/// <remarks>");
        sb.AppendLine($"/// Register via <c>builder.Services.AddJsModules();</c> or manually:");
        sb.AppendLine($"/// <c>builder.Services.AddScoped&lt;{className}&gt;();</c>");
        sb.AppendLine($"/// </remarks>");
        sb.AppendLine($"{accessibility} partial class {className} : System.IAsyncDisposable {{");
        sb.AppendLine($"    private readonly IJSRuntime _js;");
        sb.AppendLine($"    private IJSObjectReference? _module;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Creates a new <see cref=\"{className}\"/>. Typically resolved via DI, not constructed directly.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"js\">The Blazor JS runtime.</param>");
        sb.AppendLine($"    public {className}(IJSRuntime js) => _js = js;");
        sb.AppendLine();

        // Module loader
        sb.AppendLine($"    private async System.Threading.Tasks.Task<IJSObjectReference> GetModuleAsync() {{");
        sb.AppendLine($"        return _module ??= await _js.InvokeAsync<IJSObjectReference>(\"import\", \"{jsPath}\");");
        sb.AppendLine($"    }}");

        // Methods
        foreach (var func in functions) {
            sb.AppendLine();
            EmitFunction(sb, func);
        }

        // DisposeAsync
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Disposes the cached JS module reference.</summary>");
        sb.AppendLine($"    public async System.Threading.Tasks.ValueTask DisposeAsync() {{");
        sb.AppendLine($"        if (_module is not null) {{");
        sb.AppendLine($"            await _module.DisposeAsync();");
        sb.AppendLine($"            _module = null;");
        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");

        sb.AppendLine("}");

        context.AddSource($"{className}.g.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
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
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Extension methods for registering all generated JS module wrappers.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class JsModuleServiceCollectionExtensions {");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers all generated JS module wrapper classes as scoped services.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("    /// <returns>The service collection for chaining.</returns>");
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
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {EscapeXml(func.Summary)}");
            sb.AppendLine($"    /// </summary>");
        }

        foreach (var p in func.Params) {
            if (p.Description is not null)
                sb.AppendLine($"    /// <param name=\"{p.CSharpName}\">{EscapeXml(p.Description)}</param>");
            else
                sb.AppendLine($"    /// <param name=\"{p.CSharpName}\">The {p.Name} parameter.</param>");
        }

        if (func.ReturnsDoc is not null)
            sb.AppendLine($"    /// <returns>{EscapeXml(func.ReturnsDoc)}</returns>");

        var paramList = func.Params.Count > 0
            ? string.Join(", ", func.Params.Select(p => $"{p.CSharpType} {p.CSharpName}"))
            : "";

        var argList = func.Params.Count > 0
            ? ", " + string.Join(", ", func.Params.Select(p => p.CSharpName))
            : "";

        if (func.ReturnType is null || func.ReturnType == "void") {
            sb.AppendLine($"    public async System.Threading.Tasks.Task {func.CSharpName}({paramList}) {{");
            sb.AppendLine($"        var module = await GetModuleAsync();");
            sb.AppendLine($"        await module.InvokeVoidAsync(\"{func.JsName}\"{argList});");
            sb.AppendLine($"    }}");
        } else {
            var taskType = $"System.Threading.Tasks.Task<{func.ReturnType}>";
            sb.AppendLine($"    public async {taskType} {func.CSharpName}({paramList}) {{");
            sb.AppendLine($"        var module = await GetModuleAsync();");
            sb.AppendLine($"        return await module.InvokeAsync<{func.ReturnType}>(\"{func.JsName}\"{argList});");
            sb.AppendLine($"    }}");
        }
    }

    private static string EscapeXml(string text) {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static AdditionalText? FindJsFile(ImmutableArray<AdditionalText> jsFiles, string jsPath) {
        var normalized = NormalizePath(jsPath);
        return jsFiles.FirstOrDefault(f =>
            NormalizePath(f.Path).EndsWith(normalized));
    }

    private static string NormalizePath(string path) {
        return path.Replace("\\", "/").TrimStart('.', '/');
    }

    private static string DeriveImportPath(string filePath) {
        // Try to derive a relative import path from the file path
        // e.g., /Users/.../wwwroot/js/utils.js → ./js/utils.js
        var normalized = filePath.Replace("\\", "/");
        var wwwrootIndex = normalized.LastIndexOf("wwwroot/");
        if (wwwrootIndex >= 0)
            return "./" + normalized.Substring(wwwrootIndex + "wwwroot/".Length);
        // Fallback: use just the filename
        return "./" + Path.GetFileName(filePath);
    }

    private const string AttributeSource = @"// <auto-generated/>
namespace BrowserApi.SourceGen;

/// <summary>
/// Marks a partial class as a typed wrapper for a specific JavaScript ES module.
/// Use this when you want to control the class name or namespace.
/// For zero-config auto-discovery, just add JS files as AdditionalFiles and
/// call <c>builder.Services.AddJsModules();</c> — no attribute needed.
/// </summary>
/// <remarks>
/// <para>
/// The JS file must be registered as an <c>AdditionalFile</c> in the project:
/// <code>&lt;AdditionalFiles Include=""wwwroot/js/myModule.js"" /&gt;</code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit declaration (optional — for custom class name):
/// [JsModule(""./js/utils.js"")]
/// public partial class MyCustomName;
///
/// // Or skip the attribute entirely — the generator auto-discovers JS files
/// // and creates UtilsModule from utils.js automatically.
///
/// // Either way, register in Program.cs:
/// builder.Services.AddJsModules();
///
/// // Then inject:
/// @inject MyCustomName Utils
/// // or @inject UtilsModule Utils
/// </code>
/// </example>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class JsModuleAttribute : System.Attribute {
    /// <summary>Gets the path to the JavaScript module file.</summary>
    public string Path { get; }

    /// <summary>Initializes a new <see cref=""JsModuleAttribute""/> with the specified module path.</summary>
    /// <param name=""path"">The path to the JS file (e.g., <c>""./js/utils.js""</c>). Must match an AdditionalFile.</param>
    public JsModuleAttribute(string path) => Path = path;
}
";

    private sealed class ExplicitClassInfo {
        public string ClassName { get; }
        public string? Namespace { get; }
        public string JsPath { get; }
        public string Accessibility { get; }
        public ExplicitClassInfo(string className, string? ns, string jsPath, string accessibility) {
            ClassName = className;
            Namespace = ns;
            JsPath = jsPath;
            Accessibility = accessibility;
        }
    }

    private sealed class GeneratedClassInfo {
        public string ClassName { get; }
        public string? Namespace { get; }
        public GeneratedClassInfo(string className, string? ns) {
            ClassName = className;
            Namespace = ns;
        }
    }
}
