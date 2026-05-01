using BrowserApi.Css;
using BrowserApi.Css.Authoring;
// Disambiguate from the CSSOM-generated types in BrowserApi.Css.
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;
using FontFace = BrowserApi.Css.Authoring.FontFace;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for var() fallbacks via .Or().</summary>
public class CssVarOrTests {
    private class FallbackStyles : StyleSheet {
        public static readonly CssVar<CssColor> Brand   = new(CssColor.Hex("#0066cc"));
        public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#003399"));

        public static readonly Class Btn = new() {
            Background = Brand.Or(Primary.Or(CssColor.Blue)),
        };
    }

    [Fact]
    public void Or_emits_var_with_fallback_value() {
        var css = StyleSheet.Render<FallbackStyles>();
        // Inside-out: Primary.Or(Blue) → "var(--primary, blue)", then
        // Brand.Or(that) → "var(--brand, var(--primary, blue))".
        Assert.Contains("background: var(--brand, var(--primary, blue))", css);
    }
}

/// <summary>Tests for @font-face emission.</summary>
public class FontFaceTests {
    private class FontStyles : StyleSheet {
        public static readonly FontFace Inter = new() {
            Family   = "Inter",
            Src      = "url('/fonts/Inter.woff2') format('woff2')",
            Weight   = "400 700",
            Style    = "normal",
            Display  = "swap",
        };
    }

    [Fact]
    public void Font_face_emits_at_font_face_block_with_quoted_family() {
        var css = StyleSheet.Render<FontStyles>();
        Assert.Contains("@font-face {", css);
        Assert.Contains("font-family: \"Inter\";", css);
        Assert.Contains("src: url('/fonts/Inter.woff2') format('woff2');", css);
        Assert.Contains("font-weight: 400 700;", css);
        Assert.Contains("font-display: swap;", css);
    }
}

/// <summary>Tests for auto-emitted @property rules from CssVar&lt;T&gt;.</summary>
public class AtPropertyTests {
    private class TypedTokens : StyleSheet {
        public static readonly CssVar<Length>   Radius  = new(Length.Px(8));
        public static readonly CssVar<CssColor> Primary = new(CssColor.Hex("#0066cc"));
    }

    [Fact]
    public void At_property_block_is_emitted_per_typed_var() {
        var css = StyleSheet.Render<TypedTokens>();
        Assert.Contains("@property --radius {", css);
        Assert.Contains("syntax: \"<length>\";", css);
        Assert.Contains("inherits: true;", css);
        Assert.Contains("initial-value: 8px;", css);

        Assert.Contains("@property --primary {", css);
        Assert.Contains("syntax: \"<color>\";", css);
    }

    private class NonInheritingToken : StyleSheet {
        public static readonly CssVar<Length> Size = new(Length.Px(16)) {
            Inherits = false,
        };
    }

    [Fact]
    public void Inherits_init_property_propagates_to_at_property() {
        var css = StyleSheet.Render<NonInheritingToken>();
        Assert.Contains("inherits: false;", css);
    }
}

/// <summary>Tests for Rules collections of anonymous rules.</summary>
public class RulesCollectionTests {
    private class ResetSheet : StyleSheet {
        public static readonly Rules Reset = new() {
            new Rule(El.All)  { BoxSizing = BrowserApi.Css.BoxSizing.BorderBox },
            new Rule(El.Body) { Margin    = Length.Px(0) },
        };
    }

    [Fact]
    public void Rules_collection_emits_each_rule_in_source_order() {
        var css = StyleSheet.Render<ResetSheet>();
        Assert.Contains("* {", css);
        Assert.Contains("box-sizing: border-box;", css);
        Assert.Contains("body {", css);
        Assert.Contains("margin: 0px;", css);
        // Order check: All before Body.
        var allIdx  = css.IndexOf("* {", System.StringComparison.Ordinal);
        var bodyIdx = css.IndexOf("body {", System.StringComparison.Ordinal);
        Assert.True(allIdx < bodyIdx);
    }
}

/// <summary>Tests for the typed Keyframes → animation-name reference.</summary>
public class KeyframesReferenceTests {
    private class AnimSheet : StyleSheet {
        public static readonly Keyframes SlideIn = new() {
            [From] = new() { Opacity = 0 },
            [To]   = new() { Opacity = 1 },
        };

        public static readonly Class Toast = new() {
            Animation = SlideIn + " 200ms ease-out",
        };
    }

    [Fact]
    public void Animation_reference_resolves_to_kebab_case_name() {
        var css = StyleSheet.Render<AnimSheet>();
        Assert.Contains("@keyframes slide-in {", css);
        Assert.Contains("animation: slide-in 200ms ease-out", css);
    }
}
