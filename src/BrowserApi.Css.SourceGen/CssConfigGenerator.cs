using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BrowserApi.Css.SourceGen;

/// <summary>
/// Makes <c>Program.cs</c> the single place CSS-in-C# is configured.
/// </summary>
/// <remarks>
/// <para>
/// Build-time tools (the <c>browserapi-css</c> static emitter, the analyzers)
/// run before <c>Program.cs</c> executes, so on their own they could not know
/// the prefix or the static asset path. Rather than asking users to repeat
/// those values in MSBuild properties or CLI flags, this generator reads the
/// <c>AddBrowserApiCss(css =&gt; { … })</c> lambda during the normal build,
/// collects every <c>css.X = &lt;constant&gt;</c> assignment, and emits
/// <c>[assembly: BrowserApi.Css.Authoring.BrowserApiCssConfig(…)]</c>. Downstream
/// tools read that attribute from the compiled DLL.
/// </para>
/// <para>
/// Only constant right-hand sides are captured — that is what an attribute can
/// hold, and what a build-time tool can know. Anything else is reported by
/// <see cref="CssConfigAnalyzer"/> (BCA005) as invisible to the build; it still
/// takes effect at runtime.
/// </para>
/// </remarks>
[Generator]
public sealed class CssConfigGenerator : IIncrementalGenerator {
    internal const string MethodName = "AddBrowserApiCss";
    internal const string AttributeFullName = "BrowserApi.Css.Authoring.BrowserApiCssConfig";

    /// <summary>Option names the attribute can carry, with their attribute literal kind.</summary>
    internal static readonly ImmutableDictionary<string, SpecialType> BuildTimeOptions =
        ImmutableDictionary.CreateRange(new[] {
            new KeyValuePair<string, SpecialType>("GlobalPrefix", SpecialType.System_String),
            new KeyValuePair<string, SpecialType>("StaticAssetPath", SpecialType.System_String),
            new KeyValuePair<string, SpecialType>("AllowAppReferences", SpecialType.System_Boolean),
        });

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var lambdas = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsAddBrowserApiCssCall(node),
                transform: static (ctx, _) => Collect(ctx.SemanticModel, (InvocationExpressionSyntax)ctx.Node))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value);

        context.RegisterSourceOutput(lambdas.Collect(), static (spc, all) => {
            if (all.IsDefaultOrEmpty) return;
            // Several call sites would be a user error (registry is process-global);
            // take the first in source order and let runtime behavior match.
            var values = all[0];
            if (values.Length == 0) return;
            spc.AddSource("BrowserApiCssConfig.g.cs", Emit(values));
        });
    }

    internal static bool IsAddBrowserApiCssCall(SyntaxNode node) =>
        node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: MethodName } } call
            && call.ArgumentList.Arguments.Count == 1;

    /// <summary>
    /// Finds <c>param.Option = constant</c> assignments in the lambda body.
    /// Returns null when the argument is not a lambda at all.
    /// </summary>
    internal static ImmutableArray<(string Name, object? Value)>? Collect(SemanticModel model, InvocationExpressionSyntax call) {
        var arg = call.ArgumentList.Arguments[0].Expression;
        if (arg is not LambdaExpressionSyntax lambda) return null;

        var paramName = lambda switch {
            SimpleLambdaExpressionSyntax s => s.Parameter.Identifier.ValueText,
            ParenthesizedLambdaExpressionSyntax p when p.ParameterList.Parameters.Count == 1 => p.ParameterList.Parameters[0].Identifier.ValueText,
            _ => null,
        };
        if (paramName is null) return null;

        var result = ImmutableArray.CreateBuilder<(string, object?)>();
        foreach (var assignment in lambda.Body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>()) {
            if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)) continue;
            if (assignment.Left is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax id } member) continue;
            if (id.Identifier.ValueText != paramName) continue;

            var name = member.Name.Identifier.ValueText;
            if (!BuildTimeOptions.ContainsKey(name)) continue;

            var constant = model.GetConstantValue(assignment.Right);
            if (!constant.HasValue) continue; // BCA005 reports this one
            result.Add((name, constant.Value));
        }
        return result.ToImmutable();
    }

    private static string Emit(ImmutableArray<(string Name, object? Value)> values) {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by BrowserApi.Css.SourceGen from the AddBrowserApiCss(css => …) lambda in this");
        sb.AppendLine("// project. Mirrors the CONSTANT option values so build-time tools (browserapi-css,");
        sb.AppendLine("// analyzers) read the same configuration as the runtime. Edit Program.cs, not this file.");
        sb.AppendLine();
        sb.Append("[assembly: ").Append(AttributeFullName).Append('(');
        var first = true;
        foreach (var (name, value) in values) {
            if (!first) sb.Append(", ");
            first = false;
            sb.Append(name).Append(" = ").Append(Literal(value));
        }
        sb.AppendLine(")]");
        return sb.ToString();
    }

    private static string Literal(object? value) => value switch {
        null => "null",
        string s => SyntaxFactory.Literal(s).ToFullString(),
        bool b => b ? "true" : "false",
        _ => SyntaxFactory.Literal(value.ToString()).ToFullString(),
    };
}

/// <summary>
/// BCA005 — an <c>AddBrowserApiCss</c> option that build-time tools need was
/// assigned a non-constant, so the static emitter and analyzers will not see it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CssConfigAnalyzer : DiagnosticAnalyzer {
    public static readonly DiagnosticDescriptor Rule = new(
        id: "BCA005",
        title: "CSS option is not visible to build-time tools",
        messageFormat: "'{0}' is assigned a non-constant value; the static CSS emitter and analyzers will not see it (runtime is unaffected). Use a literal or const.",
        category: "BrowserApi.Css.Authoring",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "AddBrowserApiCss(css => …) is the single place CSS-in-C# is configured. Build-time " +
            "tools read it through an assembly attribute the source generator emits from the " +
            "lambda, and an attribute can only hold constants. A value computed at runtime " +
            "(configuration, environment) works for the running app but is invisible to " +
            "`browserapi-css build`, which would then emit with defaults and trip the drift guard.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext ctx) {
        var call = (InvocationExpressionSyntax)ctx.Node;
        if (!CssConfigGenerator.IsAddBrowserApiCssCall(call)) return;
        if (call.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda) return;

        var paramName = lambda switch {
            SimpleLambdaExpressionSyntax s => s.Parameter.Identifier.ValueText,
            ParenthesizedLambdaExpressionSyntax p when p.ParameterList.Parameters.Count == 1 => p.ParameterList.Parameters[0].Identifier.ValueText,
            _ => null,
        };
        if (paramName is null) return;

        foreach (var assignment in lambda.Body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>()) {
            if (assignment.Left is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax id } member) continue;
            if (id.Identifier.ValueText != paramName) continue;
            var name = member.Name.Identifier.ValueText;
            if (!CssConfigGenerator.BuildTimeOptions.ContainsKey(name)) continue;
            if (ctx.SemanticModel.GetConstantValue(assignment.Right).HasValue) continue;

            ctx.ReportDiagnostic(Diagnostic.Create(Rule, assignment.Right.GetLocation(), name));
        }
    }
}
