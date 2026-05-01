using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>
/// Tests for the spec §17 <see cref="LengthOrPercentage"/> union wrapper.
/// Properties that genuinely accept both length and percentage (Width, Height,
/// padding, etc.) take this type so both forms compile; properties that don't
/// (FontWeight, LineHeight, Opacity) only take their narrower input.
/// </summary>
[Collection(nameof(CssRegistryCollection))]
public class UnionTypeTests {
    private class WidthHeightStyles : StyleSheet {
        public static readonly Class Sized = new() {
            Width  = Length.Px(200),
            Height = Length.Percent(50),
            MinWidth = Length.Em(10),
            MaxHeight = Length.Vh(80),
        };

        public static readonly Class Pct = new() {
            Width  = (LengthOrPercentage)Length.Percent(80),
            Height = (LengthOrPercentage)Length.Percent(100),
        };
    }

    [Fact]
    public void Length_value_compiles_for_width_height() {
        var css = StyleSheet.Render<WidthHeightStyles>();
        Assert.Contains(".sized {", css);
        Assert.Contains("width: 200px;", css);
        Assert.Contains("height: 50%;", css);
        Assert.Contains("min-width: 10em;", css);
        Assert.Contains("max-height: 80vh;", css);
    }

    [Fact]
    public void Percentage_value_compiles_for_width_height() {
        var css = StyleSheet.Render<WidthHeightStyles>();
        Assert.Contains(".pct {", css);
        Assert.Contains("width: 80%;", css);
        Assert.Contains("height: 100%;", css);
    }

    [Fact]
    public void Length_or_percentage_wrapper_accepts_both_primitives_directly() {
        // Length → LengthOrPercentage
        LengthOrPercentage a = Length.Px(16);
        Assert.Equal("16px", a.ToCss());

        // Percentage → LengthOrPercentage
        LengthOrPercentage b = Percentage.Of(50);
        Assert.Equal("50%", b.ToCss());

        // CssVar<Length> → LengthOrPercentage (var(--name))
        var v = CssVar.External<Length>("--gap");
        LengthOrPercentage c = v;
        Assert.Equal("var(--gap)", c.ToCss());
    }

    [Fact]
    public void Sides_accepts_percentage_via_implicit_conversion() {
        // Padding = 50.Percent() — single percentage applied to all sides.
        Sides padding = Length.Percent(50);
        Assert.Equal("50%", padding.ToCss());

        // Two-axis tuple of LengthOrPercentage.
        Sides margins = ((LengthOrPercentage)Length.Px(16), (LengthOrPercentage)Length.Percent(10));
        Assert.Equal("16px 10%", margins.ToCss());
    }
}
