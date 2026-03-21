using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class ShadowTests {
    [Fact]
    public void None_outputs_none() {
        Assert.Equal("none", Shadow.None.ToCss());
    }

    [Fact]
    public void Box_minimal() {
        var result = Shadow.Box(Length.Px(2), Length.Px(4));
        Assert.Equal("2px 4px", result.ToCss());
    }

    [Fact]
    public void Box_with_blur() {
        var result = Shadow.Box(Length.Px(2), Length.Px(4), blur: Length.Px(8));
        Assert.Equal("2px 4px 8px", result.ToCss());
    }

    [Fact]
    public void Box_with_all_params() {
        var result = Shadow.Box(Length.Px(2), Length.Px(4),
            blur: Length.Px(8), spread: Length.Px(2), color: CssColor.Black);
        Assert.Equal("2px 4px 8px 2px black", result.ToCss());
    }

    [Fact]
    public void Box_inset() {
        var result = Shadow.Box(Length.Px(0), Length.Px(2),
            blur: Length.Px(4), inset: true);
        Assert.Equal("inset 0px 2px 4px", result.ToCss());
    }

    [Fact]
    public void Box_inset_with_color() {
        var result = Shadow.Box(Length.Px(0), Length.Px(2),
            blur: Length.Px(4), color: CssColor.Rgba(0, 0, 0, 0.1), inset: true);
        Assert.Equal("inset 0px 2px 4px rgba(0, 0, 0, 0.1)", result.ToCss());
    }

    [Fact]
    public void Text_minimal() {
        var result = Shadow.Text(Length.Px(1), Length.Px(1));
        Assert.Equal("1px 1px", result.ToCss());
    }

    [Fact]
    public void Text_with_blur_and_color() {
        var result = Shadow.Text(Length.Px(1), Length.Px(1),
            blur: Length.Px(2), color: CssColor.Red);
        Assert.Equal("1px 1px 2px red", result.ToCss());
    }

    [Fact]
    public void Combine_multiple_shadows() {
        var result = Shadow.Combine(
            Shadow.Box(Length.Px(2), Length.Px(4), blur: Length.Px(8)),
            Shadow.Box(Length.Px(0), Length.Px(0), blur: Length.Px(4)));
        Assert.Equal("2px 4px 8px, 0px 0px 4px", result.ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("none", Shadow.None.ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Shadow.None, Shadow.None);
        Assert.True(Shadow.Box(Length.Px(2), Length.Px(4)) == Shadow.Box(Length.Px(2), Length.Px(4)));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Shadow.Box(Length.Px(2), Length.Px(4)) != Shadow.None);
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Shadow).ToCss());
    }
}
