using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace BrowserApi.SourceGen;

[Generator(LanguageNames.CSharp)]
public sealed class JsModuleGenerator : IIncrementalGenerator {
    private const string AttributeFullName = "BrowserApi.SourceGen.JsModuleAttribute";
    private const string PathResolverFullName = "BrowserApi.SourceGen.IJsModulePathResolver";
    private const string ElementReferenceFullName = "Microsoft.AspNetCore.Components.ElementReference";
    private const string DefaultNamespace = "JsModules";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Only the marker attribute is emitted unconditionally, and it stays internal. Anything
        // public emitted here lands in every assembly under one fixed name, which is what made a
        // generator-using library unusable from a generator-using app. The path resolver contract
        // therefore lives in BrowserApi.JSInterop, not in generated code.
        context.RegisterPostInitializationOutput(static ctx => {
            ctx.AddSource("JsModuleAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
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

        // Reduced to plain values right away so the output step only reruns when one of them
        // changes, not on every keystroke's new Compilation.
        var compilationFacts = context.CompilationProvider.Select(static (compilation, _) => (
            compilation.AssemblyName,
            // Plural lookup on purpose: GetTypeByMetadataName returns null when two referenced
            // assemblies define the name, which would silently drop resolver support. An old
            // library still carrying its generated copy should fail loudly as CS0433 instead.
            HasPathResolver: compilation.GetTypesByMetadataName(PathResolverFullName).Length > 0,
            HasElementReference: compilation.GetTypeByMetadataName(ElementReferenceFullName) is not null));

        var buildProperties = context.AnalyzerConfigOptionsProvider.Select(static (options, _) => (
            Namespace: ReadBuildProperty(options, "BrowserApiSourceGenNamespace"),
            ModuleBasePath: ReadBuildProperty(options, "BrowserApiSourceGenModuleBasePath")));

        var combined = scriptFiles.Collect()
            .Combine(explicitDeclarations.Collect())
            .Combine(compilationFacts)
            .Combine(buildProperties);

        context.RegisterSourceOutput(combined, static (spc, input) => {
            var (((allFiles, explicitClasses), facts), properties) = input;
            var settings = new GenerationSettings(
                facts.AssemblyName, facts.HasPathResolver, facts.HasElementReference,
                properties.Namespace, properties.ModuleBasePath);
            GenerateAll(spc, allFiles, explicitClasses!, settings);
        });
    }

    private static string? ReadBuildProperty(AnalyzerConfigOptionsProvider options, string name) {
        return options.GlobalOptions.TryGetValue("build_property." + name, out var value)
            && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
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
        ImmutableArray<ExplicitClassInfo?> explicitClasses,
        GenerationSettings settings) {

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

        // 1. Process explicit [JsModule] declarations.
        //
        // Source-file priority (same as auto-discovery below):
        //   .d.ts (typed) → .ts (typed) → .js (JSDoc-only).
        //
        // `.d.ts` wins when both it and `.ts` are present because it's the post-tsc
        // canonical type surface (cleaner, no bodies). A consumer shipping only `.ts`
        // gets the same typed treatment without needing a tsc step — new as of
        // preview.7.
        foreach (var ec in explicitClasses) {
            if (ec is null) continue;
            var stem = GetStem(ec.JsPath);

            var dtsFile = dtsFiles.FirstOrDefault(f => GetStem(f.Path) == stem);
            var tsFile = tsFiles.FirstOrDefault(f => GetStem(f.Path) == stem);
            var jsFile = jsFiles.FirstOrDefault(f => GetStem(f.Path) == stem);

            // Try the typed parser on .d.ts first.
            if (dtsFile is not null) {
                var dtsSource = dtsFile.GetText(context.CancellationToken)?.ToString();
                if (dtsSource is not null) {
                    var parsed = ParseAndReport(dtsSource, context, settings);
                    if (parsed.Functions.Count > 0 || parsed.Interfaces.Count > 0) {
                        EmitFromTsResult(context, ec.ClassName, ec.Namespace, ec.JsPath,
                            ec.Accessibility, parsed, emittedTypes, dtsFile.Path, settings);
                        generatedClasses.Add(new GeneratedClassInfo(ec.ClassName, ec.Namespace));
                        continue;
                    }
                }
            }

            // Then try the typed parser on .ts.
            if (tsFile is not null) {
                var tsSource = tsFile.GetText(context.CancellationToken)?.ToString();
                if (tsSource is not null) {
                    var parsed = ParseAndReport(tsSource, context, settings);
                    if (parsed.Functions.Count > 0 || parsed.Interfaces.Count > 0) {
                        EmitFromTsResult(context, ec.ClassName, ec.Namespace, ec.JsPath,
                            ec.Accessibility, parsed, emittedTypes, tsFile.Path, settings);
                        generatedClasses.Add(new GeneratedClassInfo(ec.ClassName, ec.Namespace));
                        continue;
                    }
                }
            }

            // Fallback to JSDoc on .js (or an untyped .ts).
            var sourceFile = jsFile ?? tsFile ?? FindFile(allFiles, ec.JsPath);
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
            EmitModuleClass(context, ec.ClassName, ec.Namespace, ec.JsPath, ec.Accessibility, functions, settings);
            generatedClasses.Add(new GeneratedClassInfo(ec.ClassName, ec.Namespace));
        }

        // 2. Auto-discover files without explicit declarations.
        //
        // Priority, highest first:
        //   1. `.d.ts` — pure typed declarations. Best signal; parsed by TsDeclarationParser.
        //   2. `.ts`   — typed source with implementations. Parsed by TsDeclarationParser
        //                (which now knows how to skip function bodies). This is the recommended
        //                shape post-preview.7 — no tsc step needed.
        //   3. `.js`   — untyped. Parsed by JsDocParser using JSDoc hints only.
        //
        // The priority is "stem-based": for module "mw-dnd", we pick the first file we find
        // with that stem in the priority order above. A consumer shipping both `mw-dnd.ts`
        // and `mw-dnd.d.ts` wins on the `.d.ts` because it's the post-tsc canonical surface.
        var processedStems = new System.Collections.Generic.HashSet<string>();

        // First pass: .d.ts files (typed parser).
        foreach (var dtsFile in dtsFiles) {
            var stem = GetStem(dtsFile.Path);
            if (explicitPaths.Any(ep => ep.Contains(stem))) continue;
            if (!processedStems.Add(stem)) continue;

            var dtsSource = dtsFile.GetText(context.CancellationToken)?.ToString();
            if (dtsSource is null) continue;

            var parsed = ParseAndReport(dtsSource, context, settings);
            if (parsed.Functions.Count == 0 && parsed.Interfaces.Count == 0) continue;

            var className = JsDocParser.SanitizeIdentifier(JsDocParser.ToPascalCase(stem)) + "Module";
            var jsPath = DeriveImportPath(dtsFile.Path, settings);
            var ns = settings.Namespace;

            EmitFromTsResult(context, className, ns, jsPath, "public", parsed, emittedTypes, dtsFile.Path, settings);
            generatedClasses.Add(new GeneratedClassInfo(className, ns));
        }

        // Second pass: .ts files (typed parser). Each .ts with at least one exported function
        // or interface gets the full TypeScript-aware treatment — interfaces become records,
        // string unions become enums, JSDoc flows to C# <summary>, DotNetObjectReference
        // params become generic methods, etc. No .d.ts needed.
        foreach (var tsFile in tsFiles) {
            var stem = GetStem(tsFile.Path);
            if (explicitPaths.Any(ep => ep.Contains(stem))) continue;
            if (!processedStems.Add(stem)) continue;

            var tsSource = tsFile.GetText(context.CancellationToken)?.ToString();
            if (tsSource is null) continue;

            var parsed = ParseAndReport(tsSource, context, settings);
            // If nothing was parsed, fall through to the JSDoc-only pass below. That preserves
            // the legacy "parse any .ts like a .js" behavior for files that have JSDoc on
            // plain JavaScript without TypeScript typing annotations.
            if (parsed.Functions.Count == 0 && parsed.Interfaces.Count == 0) {
                processedStems.Remove(stem);
                continue;
            }

            var className = JsDocParser.SanitizeIdentifier(JsDocParser.ToPascalCase(stem)) + "Module";
            var jsPath = DeriveImportPath(tsFile.Path, settings);
            var ns = settings.Namespace;

            EmitFromTsResult(context, className, ns, jsPath, "public", parsed, emittedTypes, tsFile.Path, settings);
            generatedClasses.Add(new GeneratedClassInfo(className, ns));
        }

        // Third pass: .js files and .ts files not picked up by the typed parser above.
        // Uses JSDoc-only parsing — weak typing, but covers legacy / untyped modules.
        foreach (var jsFile in jsFiles.Concat(tsFiles)) {
            var stem = GetStem(jsFile.Path);
            if (explicitPaths.Any(ep => ep.Contains(stem))) continue;
            if (!processedStems.Add(stem)) continue;

            var jsSource = jsFile.GetText(context.CancellationToken)?.ToString();
            if (jsSource is null) continue;

            var functions = JsDocParser.Parse(jsSource);
            if (functions.Count == 0) continue;

            var className = JsDocParser.SanitizeIdentifier(JsDocParser.ToPascalCase(stem)) + "Module";
            var jsPath = DeriveImportPath(jsFile.Path, settings);
            var ns = settings.Namespace;

            EmitModuleClass(context, className, ns, jsPath, "public", functions, settings);
            generatedClasses.Add(new GeneratedClassInfo(className, ns));
        }

        // 3. Generate the DI registration
        if (generatedClasses.Count > 0)
            EmitServiceRegistration(context, generatedClasses, settings);
    }

    private static TsParseResult ParseAndReport(string dtsSource, SourceProductionContext context,
        GenerationSettings settings) {
        var parsed = TsDeclarationParser.Parse(dtsSource, settings.HasElementReference);
        foreach (var fb in parsed.UnknownTypeFallbacks) {
            if (fb.IsSkippedProperty) {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("BAPI002",
                        "TypeScript property left out of the generated record",
                        "Could not parse '{0}' in interface {1}, so this property is left out of the generated record. Its type is not supported; declare an interface or use a supported shape.",
                        "BrowserApi.SourceGen", DiagnosticSeverity.Warning, true),
                    Location.None, fb.TsType, fb.Context));
                continue;
            }

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
        System.Collections.Generic.HashSet<string> emittedTypes,
        string sourcePath, GenerationSettings settings) {

        // Emit enum files (skip duplicates across files)
        foreach (var enumInfo in parsed.Enums) {
            var key = (ns ?? "") + "." + enumInfo.CSharpName;
            if (!emittedTypes.Add(key)) continue;
            EmitEnum(context, ns, accessibility, enumInfo, sourcePath);
        }

        // Emit record files (skip duplicates across files)
        foreach (var iface in parsed.Interfaces) {
            var key = (ns ?? "") + "." + iface.CSharpName;
            if (!emittedTypes.Add(key)) continue;
            EmitRecord(context, ns, accessibility, iface, sourcePath);
        }

        // Emit module class
        if (parsed.Functions.Count > 0)
            EmitModuleClass(context, className, ns, jsPath, accessibility, parsed.Functions, settings, sourcePath);
    }

    /// <summary>
    /// Write the canonical header for a generated file: auto-generated marker, the
    /// originating TypeScript source path, a "don't hand-edit" note, and
    /// <c>#nullable enable</c>. Every <c>.g.cs</c> the generator emits should start
    /// with this so a reader opening the file in an IDE knows what it is, where it
    /// came from, and where to make real edits.
    /// </summary>
    private static void EmitFileHeader(StringBuilder sb, string sourcePath) {
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"//     Generated by BrowserApi.SourceGen from: {sourcePath}");
        sb.AppendLine("//     Do not edit this file directly. Edit the TypeScript source and rebuild.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
    }

    private static void EmitEnum(SourceProductionContext context, string? ns,
        string accessibility, TsEnumInfo enumInfo, string sourcePath) {
        var sb = new StringBuilder();
        EmitFileHeader(sb, sourcePath);
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        if (ns is not null) {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        // Enums arise from inline string-literal unions in property positions (e.g.
        // `mode: 'a' | 'b'`). There's no JSDoc attached to the union itself — the
        // property that uses it carries the docs. So the enum-level summary stays
        // a generic pointer that explains where the enum came from.
        sb.AppendLine($"/// <summary>Generated from a TypeScript string-literal union. Values round-trip as the original literal strings via <see cref=\"JsonStringEnumConverter{{T}}\"/>.</summary>");
        sb.AppendLine($"[JsonConverter(typeof(JsonStringEnumConverter<{enumInfo.CSharpName}>))]");
        sb.AppendLine($"{accessibility} enum {enumInfo.CSharpName} {{");

        for (var i = 0; i < enumInfo.Members.Count; i++) {
            var m = enumInfo.Members[i];
            var comma = i < enumInfo.Members.Count - 1 ? "," : "";
            // [JsonStringEnumMemberName] preserves the original TS literal on the JSON
            // wire (often camelCase or kebab-case) while the C# member follows PascalCase.
            sb.AppendLine($"    /// <summary>Serializes to the TypeScript literal <c>\"{m.JsValue}\"</c>.</summary>");
            sb.AppendLine($"    [JsonStringEnumMemberName(\"{m.JsValue}\")]");
            sb.AppendLine($"    {m.CSharpName}{comma}");
        }

        sb.AppendLine("}");
        context.AddSource($"{enumInfo.CSharpName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void EmitRecord(SourceProductionContext context, string? ns,
        string accessibility, TsInterfaceInfo iface, string sourcePath) {
        var sb = new StringBuilder();
        EmitFileHeader(sb, sourcePath);
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        if (ns is not null) {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        // Interface-level summary: use the author's JSDoc when present, otherwise a
        // helpful generic pointer back to the TypeScript source.
        if (iface.Summary is not null) {
            sb.AppendLine($"/// <summary>{EscapeXml(iface.Summary)}</summary>");
            sb.AppendLine($"/// <remarks>Generated from the TypeScript interface <c>{iface.TsName}</c>.</remarks>");
        } else {
            sb.AppendLine($"/// <summary>Generated from the TypeScript interface <c>{iface.TsName}</c>.</summary>");
        }
        sb.AppendLine($"{accessibility} sealed class {iface.CSharpName} {{");

        for (var i = 0; i < iface.Properties.Count; i++) {
            var prop = iface.Properties[i];
            // Per-property summary: author's JSDoc wins; fall back to the boilerplate
            // that at least records the original TS property name.
            if (prop.Summary is not null)
                sb.AppendLine($"    /// <summary>{EscapeXml(prop.Summary)}</summary>");
            else
                sb.AppendLine($"    /// <summary>Maps to TypeScript property <c>{prop.TsName}</c>.</summary>");
            sb.AppendLine($"    [JsonPropertyName(\"{prop.TsName}\")]");
            if (prop.IsRequired)
                sb.AppendLine($"    public required {prop.CSharpType} {prop.CSharpName} {{ get; init; }}");
            else
                sb.AppendLine($"    public {prop.CSharpType} {prop.CSharpName} {{ get; init; }}");

            // Blank line between properties for readability — except after the last one.
            if (i < iface.Properties.Count - 1) sb.AppendLine();
        }

        sb.AppendLine("}");
        context.AddSource($"{iface.CSharpName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // ─── Emit module class (shared by .js and .d.ts paths) ──────────────────

    private static void EmitModuleClass(
        SourceProductionContext context, string className, string? ns,
        string jsPath, string accessibility,
        System.Collections.Generic.List<JsFunctionInfo> functions,
        GenerationSettings settings,
        string? sourcePath = null) {

        var moduleName = GetStem(jsPath);
        var sb = new StringBuilder();
        // When called from the .d.ts path we have the source file path; when called
        // from the JSDoc-only .js path we fall back to the import path as the best
        // identifier available. Either way the header points a reader at "where to edit".
        EmitFileHeader(sb, sourcePath ?? jsPath);
        sb.AppendLine("using Microsoft.JSInterop;");
        sb.AppendLine();

        if (ns is not null) {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"/// <summary>Typed C# wrapper for the <c>{jsPath}</c> JavaScript module.</summary>");
        sb.AppendLine($"/// <remarks>");
        sb.AppendLine($"/// Register in DI via <c>services.AddJsModules()</c> inside the <c>{settings.AssemblyDisplayName}</c> assembly, or <c>services.{settings.RegistrationMethodName}()</c> from any other, then inject as a scoped service.");
        sb.AppendLine($"/// The JavaScript is lazily imported on first call, not at registration time.");
        sb.AppendLine($"/// </remarks>");
        sb.AppendLine($"{accessibility} partial class {className} : System.IAsyncDisposable {{");
        sb.AppendLine($"    private readonly IJSRuntime _js;");
        sb.AppendLine($"    private readonly string _modulePath;");
        sb.AppendLine($"    private IJSObjectReference? _module;");
        sb.AppendLine();

        // Constructor with optional path resolver. Without BrowserApi.JSInterop referenced there
        // is no contract to name, and a generator-only consumer must keep compiling, so the
        // parameter is left out rather than emitting a private copy of the interface again.
        sb.AppendLine($"    /// <summary>Creates a new <see cref=\"{className}\"/>.</summary>");
        sb.AppendLine($"    /// <param name=\"js\">The Blazor JS runtime.</param>");
        if (settings.HasPathResolver) {
            sb.AppendLine($"    /// <param name=\"pathResolver\">Optional path resolver for Vite/hashed module paths.</param>");
            sb.AppendLine($"    public {className}(IJSRuntime js, global::{PathResolverFullName}? pathResolver = null) {{");
            sb.AppendLine($"        _js = js;");
            sb.AppendLine($"        _modulePath = pathResolver?.Resolve(\"{moduleName}\", \"{settings.AssemblyDisplayName}\") ?? \"{jsPath}\";");
        } else {
            sb.AppendLine($"    public {className}(IJSRuntime js) {{");
            sb.AppendLine($"        _js = js;");
            sb.AppendLine($"        _modulePath = \"{jsPath}\";");
        }
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

    /// <summary>
    /// Emit the DI registration as two classes, so it cannot collide across assemblies:
    /// a public one named after the assembly, reachable from a host, and an internal
    /// <c>AddJsModules()</c> alias that keeps the short name working inside the assembly.
    /// Internal types of a referenced assembly are invisible to the compiler, which is what
    /// makes the fixed short name safe (barring <c>InternalsVisibleTo</c> between two
    /// generator-using assemblies).
    /// </summary>
    private static void EmitServiceRegistration(
        SourceProductionContext context,
        System.Collections.Generic.List<GeneratedClassInfo> classes,
        GenerationSettings settings) {

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Registers the JS module wrappers generated into the <c>{settings.AssemblyDisplayName}</c> assembly.</summary>");
        sb.AppendLine($"public static class {settings.RegistrationClassName} {{");
        sb.AppendLine($"    /// <summary>Registers the JS module wrappers of <c>{settings.AssemblyDisplayName}</c> as scoped services.</summary>");
        sb.AppendLine($"    public static IServiceCollection {settings.RegistrationMethodName}(this IServiceCollection services) {{");

        foreach (var c in classes) {
            var fullName = c.Namespace is not null ? $"{c.Namespace}.{c.ClassName}" : c.ClassName;
            sb.AppendLine($"        services.AddScoped<{fullName}>();");
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Short alias of <see cref=\"{settings.RegistrationClassName}.{settings.RegistrationMethodName}\"/> for code inside this assembly.</summary>");
        sb.AppendLine("internal static class JsModuleServiceCollectionExtensions {");
        sb.AppendLine("    /// <summary>Registers the JS module wrappers of this assembly as scoped services.</summary>");
        sb.AppendLine($"    internal static IServiceCollection AddJsModules(this IServiceCollection services) => services.{settings.RegistrationMethodName}();");
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
                // `using Microsoft.JSInterop;` is emitted in the module file header above,
                // so the unqualified `DotNetObjectReference<T>` resolves correctly. Path C's
                // skip-list also guarantees no `JsModules.DotNetObjectReference` class exists
                // to shadow it — consumers get clean, readable signatures with no namespace
                // clutter even when `using JsModules;` is in scope at the call site.
                var refType = $"DotNetObjectReference<{tp}>";
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

    private static string DeriveImportPath(string filePath, GenerationSettings settings) {
        var normalized = filePath.Replace("\\", "/");
        // Always use .js extension for the import path (browsers don't load .d.ts or .ts)
        var stem = GetStem(filePath);

        // An RCL serves its bundles from _content/<PackageId>/, a URL the source file's location
        // can't reveal, so the project states the folder.
        if (settings.ModuleBasePath is not null)
            return settings.ModuleBasePath.TrimEnd('/') + "/" + stem + ".js";

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
/// and call <c>services.AddJsModules()</c>.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class JsModuleAttribute : System.Attribute {
    /// <summary>Gets the module path.</summary>
    public string Path { get; }
    /// <summary>Creates a new JsModuleAttribute.</summary>
    public JsModuleAttribute(string path) => Path = path;
}
";

    // ─── Internal types ─────────────────────────────────────────────────────

    /// <summary>Everything about the consuming project that shapes the generated code.</summary>
    private sealed class GenerationSettings {
        public string AssemblyDisplayName { get; }
        public bool HasPathResolver { get; }
        public bool HasElementReference { get; }
        public string Namespace { get; }
        public string? ModuleBasePath { get; }
        public string RegistrationClassName { get; }
        public string RegistrationMethodName { get; }

        public GenerationSettings(string? assemblyName, bool hasPathResolver, bool hasElementReference,
            string? ns, string? moduleBasePath) {
            AssemblyDisplayName = assemblyName ?? "";
            HasPathResolver = hasPathResolver;
            HasElementReference = hasElementReference;
            Namespace = ns ?? DefaultNamespace;
            ModuleBasePath = moduleBasePath;

            var stem = ToIdentifierStem(assemblyName);
            RegistrationClassName = stem + "JsModuleServiceCollectionExtensions";
            RegistrationMethodName = "Add" + stem + "JsModules";
        }

        /// <summary>Turns <c>MitWare.Components.Interop</c> into <c>MitWareComponentsInterop</c>.</summary>
        private static string ToIdentifierStem(string? assemblyName) {
            var sb = new StringBuilder();
            var startOfWord = true;
            foreach (var c in assemblyName ?? "") {
                if (!char.IsLetterOrDigit(c)) {
                    startOfWord = true;
                    continue;
                }
                sb.Append(startOfWord ? char.ToUpperInvariant(c) : c);
                startOfWord = false;
            }
            if (sb.Length > 0 && char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }
    }

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
