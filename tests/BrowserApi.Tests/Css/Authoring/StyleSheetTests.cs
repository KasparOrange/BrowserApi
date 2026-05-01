using System.Linq;
using BrowserApi.Css;
using BrowserApi.Css.Authoring;

// Disambiguate from BrowserApi.Css.StyleSheet (the CSSOM type generated from WebIDL).
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>End-to-end tests for the CSS-in-C# authoring pipeline.</summary>
[Collection(nameof(CssRegistryCollection))]
public class StyleSheetTests {
    private class FlatStyles : StyleSheet {
        public static readonly Class Card = new() {
            Padding = Length.Px(16),
            Background = CssColor.White,
            BorderRadius = Length.Px(8),
        };
    }

    [Fact]
    public void Renders_a_flat_class_in_source_order() {
        var css = StyleSheet.Render<FlatStyles>();

        Assert.Contains(".card {", css);
        Assert.Contains("padding: 16px;", css);
        Assert.Contains("background:", css);
        Assert.Contains("border-radius: 8px;", css);

        // Source order check: padding precedes border-radius in output.
        var paddingIdx = css.IndexOf("padding:", System.StringComparison.Ordinal);
        var radiusIdx = css.IndexOf("border-radius:", System.StringComparison.Ordinal);
        Assert.True(paddingIdx < radiusIdx, "Source order should be preserved.");
    }

    private class NestedStyles : StyleSheet {
        public static readonly Class Card = new() {
            Padding = Length.Px(16),
            Background = CssColor.White,
            [Self.Hover] = new() {
                Background = CssColor.Hex("#f5f5f5"),
            },
            [Self > El.A] = new() {
                Color = CssColor.Hex("#0066cc"),
            },
        };
    }

    [Fact]
    public void Resolves_self_hover_into_a_separate_rule_block() {
        var css = StyleSheet.Render<NestedStyles>();

        Assert.Contains(".card {", css);
        Assert.Contains(".card:hover {", css);
        Assert.Contains(".card > a {", css);
        Assert.Contains("#f5f5f5", css);
    }

    [Fact]
    public void Concrete_Declarations_supports_target_typed_new() {
        // Lock in the ergonomic guarantee: `new() { ... }` works inside the
        // nesting indexer without a helper subclass — Declarations is concrete.
        var instance = new Declarations { Padding = Length.Px(4) };
        Assert.Single(instance.Properties);
        Assert.Equal("padding", instance.Properties[0].Key);
        Assert.Equal("4px", instance.Properties[0].Value);
    }

    [Fact]
    public void Class_implicit_string_returns_bare_name_via_lazy_scan() {
        // Critical path: in Razor users write `class="@AppStyles.Card"` BEFORE
        // any Render() call. The lazy scan in the implicit-string conversion
        // populates Name on demand.
        CssRegistry.Refresh(); // force a clean state
        string asAttribute = FlatStyles.Card;
        Assert.Equal("card", asAttribute);
    }

    [Fact]
    public void ClassList_joins_with_single_space_and_skips_none() {
        _ = StyleSheet.Render<FlatStyles>();
        var list = FlatStyles.Card + Class.None;
        Assert.Equal("card", list.ToString());
    }

    [Fact]
    public void ClassList_supports_string_escape_hatch() {
        _ = StyleSheet.Render<FlatStyles>();
        var list = FlatStyles.Card + "vendor-specific";
        Assert.Equal("card vendor-specific", list.ToString());
    }
}

/// <summary>Tests for the auto-discovery registry.</summary>
[Collection(nameof(CssRegistryCollection))]
public class CssRegistryTests {
    private class RegistryStyles : StyleSheet {
        public static readonly Class Widget = new() {
            Color = CssColor.Hex("#123456"),
        };
    }

    [Fact]
    public void Registry_discovers_all_stylesheet_types_in_appdomain() {
        CssRegistry.Refresh();
        var types = CssRegistry.DiscoveredStyleSheets;

        Assert.Contains(typeof(RegistryStyles), types);
    }

    [Fact]
    public void Registry_renders_combined_css_for_all_stylesheets() {
        CssRegistry.Refresh();
        var css = CssRegistry.GetCombinedCss();
        Assert.Contains(".widget", css);
        Assert.Contains("#123456", css);
    }

    [Fact]
    public void Registry_can_render_a_single_stylesheet_by_type() {
        CssRegistry.Refresh();
        var single = CssRegistry.GetCss<RegistryStyles>();
        Assert.Contains(".widget", single);
    }
}

