using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BrowserApi.Css.SourceGen;

/// <summary>
/// Analyzer <c>BCA002</c> — flags use of <c>&lt;</c> and <c>&lt;&lt;</c>
/// operators on <see cref="Selector"/> values. C# requires those operators
/// be declared together with their <c>&gt;</c>/<c>&gt;&gt;</c> partners
/// (which we DO want for child / descendant combinators), but they have no
/// CSS equivalent. The runtime <c>NotSupportedException</c> backstop is a
/// last resort; this analyzer turns the misuse into a compile error per
/// spec §3.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelectorOperatorAnalyzer : DiagnosticAnalyzer {
    private const string SelectorFullName = "BrowserApi.Css.Authoring.Selector";
    private const string PseudoElementSelectorFullName = "BrowserApi.Css.Authoring.PseudoElementSelector";

    /// <summary>The BCA002 diagnostic descriptor.</summary>
    public static readonly DiagnosticDescriptor Rule = new(
        id: "BCA002",
        title: "Reverse-direction selector operator has no CSS equivalent",
        messageFormat: "The '{0}' operator is not a CSS combinator. Use '{1}' instead (child/descendant goes left-to-right).",
        category: "BrowserApi.Css.Authoring",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "C# operator-pair declaration rules force us to declare '<' alongside '>' and " +
            "'<<' alongside '>>'. Only the right-pointing variants map to CSS combinators " +
            "(child and descendant); the left-pointing ones are inert and throw at runtime. " +
            "Use '>' for child and '>>' for descendant; if you wanted the OTHER element on the " +
            "right side, simply swap the operands.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeBinary, SyntaxKind.LessThanExpression, SyntaxKind.LeftShiftExpression);
    }

    private static void AnalyzeBinary(SyntaxNodeAnalysisContext ctx) {
        var binary = (BinaryExpressionSyntax)ctx.Node;
        var leftType = ctx.SemanticModel.GetTypeInfo(binary.Left).Type;
        var rightType = ctx.SemanticModel.GetTypeInfo(binary.Right).Type;
        if (!IsSelectorish(leftType) && !IsSelectorish(rightType)) return;

        var op = binary.OperatorToken.Text;
        var preferred = op == "<" ? ">" : ">>";
        ctx.ReportDiagnostic(Diagnostic.Create(Rule, binary.OperatorToken.GetLocation(), op, preferred));
    }

    private static bool IsSelectorish(ITypeSymbol? type) {
        if (type is null) return false;
        var name = type.ToDisplayString();
        if (name == SelectorFullName || name == PseudoElementSelectorFullName) return true;
        // Cover Class and other types that implicitly convert to Selector.
        foreach (var iface in type.AllInterfaces) {
            if (iface.ToDisplayString() == SelectorFullName) return true;
        }
        return false;
    }
}
