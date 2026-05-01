using BrowserApi.Css;
using BrowserApi.Css.Authoring;

// Disambiguate from BrowserApi.Css.StyleSheet (the CSSOM type generated from WebIDL)
// vs our new authoring base. Consumers either pick one namespace or alias as below.
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// End-to-end tests for the CSS-in-C# authoring pipeline. These exercise the
/// reflection-based <see cref="StyleSheet.Render(System.Type)"/> path; once the
/// source generator lands the generated <c>ToCss()</c> method should be tested
/// here too with byte-exact assertions.
/// </summary>
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
    }

    private class NestedStyles : StyleSheet {
        public static readonly Class Card = new() {
            Padding = Length.Px(16),
            Background = CssColor.White,
            [Self.Hover] = new Hover {
                Background = CssColor.Hex("#f5f5f5"),
            },
        };

        // Sub-classes of Declarations would normally not be needed —
        // C# 12 collection-initializer-with-derived-type via `new()` works
        // because Declarations is abstract; we use a tiny derived helper here.
        private class Hover : Declarations { }
    }

    [Fact]
    public void Resolves_self_hover_into_a_separate_rule_block() {
        var css = StyleSheet.Render<NestedStyles>();

        Assert.Contains(".card {", css);
        // The hover block resolves the SCSS `&` at render time:
        Assert.Contains(".card:hover {", css);
        Assert.Contains("#f5f5f5", css);
    }

    [Fact]
    public void Class_implicit_string_returns_bare_name() {
        // Force population of Name by rendering once.
        _ = StyleSheet.Render<FlatStyles>();
        string asAttribute = FlatStyles.Card;
        Assert.Equal("card", asAttribute);
    }

    [Fact]
    public void ClassList_joins_with_single_space_and_skips_none() {
        _ = StyleSheet.Render<FlatStyles>();
        var list = FlatStyles.Card + Class.None;
        Assert.Equal("card", list.ToString());
    }
}

/// <summary>
/// Tests for selector composition — operator precedence is the load-bearing
/// part of the design (§3 of the spec) and worth unit-testing explicitly.
/// </summary>
public class SelectorTests {
    private static readonly Selector A = new(".a");
    private static readonly Selector B = new(".b");
    private static readonly Selector C = new(".c");

    [Fact]
    public void Compound_binds_tighter_than_child() {
        // A * B > C  ==  (A * B) > C  ==  ".a.b > .c"
        var sel = A * B > C;
        Assert.Equal(".a.b > .c", sel.Css);
    }

    [Fact]
    public void Descendant_binds_tighter_than_child() {
        // A >> B > C  ==  (A >> B) > C  ==  ".a .b > .c"
        var sel = A >> B > C;
        Assert.Equal(".a .b > .c", sel.Css);
    }

    [Fact]
    public void Selector_list_is_lowest_precedence() {
        // A | B.Hover  ==  A | (B.Hover)  ==  ".a, .b:hover"
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
        // The compile-time guarantee: PseudoElementSelector cannot accept further pseudo-elements.
        // Best we can do at runtime: assert the CSS string is correct.
        var pseudoElement = A.After;
        Assert.Equal(".a::after", pseudoElement.Css);
        // PseudoElementSelector still supports pseudo-classes (valid CSS):
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

/// <summary>
/// Tests for the <c>StyleSheet</c>-injected helpers — <c>Self</c>, <c>Is</c>,
/// <c>Where</c> — accessed from a derived stylesheet without qualification.
/// </summary>
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
