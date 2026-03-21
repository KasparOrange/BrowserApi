using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class GradientTests {
    [Fact]
    public void Linear_with_colors() {
        var result = Gradient.Linear(CssColor.Red, CssColor.Blue);
        Assert.Equal("linear-gradient(red, blue)", result.ToCss());
    }

    [Fact]
    public void Linear_with_angle() {
        var result = Gradient.Linear(Angle.Deg(45), CssColor.Red, CssColor.Blue);
        Assert.Equal("linear-gradient(45deg, red, blue)", result.ToCss());
    }

    [Fact]
    public void Linear_with_positioned_stops() {
        var result = Gradient.Linear(
            GradientStop.At(CssColor.Red, Percentage.Of(0)),
            GradientStop.At(CssColor.Blue, Percentage.Of(100)));
        Assert.Equal("linear-gradient(red 0%, blue 100%)", result.ToCss());
    }

    [Fact]
    public void Linear_with_length_positioned_stop() {
        var result = Gradient.Linear(
            GradientStop.At(CssColor.Red, Length.Px(0)),
            GradientStop.At(CssColor.Blue, Length.Px(200)));
        Assert.Equal("linear-gradient(red 0px, blue 200px)", result.ToCss());
    }

    [Fact]
    public void Radial_with_colors() {
        var result = Gradient.Radial(CssColor.Red, CssColor.Blue);
        Assert.Equal("radial-gradient(red, blue)", result.ToCss());
    }

    [Fact]
    public void Radial_with_shape() {
        var result = Gradient.Radial("circle", CssColor.Red, CssColor.Blue);
        Assert.Equal("radial-gradient(circle, red, blue)", result.ToCss());
    }

    [Fact]
    public void Conic_with_colors() {
        var result = Gradient.Conic(CssColor.Red, CssColor.Blue);
        Assert.Equal("conic-gradient(red, blue)", result.ToCss());
    }

    [Fact]
    public void Conic_with_from_angle() {
        var result = Gradient.Conic(Angle.Deg(45), CssColor.Red, CssColor.Blue);
        Assert.Equal("conic-gradient(from 45deg, red, blue)", result.ToCss());
    }

    [Fact]
    public void RepeatingLinear_formats_correctly() {
        var result = Gradient.RepeatingLinear(Angle.Deg(45), CssColor.Red, CssColor.Blue);
        Assert.Equal("repeating-linear-gradient(45deg, red, blue)", result.ToCss());
    }

    [Fact]
    public void RepeatingRadial_formats_correctly() {
        var result = Gradient.RepeatingRadial("circle", CssColor.Red, CssColor.Blue);
        Assert.Equal("repeating-radial-gradient(circle, red, blue)", result.ToCss());
    }

    [Fact]
    public void RepeatingConic_formats_correctly() {
        var result = Gradient.RepeatingConic(Angle.Deg(45), CssColor.Red, CssColor.Blue);
        Assert.Equal("repeating-conic-gradient(from 45deg, red, blue)", result.ToCss());
    }

    [Fact]
    public void Implicit_CssColor_to_GradientStop_conversion() {
        GradientStop stop = CssColor.Red;
        Assert.Equal("red", stop.ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        var result = Gradient.Linear(CssColor.Red, CssColor.Blue);
        Assert.Equal("linear-gradient(red, blue)", result.ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(
            Gradient.Linear(CssColor.Red, CssColor.Blue),
            Gradient.Linear(CssColor.Red, CssColor.Blue));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.NotEqual(
            Gradient.Linear(CssColor.Red, CssColor.Blue),
            Gradient.Radial(CssColor.Red, CssColor.Blue));
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Gradient).ToCss());
    }
}
