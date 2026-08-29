using BrowserApi.Css.Authoring;
using BrowserApi.Css.Compiler;

namespace BrowserApi.Css.Compiler.Tests {

/// <summary>
/// The island compiler must produce exactly what the in-app renderer produces
/// for the same source — that equivalence is the whole premise of modes 2 and 3.
/// </summary>
[Collection("CssRegistry")]
public class CssIslandCompilerTests {
    private const string Tokens = """
        using BrowserApi.Css;
        using BrowserApi.Css.Authoring;
        using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

        namespace Island;

        public class Tokens : StyleSheet {
            public static readonly CssVar<Length> SpacingMd = new(Length.Px(12));
        }
        """;

    private const string App = """
        using BrowserApi.Css;
        using BrowserApi.Css.Authoring;
        using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

        namespace Island;

        public class AppStyles : StyleSheet {
            public static readonly Class Btn = new() {
                Padding = Tokens.SpacingMd,
                [Self.Hover] = new() { Padding = Length.Px(4) },
            };
        }
        """;

    [Fact]
    public void Compiles_island_and_renders_every_stylesheet() {
        var result = new CssIslandCompiler().CompileSources([("Tokens.css.cs", Tokens), ("App.css.cs", App)]);

        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        Assert.Equal(["Island.AppStyles", "Island.Tokens"], result.CssByType.Keys);
        Assert.Contains(".btn { padding: var(--spacing-md); }", result.CombinedCss);
        Assert.Contains(".btn:hover { padding: 4px; }", result.CombinedCss);
        Assert.Contains(":root { --spacing-md: 12px; }", result.CombinedCss);
    }

    [Fact]
    public void Cross_sheet_reference_resolves_regardless_of_compile_order() {
        // AppStyles sorts before Tokens, so its cctor runs first — the case that
        // used to yield "var()" before CssVar learned to late-bind its name.
        var result = new CssIslandCompiler().CompileSources([("App.css.cs", App), ("Tokens.css.cs", Tokens)]);
        Assert.Contains("var(--spacing-md)", result.CombinedCss);
        Assert.DoesNotContain("var()", result.CombinedCss);
    }

    [Fact]
    public void Island_output_matches_in_process_renderer_for_same_source() {
        var island = new CssIslandCompiler().CompileSources([("Tokens.css.cs", Tokens), ("App.css.cs", App)]);
        // The same two classes compiled into THIS test assembly, rendered by the
        // runtime path. Namespaces differ, so compare per-sheet bodies.
        var runtime = Authoring.StyleSheet.RenderAll([typeof(Local.Tokens), typeof(Local.AppStyles)]);

        Assert.Equal(runtime[typeof(Local.AppStyles).FullName!], island.CssByType["Island.AppStyles"]);
        Assert.Equal(runtime[typeof(Local.Tokens).FullName!], island.CssByType["Island.Tokens"]);
    }

    [Fact]
    public void Compile_error_is_reported_not_thrown() {
        var result = new CssIslandCompiler().CompileSources([("Bad.css.cs", "public class X : BrowserApi.Css.Authoring.StyleSheet { public static readonly int Y = \"no\"; }")]);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Id == "CS0029");
        Assert.Empty(result.CssByType);
    }

    [Fact]
    public void Island_assemblies_are_invisible_to_the_registry_scan() {
        new CssIslandCompiler().CompileSources([("Tokens.css.cs", Tokens)]);
        CssRegistry.Refresh();
        Assert.DoesNotContain(CssRegistry.DiscoveredStyleSheets, t => t.FullName == "Island.Tokens");
    }

    [Fact]
    public void Override_replaces_registry_output_and_raises_Changed() {
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        CssRegistry.Changed += handler;
        try {
            var result = new CssIslandCompiler().CompileSources([("Tokens.css.cs", Tokens)]);
            CssRegistry.SetOverride(result.CssByType);

            Assert.True(CssRegistry.HasOverride);
            Assert.Equal(result.CombinedCss, CssRegistry.GetCombinedCss());
            Assert.Equal(1, raised);
        } finally {
            CssRegistry.SetOverride(null);
            CssRegistry.Changed -= handler;
        }
        Assert.False(CssRegistry.HasOverride);
    }
}
}

namespace Local {
    using BrowserApi.Css;
    using BrowserApi.Css.Authoring;
    using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

    public class Tokens : StyleSheet {
        public static readonly CssVar<Length> SpacingMd = new(Length.Px(12));
    }

    public class AppStyles : StyleSheet {
        public static readonly Class Btn = new() {
            Padding = Tokens.SpacingMd,
            [Self.Hover] = new() { Padding = Length.Px(4) },
        };
    }
}

namespace AppSide {
    /// <summary>Stands in for "the app assembly" in the AllowAppReferences test.</summary>
    public static class AppConstants { public const int RowHeight = 42; }
}

namespace BrowserApi.Css.Compiler.Tests {
    public class AppReferenceTests {
        private const string Sheet = """
            using BrowserApi.Css;
            using BrowserApi.Css.Authoring;
            public class Rows : BrowserApi.Css.Authoring.StyleSheet {
                public static readonly Class Row = new() { Height = Length.Px(AppSide.AppConstants.RowHeight) };
            }
            """;

        [Fact]
        public void Closed_island_rejects_app_references_by_default() {
            var result = new CssIslandCompiler().CompileSources([("Rows.css.cs", Sheet)]);
            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics, d => d.Id == "CS0246" || d.Id == "CS0103");
        }

        [Fact]
        public void App_references_compile_when_the_app_assembly_is_referenced() {
            var result = new CssIslandCompiler(CssIslandCompiler.AppReferences()).CompileSources([("Rows.css.cs", Sheet)]);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics));
            Assert.Contains(".row { height: 42px; }", result.CombinedCss);
        }
    }
}
