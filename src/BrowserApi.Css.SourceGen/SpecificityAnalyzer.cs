using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BrowserApi.Css.SourceGen;

/// <summary>
/// Analyzer <c>BCA003</c> — counts compound-selector operators
/// (<c>A * B * C ...</c>) and warns when the chain exceeds the
/// configured threshold. Each <c>*</c> is one class/attribute/pseudo-class
/// in the (b) component of CSS specificity. Spec §35.
/// </summary>
/// <remarks>
/// <para>
/// This is a minimal first pass — checks the most common selectivity
/// problem (over-compound classes) without doing full selector walking.
/// Future iterations could look at descendant chains, attribute selectors,
/// pseudo-class chaining, etc. for a more complete specificity calculation.
/// </para>
/// <para>
/// Threshold is configurable via <c>.editorconfig</c>:
/// </para>
/// <code>
/// [*.cs]
/// browserapi_css_specificity_class_threshold = 2
/// </code>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SpecificityAnalyzer : DiagnosticAnalyzer {
    private const string SelectorFullName = "BrowserApi.Css.Authoring.Selector";
    private const string DefaultThresholdKey = "browserapi_css_specificity_class_threshold";
    private const int DefaultThreshold = 2;

    /// <summary>The BCA003 diagnostic descriptor.</summary>
    public static readonly DiagnosticDescriptor Rule = new(
        id: "BCA003",
        title: "Selector specificity exceeds threshold",
        messageFormat: "Compound selector has {0} class/attribute components — exceeds threshold ({1}). Consider wrapping in :where(...) to flatten specificity to zero.",
        category: "BrowserApi.Css.Authoring",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "High-specificity selectors are difficult to override. Each '*' operator " +
            "in BrowserApi.Css.Authoring is one class/attribute/pseudo-class in CSS " +
            "specificity's (b) component. When the chain gets long, prefer :where(...) " +
            "to flatten to zero, or refactor to fewer modifiers.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMultiply, SyntaxKind.MultiplyExpression);
    }

    private static void AnalyzeMultiply(SyntaxNodeAnalysisContext ctx) {
        var binary = (BinaryExpressionSyntax)ctx.Node;

        // Only check the OUTERMOST `*` in a chain — children will be checked
        // when their own enclosing `*` fires the analysis. We pick the
        // outermost by checking the parent isn't another `*` on Selectors.
        if (binary.Parent is BinaryExpressionSyntax parentBinary &&
            parentBinary.IsKind(SyntaxKind.MultiplyExpression) &&
            IsSelectorMultiply(parentBinary, ctx.SemanticModel)) {
            return;
        }
        if (!IsSelectorMultiply(binary, ctx.SemanticModel)) return;

        // Count terms in the * chain.
        int count = CountMultiplyTerms(binary, ctx.SemanticModel);

        // Read configured threshold from .editorconfig.
        ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
            DefaultThresholdKey, out var thresholdStr);
        var threshold = (int.TryParse(thresholdStr, out var t) && t > 0) ? t : DefaultThreshold;

        if (count <= threshold) return;
        ctx.ReportDiagnostic(Diagnostic.Create(Rule, binary.GetLocation(), count, threshold));
    }

    private static bool IsSelectorMultiply(BinaryExpressionSyntax expr, SemanticModel sem) {
        var leftType = sem.GetTypeInfo(expr.Left).Type;
        var rightType = sem.GetTypeInfo(expr.Right).Type;
        return IsSelectorish(leftType) || IsSelectorish(rightType);
    }

    private static bool IsSelectorish(ITypeSymbol? type) {
        if (type is null) return false;
        if (type.ToDisplayString() == SelectorFullName) return true;
        // Class implicitly converts to Selector.
        var t = type;
        while (t is not null) {
            if (t.ToDisplayString() == "BrowserApi.Css.Authoring.Class") return true;
            t = t.BaseType;
        }
        return false;
    }

    private static int CountMultiplyTerms(BinaryExpressionSyntax binary, SemanticModel sem) {
        // Recursive descent — count leaves in the * chain.
        int Count(ExpressionSyntax expr) {
            if (expr is BinaryExpressionSyntax b && b.IsKind(SyntaxKind.MultiplyExpression) &&
                IsSelectorMultiply(b, sem)) {
                return Count(b.Left) + Count(b.Right);
            }
            return 1;
        }
        return Count(binary);
    }
}