/// <summary>Selector composition / operator-precedence tests.</summary>
public class SelectorTests {
    private static readonly Selector A = new(".a");
    private static readonly Selector B = new(".b");
    private static readonly Selector C = new(".c");

    [Fact]
    public void Compound_binds_tighter_than_child() {
        var sel = A * B > C;
        Assert.Equal(".a.b > .c", sel.Css);
    }

    [Fact]
    public void Descendant_binds_tighter_than_child() {
        var sel = A >> B > C;
        Assert.Equal(".a .b > .c", sel.Css);
    }

    [Fact]
    public void Selector_list_is_lowest_precedence() {
        var sel = A | B.Hover;
        Assert.Equal(".a, .b:hover", sel.Css);
    }

    [Fact]
    public void Pseudo_class_chains() {
        var sel = A.Hover.FocusVisible;
        Assert.Equal(".a:hover:focus-visible", sel.Css);
    }

    [Fact]
    public void Adjacent_and_general_sibling_use_plus_and_minus() {
        Assert.Equal(".a + .b", (A + B).Css);
        Assert.Equal(".a ~ .b", (A - B).Css);
    }

    [Fact]
    public void Pseudo_element_returns_terminal_type() {
        var pseudoElement = A.After;
        Assert.Equal(".a::after", pseudoElement.Css);
        Assert.Equal(".a::after:hover", pseudoElement.Hover.Css);
    }

    [Fact]
    public void Reverse_operators_throw_with_a_helpful_message() {
        var ex = Assert.Throws<System.NotSupportedException>(() => _ = A < B);
        Assert.Contains("'>'", ex.Message);
    }

    [Fact]
    public void El_predefined_selectors_compose() {
        var sel = El.Article >> El.A.Hover;
        Assert.Equal("article a:hover", sel.Css);

        var list = El.H1 | El.H2 | El.H3;
        Assert.Equal("h1, h2, h3", list.Css);
    }
}

/// <summary>StyleSheet-injected helpers (Self, Is, Where).</summary>
public class InjectedHelperTests : StyleSheet {
    [Fact]
    public void Where_groups_selectors_with_zero_specificity_intent() {
        var sel = Where(El.H1, El.H2, El.H3);
        Assert.Equal(":where(h1, h2, h3)", sel.Css);
    }

    [Fact]
    public void Is_groups_selectors_with_max_specificity_intent() {
        var sel = Is(Self.Hover, Self.FocusVisible);
        Assert.Equal(":is(&:hover, &:focus-visible)", sel.Css);
    }

    [Fact]
    public void Self_resolves_to_ampersand() {
        Assert.Equal("&", Self.Css);
        Assert.Equal("&:hover", Self.Hover.Css);
    }
}

/// <summary>Coverage for the expanded property surface — keyword enums, layout, etc.</summary>
public class PropertySurfaceTests {
    private class WidePropStyles : StyleSheet {
        public static readonly Class Btn = new() {
            Display = Display.InlineFlex,
            AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center,
            Gap = Length.Px(8),
            Padding = Length.Px(8),
            Border = Border.Solid(Length.Px(1), CssColor.Hex("#ccc")),
            BorderRadius = Length.Px(4),
            Cursor = Cursor.Pointer,
            FontWeight = 600,
            TextTransform = TextTransform.Uppercase,
            BoxSizing = BrowserApi.Css.BoxSizing.BorderBox,
        };
    }

    [Fact]
    public void Display_enum_serializes_kebab_case() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("display: inline-flex;", css);
    }

    [Fact]
    public void Align_and_justify_keywords_emit_correctly() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("align-items: center;", css);
        Assert.Contains("justify-content: center;", css);
    }

    [Fact]
    public void Border_factory_emits_shorthand() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("border: 1px solid #ccc;", css);
    }

    [Fact]
    public void Cursor_keyword_is_kebab_cased() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("cursor: pointer;", css);
    }

    [Fact]
    public void Numeric_font_weight_emits_unitless() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("font-weight: 600;", css);
        Assert.DoesNotContain("font-weight: 600px", css);
    }

    [Fact]
    public void Text_transform_kebab_cases() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("text-transform: uppercase;", css);
    }

    [Fact]
    public void Box_sizing_kebab_cases() {
        var css = StyleSheet.Render<WidePropStyles>();
        Assert.Contains("box-sizing: border-box;", css);
    }
}
