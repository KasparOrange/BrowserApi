using System.Collections.Immutable;
using BrowserApi.Css.Authoring;
using BrowserApi.Css.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BrowserApi.Css.Compiler.Tests;

/// <summary>
/// Program.cs is the single configuration place. These tests pin the bridge
/// that makes that true for build-time tools: lambda literals → assembly
/// attribute, non-literals → BCA005.
/// </summary>
public class CssConfigGeneratorTests {
    private const string Stub = """
        using System;
        using BrowserApi.Css.Authoring;
        namespace Microsoft.Extensions.DependencyInjection {
            public interface IServiceCollection { }
            public static class Ext {
                public static IServiceCollection AddBrowserApiCss(this IServiceCollection s, Action<CssOptions>? c = null) => s;
            }
        }
        """;

    [Fact]
    public void Literal_and_const_assignments_become_an_assembly_attribute() {
        var program = """
            using Microsoft.Extensions.DependencyInjection;
            static class P {
                const string Prefix = "mw";
                static void Main(IServiceCollection services) {
                    services.AddBrowserApiCss(css => {
                        css.GlobalPrefix = Prefix;
                        css.StaticAssetPath = "css/app.css";
                        css.AllowAppReferences = true;
                        css.HotReload = true;               // runtime-only, not mirrored
                    });
                }
            }
            """;
        var (generated, diagnostics) = Run(program);

        Assert.Empty(diagnostics);
        var attr = Assert.Single(generated, t => t.FilePath.EndsWith("BrowserApiCssConfig.g.cs"));
        var text = attr.ToString();
        Assert.Contains("[assembly: BrowserApi.Css.Authoring.BrowserApiCssConfig(GlobalPrefix = \"mw\", StaticAssetPath = \"css/app.css\", AllowAppReferences = true)]", text);
    }

    [Fact]
    public void Non_constant_assignment_is_skipped_and_reported_as_BCA005() {
        var program = """
            using Microsoft.Extensions.DependencyInjection;
            static class P {
                static void Main(IServiceCollection services, string fromConfig) {
                    services.AddBrowserApiCss(css => {
                        css.GlobalPrefix = fromConfig;
                        css.StaticAssetPath = "css/app.css";
                    });
                }
            }
            """;
        var (generated, diagnostics) = Run(program);

        var d = Assert.Single(diagnostics, d => d.Id == "BCA005");
        Assert.Contains("'GlobalPrefix'", d.GetMessage());
        var text = Assert.Single(generated, t => t.FilePath.EndsWith("BrowserApiCssConfig.g.cs")).ToString();
        Assert.DoesNotContain("GlobalPrefix", text);
        Assert.Contains("StaticAssetPath = \"css/app.css\"", text);
    }

    [Fact]
    public void No_lambda_means_no_attribute() {
        var program = """
            using Microsoft.Extensions.DependencyInjection;
            static class P { static void Main(IServiceCollection s) { s.AddBrowserApiCss(); } }
            """;
        var (generated, _) = Run(program);
        Assert.DoesNotContain(generated, t => t.FilePath.EndsWith("BrowserApiCssConfig.g.cs"));
    }

    [Fact]
    public void Class_name_initializer_uses_the_global_prefix_from_the_same_lambda() {
        var program = """
            using Microsoft.Extensions.DependencyInjection;
            using BrowserApi.Css.Authoring;
            static class P {
                static void Main(IServiceCollection s) { s.AddBrowserApiCss(css => css.GlobalPrefix = "mw"); }
            }
            [Prefix("sp")]
            public class Sheet : BrowserApi.Css.Authoring.StyleSheet {
                public static readonly Class PeopleList = new();
            }
            """;
        var (generated, _) = Run(program);
        var init = Assert.Single(generated, t => t.FilePath.EndsWith("BrowserApiCssInitializer.g.cs")).ToString();
        Assert.Contains("Sheet.PeopleList.Name = \"mw-sp-people-list\"", init);
    }

    private static (ImmutableArray<SyntaxTree> Generated, ImmutableArray<Diagnostic> Analyzer) Run(string program) {
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p).StartsWith("System", StringComparison.Ordinal) || Path.GetFileName(p) == "netstandard.dll")
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(CssOptions).Assembly.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create("App",
            [CSharpSyntaxTree.ParseText(Stub), CSharpSyntaxTree.ParseText(program)],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new CssConfigGenerator(), new CssClassNameGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var generated = output.SyntaxTrees.Skip(2).ToImmutableArray();

        var analyzerDiags = compilation
            .WithAnalyzers([new CssConfigAnalyzer()])
            .GetAnalyzerDiagnosticsAsync().Result;

        return (generated, analyzerDiags);
    }
}
