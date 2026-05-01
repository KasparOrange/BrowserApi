using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for the global + per-stylesheet prefix system.</summary>
[Collection(nameof(CssRegistryCollection))]
public class PrefixTests {
    [Prefix("sp")]
    private class ShiftPlannerStyles : StyleSheet {
        public static readonly Class PeopleList = new() {
            Padding = Length.Px(8),
        };

        public static readonly CssVar<Length> Spacing = new(Length.Px(12));
    }

    public static class GlobalScope {
        // Tests below configure CssRegistry with a global prefix and inspect output.
    }

    [Fact]
    public void Per_stylesheet_prefix_attribute_prepends_to_class_name() {
        // Reset to a known state with no global prefix.
        CssRegistry.Configure(new CssOptions { GlobalPrefix = "" });

        var css = StyleSheet.Render<ShiftPlannerStyles>();
        Assert.Contains(".sp-people-list {", css);
        Assert.DoesNotContain(".people-list {", css);
    }

    [Fact]
    public void Global_plus_per_stylesheet_chain_with_dashes() {
        CssRegistry.Configure(new CssOptions { GlobalPrefix = "mw" });

        var css = StyleSheet.Render<ShiftPlannerStyles>();
        Assert.Contains(".mw-sp-people-list {", css);

        // CssVar names also chain.
        Assert.Contains("--mw-sp-spacing:", css);
    }

    [Fact]
    public void Class_implicit_string_returns_fully_prefixed_name_for_razor() {
        CssRegistry.Configure(new CssOptions { GlobalPrefix = "mw" });

        // Implicit string conversion is what Razor uses for `class="@..."`.
        string asAttribute = ShiftPlannerStyles.PeopleList;
        Assert.Equal("mw-sp-people-list", asAttribute);
    }

    [Fact]
    public void Reconfiguring_options_refreshes_the_render_cache() {
        CssRegistry.Configure(new CssOptions { GlobalPrefix = "a" });
        var firstCss = CssRegistry.GetCombinedCss();
        Assert.Contains(".a-sp-people-list", firstCss);

        CssRegistry.Configure(new CssOptions { GlobalPrefix = "b" });
        var secondCss = CssRegistry.GetCombinedCss();
        Assert.Contains(".b-sp-people-list", secondCss);
        Assert.DoesNotContain(".a-sp-people-list", secondCss);
    }

    public class UnprefixedSheet : StyleSheet {
        public static readonly Class Card = new() { Padding = Length.Px(4) };
    }

    [Fact]
    public void Stylesheet_without_attribute_omits_sheet_segment() {
        CssRegistry.Configure(new CssOptions { GlobalPrefix = "mw" });
        var css = StyleSheet.Render<UnprefixedSheet>();
        Assert.Contains(".mw-card {", css);
        Assert.DoesNotContain(".mw--card {", css);  // no double-dash from empty middle segment
    }
}
