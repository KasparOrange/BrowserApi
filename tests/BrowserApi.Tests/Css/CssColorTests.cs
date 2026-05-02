using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class CssColorTests {
    [Theory]
    [InlineData(0, 0, 0, "rgb(0, 0, 0)")]
    [InlineData(255, 255, 255, "rgb(255, 255, 255)")]
    [InlineData(0, 128, 255, "rgb(0, 128, 255)")]
    public void Rgb_formats_correctly(int r, int g, int b, string expected) {
        Assert.Equal(expected, CssColor.Rgb(r, g, b).ToCss());
    }

    [Theory]
    [InlineData(0, 0, 0, 0, "rgba(0, 0, 0, 0)")]
    [InlineData(255, 255, 255, 1, "rgba(255, 255, 255, 1)")]
    [InlineData(0, 0, 0, 0.5, "rgba(0, 0, 0, 0.5)")]
    public void Rgba_formats_correctly(int r, int g, int b, double a, string expected) {
        Assert.Equal(expected, CssColor.Rgba(r, g, b, a).ToCss());
    }

    [Theory]
    [InlineData(0, 0, 0, "hsl(0, 0%, 0%)")]
    [InlineData(220, 90, 56, "hsl(220, 90%, 56%)")]
    [InlineData(360, 100, 100, "hsl(360, 100%, 100%)")]
    public void Hsl_formats_correctly(int h, int s, int l, string expected) {
        Assert.Equal(expected, CssColor.Hsl(h, s, l).ToCss());
    }

    [Fact]
    public void Hsla_formats_correctly() {
        Assert.Equal("hsla(220, 90%, 56%, 0.5)", CssColor.Hsla(220, 90, 56, 0.5).ToCss());
    }

    [Fact]
    public void Hex_valid_long_form() {
        Assert.Equal("#ff0000", CssColor.Hex("#ff0000").ToCss());
    }

    [Fact]
    public void Hex_valid_short_form() {
        Assert.Equal("#f00", CssColor.Hex("#f00").ToCss());
    }

    [Fact]
    public void Hex_preserves_case() {
        Assert.Equal("#FF0000", CssColor.Hex("#FF0000").ToCss());
    }

    [Fact]
    public void Hex_valid_long_form_with_alpha() {
        Assert.Equal("#0000001f", CssColor.Hex("#0000001f").ToCss());
    }

    [Fact]
    public void Hex_valid_short_form_with_alpha() {
        Assert.Equal("#f008", CssColor.Hex("#f008").ToCss());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("ff0000")]
    [InlineData("#ff")]
    [InlineData("#ff00000")]
    public void Hex_throws_on_invalid_format(string hex) {
        Assert.Throws<ArgumentException>(() => CssColor.Hex(hex));
    }

    [Theory]
    [InlineData("black")]
    [InlineData("white")]
    [InlineData("red")]
    [InlineData("green")]
    [InlineData("blue")]
    [InlineData("transparent")]
    public void Named_colors_output_name(string name) {
        var color = name switch {
            "black" => CssColor.Black,
            "white" => CssColor.White,
            "red" => CssColor.Red,
            "green" => CssColor.Green,
            "blue" => CssColor.Blue,
            "transparent" => CssColor.Transparent,
            _ => throw new ArgumentException()
        };
        Assert.Equal(name, color.ToCss());
    }

    [Fact]
    public void CurrentColor_outputs_lowercase() {
        Assert.Equal("currentcolor", CssColor.CurrentColor.ToCss());
    }

    [Fact]
    public void Inherit_outputs_inherit() {
        Assert.Equal("inherit", CssColor.Inherit.ToCss());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(CssColor.Rgb(0, 0, 0), CssColor.Rgb(0, 0, 0));
        Assert.True(CssColor.Rgb(0, 0, 0) == CssColor.Rgb(0, 0, 0));
        Assert.True(CssColor.Black.Equals(CssColor.Black));
    }

    [Fact]
    public void Different_representations_are_not_equal() {
        Assert.True(CssColor.Rgb(0, 0, 0) != CssColor.Black);
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(CssColor.Rgb(0, 0, 0).GetHashCode(), CssColor.Rgb(0, 0, 0).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(CssColor).ToCss());
    }
}
