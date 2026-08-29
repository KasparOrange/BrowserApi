using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BrowserApi.Css.SourceGen;

/// <summary>
/// Roslyn incremental source generator for the CSS-in-C# authoring API.
/// Discovers <c>StyleSheet</c> subclasses at compile time and emits a
/// <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>
/// that pre-populates <c>Class.Name</c>, <c>CssVar&lt;T&gt;.Name</c>, and
/// <c>Keyframes.Name</c> on every discovered field. This replaces the
/// runtime AppDomain reflection scan with build-time work.
/// </summary>
/// <remarks>
/// <para>
/// The runtime <c>CssRegistry</c> still works without this generator —
/// it falls back to lazy AppDomain scanning at first access. The
/// generator's value is (a) zero per-app-startup reflection cost, (b) all
/// names available immediately on type initialization (no scan-in-progress
/// edge cases), and (c) the foundation for future build-time analyzers and
/// CSS-emit-to-static-asset pipelines.
/// </para>
/// <para>
/// The generator does NOT emit CSS strings yet — that's spec §12's
/// stretch goal and requires evaluating user expressions like
/// <c>Length.Px(16)</c> at compile time. The runtime emitter handles CSS
/// rendering today; the source-gen-emitted name initializers feed the same
/// emitter so output is identical.
/// </para>
/// </remarks>
[Generator]
public sealed class CssClassNameGenerator : IIncrementalGenerator {
    private const string StyleSheetFullName = "BrowserApi.Css.Authoring.StyleSheet";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Find every class declaration that LOOKS like it might be a StyleSheet
        // subclass (has a base list with at least one identifier). The semantic
        // check below confirms it's actually our StyleSheet.
        var styleSheetClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cls && cls.BaseList is not null,
                transform: static (ctx, _) => ResolveStyleSheet(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        // Group all stylesheet types by their containing module so we emit
        // ONE module initializer per assembly, not one per stylesheet class.
        // The global prefix comes from the same Program.cs lambda CssConfigGenerator
        // reads — one place of truth, so names populated here already match what
        // the runtime would compute.
        var globalPrefix = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => CssConfigGenerator.IsAddBrowserApiCssCall(node),
                transform: static (ctx, _) => CssConfigGenerator.Collect(ctx.SemanticModel, (InvocationExpressionSyntax)ctx.Node))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value.FirstOrDefault(v => v.Name == "GlobalPrefix").Value as string ?? "")
            .Collect()
            .Select(static (all, _) => all.IsDefaultOrEmpty ? "" : all[0]);

        var collected = styleSheetClasses.Collect().Combine(globalPrefix);

        context.RegisterSourceOutput(collected, (spc, input) => {
            var (classes, prefix) = input;
            if (classes.IsDefaultOrEmpty) return;
            var source = EmitInitializer(classes, prefix);
            spc.AddSource("BrowserApiCssInitializer.g.cs", source);
        });
    }

    private static StyleSheetInfo? ResolveStyleSheet(GeneratorSyntaxContext ctx) {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol) return null;

        // Skip types whose fields aren't accessible from sibling code in the
        // same assembly (private/protected nested classes commonly used for
        // test fixtures). The runtime registry's reflection scan still picks
        // them up — we just can't reference them from generated source.
        if (!IsAccessibleFromAssembly(symbol)) return null;

        // Walk the inheritance chain looking for our StyleSheet.
        var baseType = symbol.BaseType;
        while (baseType is not null) {
            if (baseType.ToDisplayString() == StyleSheetFullName) {
                return new StyleSheetInfo(symbol);
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    private static bool IsAccessibleFromAssembly(INamedTypeSymbol symbol) {
        // Walk outward; if any ancestor is private/protected, the inner type
        // can't be referenced by name from generated assembly-internal code.
        var current = symbol;
        while (current is not null) {
            switch (current.DeclaredAccessibility) {
                case Accessibility.Public:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    break;
                default:
                    return false;
            }
            current = current.ContainingType;
        }
        return true;
    }

    private static string EmitInitializer(IReadOnlyCollection<StyleSheetInfo> classes, string globalPrefix) {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by BrowserApi.Css.SourceGen — do not edit.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine("namespace BrowserApi.Css.SourceGen.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Module initializer that pre-populates Class/CssVar/Keyframes names");
        sb.AppendLine("/// on every StyleSheet-derived type discovered at compile time.");
        sb.AppendLine("/// Runs once when the module is loaded, BEFORE any user code.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static class BrowserApiCssInitializer {");
        sb.AppendLine("    [ModuleInitializer]");
        sb.AppendLine("    internal static void Initialize() {");
        sb.AppendLine("        // The runtime registry's lazy scan still handles fields the");
        sb.AppendLine("        // generator missed (e.g. assemblies loaded after build), but");
        sb.AppendLine("        // typed-and-known fields are populated here for zero startup cost.");

        foreach (var info in classes.OrderBy(c => c.Symbol.ToDisplayString())) {
            EmitFieldsForStyleSheet(sb, info, globalPrefix);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitFieldsForStyleSheet(StringBuilder sb, StyleSheetInfo info, string globalPrefix) {
        var typeName = info.Symbol.ToDisplayString();
        var prefixAttr = info.Symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "BrowserApi.Css.Authoring.PrefixAttribute");
        var sheetPrefix = prefixAttr?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

        sb.AppendLine();
        sb.AppendLine($"        // {typeName}");
        foreach (var member in info.Symbol.GetMembers().OfType<IFieldSymbol>()) {
            if (!member.IsStatic || !member.IsReadOnly) continue;
            // Only emit for fields the generated module-init code can actually
            // reach. Private/protected fields stay invisible — the runtime
            // registry's reflection scan handles them via private-binding-flags.
            if (member.DeclaredAccessibility != Accessibility.Public &&
                member.DeclaredAccessibility != Accessibility.Internal &&
                member.DeclaredAccessibility != Accessibility.ProtectedOrInternal) {
                continue;
            }
            var fieldType = member.Type.ToDisplayString();
            var baseName = ToKebabCase(member.Name);
            // Note: global prefix is a runtime option; we don't bake it into
            // the source-gen output. The runtime pass-2 still applies it before
            // rendering. The generator only fills in the BASE per-stylesheet name.

            // Compose stylesheet-prefix segment if [Prefix] is present.
            // Same composition as StyleSheet.ComposeName: global-sheet-field.
            var nameForEmit = string.Join("-",
                new[] { globalPrefix, sheetPrefix, baseName }.Where(p => !string.IsNullOrEmpty(p)));

            // Match by type name to avoid bringing the runtime types into the
            // analyzer's compile context.
            if (fieldType == "BrowserApi.Css.Authoring.Class" ||
                fieldType == "BrowserApi.Css.Authoring.Keyframes") {
                sb.AppendLine($"        if (string.IsNullOrEmpty({typeName}.{member.Name}.Name)) {typeName}.{member.Name}.Name = \"{nameForEmit}\";");
            } else if (fieldType.StartsWith("BrowserApi.Css.Authoring.CssVar<", System.StringComparison.Ordinal)) {
                sb.AppendLine($"        if (string.IsNullOrEmpty({typeName}.{member.Name}.Name)) {typeName}.{member.Name}.Name = \"--{nameForEmit}\";");
            }
        }
    }

    private static string ToKebabCase(string pascal) {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var sb = new StringBuilder(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++) {
            var ch = pascal[i];
            if (i > 0 && char.IsUpper(ch)) sb.Append('-');
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private readonly struct StyleSheetInfo {
        public StyleSheetInfo(INamedTypeSymbol symbol) { Symbol = symbol; }
        public INamedTypeSymbol Symbol { get; }
    }
}
