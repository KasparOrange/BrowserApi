using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BrowserApi.Css.SourceGen;

/// <summary>
/// Analyzer <c>BCA001</c> — when a 4-element tuple is implicitly converted
/// to <c>Sides</c>, the elements should be named (<c>top:</c>, <c>right:</c>,
/// <c>bottom:</c>, <c>left:</c>). Without the names, the order is easy to
/// get wrong (CSS goes top-right-bottom-left clockwise, but the C# reader's
/// intuition often differs). Spec §18.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SidesNamedArgsAnalyzer : DiagnosticAnalyzer {
    private const string SidesFullName = "BrowserApi.Css.Authoring.Sides";

    /// <summary>The BCA001 diagnostic descriptor.</summary>
    public static readonly DiagnosticDescriptor Rule = new(
        id: "BCA001",
        title: "4-element Sides tuple should use named elements",
        messageFormat: "Use named tuple elements or Sides.Of(...) for 4-element Sides tuples",
        category: "BrowserApi.Css.Authoring",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "CSS shorthand sides go top-right-bottom-left clockwise. Without explicit " +
            "element names, the C# reader has to remember that order. Either name the " +
            "tuple elements (top:, right:, bottom:, left:) or use the Sides.Of named-args " +
            "factory.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTuple, SyntaxKind.TupleExpression);
    }

    private static void AnalyzeTuple(SyntaxNodeAnalysisContext ctx) {
        var tuple = (TupleExpressionSyntax)ctx.Node;
        if (tuple.Arguments.Count != 4) return;

        // Only flag when ALL elements are unnamed; partially-named tuples are
        // already trying to communicate intent.
        bool anyNamed = tuple.Arguments.Any(a => a.NameColon is not null);
        if (anyNamed) return;

        // Check the target conversion type — only flag if this 4-tuple is
        // becoming a Sides.
        var convertedType = ctx.SemanticModel.GetTypeInfo(tuple).ConvertedType;
        if (convertedType?.ToDisplayString() != SidesFullName) return;

        ctx.ReportDiagnostic(Diagnostic.Create(Rule, tuple.GetLocation()));
    }
}
