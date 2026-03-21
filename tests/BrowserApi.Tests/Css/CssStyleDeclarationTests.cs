using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class CssStyleDeclarationTests {
    [Fact]
    public void Can_set_typed_length_property() {
        var style = new CssStyleDeclaration();
        style.Gap = Length.Rem(1.5);
        Assert.Equal("1.5rem", style.Gap.ToCss());
    }

    [Fact]
    public void Can_set_typed_color_property() {
        var style = new CssStyleDeclaration();
        style.BackgroundColor = CssColor.Hex("#ff0000");
        Assert.Equal("#ff0000", style.BackgroundColor.ToCss());
    }

    [Fact]
    public void Can_use_implicit_int_to_length() {
        var style = new CssStyleDeclaration();
        style.Gap = 16;
        Assert.Equal("16px", style.Gap.ToCss());
    }

    [Fact]
    public void SetMargin_all_sets_four_sides() {
        var style = new CssStyleDeclaration();
        style.SetMargin(Length.Px(10));
        Assert.Equal("10px", style.MarginTop.ToCss());
        Assert.Equal("10px", style.MarginRight.ToCss());
        Assert.Equal("10px", style.MarginBottom.ToCss());
        Assert.Equal("10px", style.MarginLeft.ToCss());
    }

    [Fact]
    public void SetMargin_vertical_horizontal() {
        var style = new CssStyleDeclaration();
        style.SetMargin(Length.Px(10), Length.Px(20));
        Assert.Equal("10px", style.MarginTop.ToCss());
        Assert.Equal("20px", style.MarginRight.ToCss());
        Assert.Equal("10px", style.MarginBottom.ToCss());
        Assert.Equal("20px", style.MarginLeft.ToCss());
    }

    [Fact]
    public void SetMargin_four_values() {
        var style = new CssStyleDeclaration();
        style.SetMargin(Length.Px(1), Length.Px(2), Length.Px(3), Length.Px(4));
        Assert.Equal("1px", style.MarginTop.ToCss());
        Assert.Equal("2px", style.MarginRight.ToCss());
        Assert.Equal("3px", style.MarginBottom.ToCss());
        Assert.Equal("4px", style.MarginLeft.ToCss());
    }

    [Fact]
    public void SetPadding_all_sets_four_sides() {
        var style = new CssStyleDeclaration();
        style.SetPadding(Length.Rem(1));
        Assert.Equal("1rem", style.PaddingTop.ToCss());
        Assert.Equal("1rem", style.PaddingRight.ToCss());
        Assert.Equal("1rem", style.PaddingBottom.ToCss());
        Assert.Equal("1rem", style.PaddingLeft.ToCss());
    }

    [Fact]
    public void SetPadding_vertical_horizontal() {
        var style = new CssStyleDeclaration();
        style.SetPadding(Length.Px(10), Length.Px(20));
        Assert.Equal("10px", style.PaddingTop.ToCss());
        Assert.Equal("20px", style.PaddingRight.ToCss());
    }

    [Fact]
    public void SetGap_all() {
        var style = new CssStyleDeclaration();
        style.SetGap(Length.Rem(1));
        Assert.Equal("1rem", style.RowGap.ToCss());
        Assert.Equal("1rem", style.ColumnGap.ToCss());
    }

    [Fact]
    public void SetGap_row_column() {
        var style = new CssStyleDeclaration();
        style.SetGap(Length.Rem(1), Length.Rem(2));
        Assert.Equal("1rem", style.RowGap.ToCss());
        Assert.Equal("2rem", style.ColumnGap.ToCss());
    }

    [Fact]
    public void Can_set_transform_property() {
        var style = new CssStyleDeclaration();
        style.Transform = Transform.Rotate(Angle.Deg(45)).ThenScale(1.5);
        Assert.Equal("rotate(45deg) scale(1.5)", style.Transform.ToCss());
    }

    [Fact]
    public void Can_set_shadow_property() {
        var style = new CssStyleDeclaration();
        style.BoxShadow = Shadow.Box(Length.Px(0), Length.Px(2),
            blur: Length.Px(8), color: CssColor.Rgba(0, 0, 0, 0.1));
        Assert.Equal("0px 2px 8px rgba(0, 0, 0, 0.1)", style.BoxShadow.ToCss());
    }

    [Fact]
    public void Can_set_transition_property() {
        var style = new CssStyleDeclaration();
        style.Transition = Transition.For("opacity", Duration.Ms(300), Easing.EaseInOut);
        Assert.Equal("opacity 300ms ease-in-out", style.Transition.ToCss());
    }
}
