using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for the CSS color-manipulation methods.</summary>
[Collection(nameof(CssRegistryCollection))]
public class ColorFunctionTests {
    private static readonly CssColor Blue = CssColor.Hex("#3498db");

    [Fact]
    public void Lighten_emits_relative_color_with_calc_increment() {
        Assert.Equal("hsl(from #3498db h s calc(l + 20%))", Blue.Lighten(20).ToCss());
    }

    [Fact]
    public void Darken_uses_subtraction() {
        Assert.Equal("hsl(from #3498db h s calc(l - 15%))", Blue.Darken(15).ToCss());
    }

    [Fact]
    public void Saturate_modifies_saturation_channel() {
        Assert.Equal("hsl(from #3498db h calc(s + 30%) l)", Blue.Saturate(30).ToCss());
    }

    [Fact]
    public void Adjust_hue_rotates_with_degrees() {
        Assert.Equal("hsl(from #3498db calc(h + 30deg) s l)", Blue.AdjustHue(30).ToCss());
    }

    [Fact]
    public void Complement_property_rotates_180_degrees() {
        Assert.Equal("hsl(from #3498db calc(h + 180deg) s l)", Blue.Complement.ToCss());
    }

    [Fact]
    public void Grayscale_zeros_saturation() {
        Assert.Equal("hsl(from #3498db h 0% l)", Blue.Grayscale.ToCss());
    }

    [Fact]
    public void With_alpha_appends_slash_alpha() {
        Assert.Equal("hsl(from #3498db h s l / 0.5)", Blue.WithAlpha(0.5).ToCss());
    }

    [Fact]
    public void Mix_uses_color_mix_in_srgb() {
        var red = CssColor.Hex("#ff0000");
        Assert.Equal("color-mix(in srgb, #3498db 50%, #ff0000)", Blue.Mix(red, 50).ToCss());
    }

    [Fact]
    public void Functions_compose_with_each_other() {
        var lighter = Blue.Lighten(20).WithAlpha(0.5);
        // Inner expression is preserved verbatim — outer function wraps it.
        Assert.Equal("hsl(from hsl(from #3498db h s calc(l + 20%)) h s l / 0.5)", lighter.ToCss());
    }

    [Fact]
    public void Functions_work_on_variable_backed_colors_when_name_is_known() {
        // Color functions construct their relative-color expression by reading
        // the receiver's current ToCss() string. For a CssVar reference whose
        // name has been populated by the registry scan, that's `var(--name)`.
        // Cctor-time composition doesn't see the populated name (the scan
        // happens after cctor), so apply color functions at runtime — outside
        // a static field initializer — when working from variables.
        BrowserApi.Css.Authoring.CssRegistry.EnsureScanned();

        var primary = CssVar.External<CssColor>("--primary");
        var lighter = ((CssColor)primary).Lighten(15);
        Assert.Equal("hsl(from var(--primary) h s calc(l + 15%))", lighter.ToCss());
    }
}
