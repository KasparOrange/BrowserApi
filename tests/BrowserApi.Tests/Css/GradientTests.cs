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

    // ── Multiple color stops ───────────────────────────────────────────

    [Fact]
    public void Linear_with_three_stops() {
        var result = Gradient.Linear(CssColor.Red, CssColor.White, CssColor.Blue);
        Assert.Equal("linear-gradient(red, white, blue)", result.ToCss());
    }

    [Fact]
    public void Radial_with_positioned_stops() {
        var result = Gradient.Radial("circle",
            GradientStop.At(CssColor.White, Percentage.Of(0)),
            GradientStop.At(CssColor.Black, Percentage.Of(100)));
        Assert.Equal("radial-gradient(circle, white 0%, black 100%)", result.ToCss());
    }

    [Fact]
    public void Conic_with_three_stops() {
        var result = Gradient.Conic(CssColor.Red, CssColor.Green, CssColor.Blue);
        Assert.Equal("conic-gradient(red, green, blue)", result.ToCss());
    }

    // ── Repeating variants with stops ──────────────────────────────────

    [Fact]
    public void RepeatingLinear_with_positioned_stops() {
        var result = Gradient.RepeatingLinear(Angle.Deg(45),
            GradientStop.At(CssColor.Red, Length.Px(0)),
            GradientStop.At(CssColor.Blue, Length.Px(20)));
        Assert.Equal("repeating-linear-gradient(45deg, red 0px, blue 20px)", result.ToCss());
    }

    [Fact]
    public void RepeatingRadial_with_positioned_stops() {
        var result = Gradient.RepeatingRadial("circle",
            GradientStop.At(CssColor.Red, Percentage.Of(0)),
            GradientStop.At(CssColor.Blue, Percentage.Of(10)));
        Assert.Equal("repeating-radial-gradient(circle, red 0%, blue 10%)", result.ToCss());
    }

    [Fact]
    public void RepeatingConic_with_positioned_stops() {
        var result = Gradient.RepeatingConic(Angle.Deg(0),
            GradientStop.At(CssColor.Red, Percentage.Of(0)),
            GradientStop.At(CssColor.Blue, Percentage.Of(25)));
        Assert.Equal("repeating-conic-gradient(from 0deg, red 0%, blue 25%)", result.ToCss());
    }

    // ── Angle variants ─────────────────────────────────────────────────

    [Fact]
    public void Linear_with_90deg_angle() {
        var result = Gradient.Linear(Angle.Deg(90), CssColor.Red, CssColor.Blue);
        Assert.Equal("linear-gradient(90deg, red, blue)", result.ToCss());
    }

    [Fact]
    public void Linear_with_180deg_angle() {
        var result = Gradient.Linear(Angle.Deg(180), CssColor.Red, CssColor.Blue);
        Assert.Equal("linear-gradient(180deg, red, blue)", result.ToCss());
    }

    // ── Equality operators ─────────────────────────────────────────────

    [Fact]
    public void Equality_operator_returns_true_for_same_values() {
        var a = Gradient.Linear(CssColor.Red, CssColor.Blue);
        var b = Gradient.Linear(CssColor.Red, CssColor.Blue);
        Assert.True(a == b);
    }

    [Fact]
    public void Inequality_operator_returns_true_for_different_values() {
        var a = Gradient.Linear(CssColor.Red, CssColor.Blue);
        var b = Gradient.Radial(CssColor.Red, CssColor.Blue);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_equal_for_same_values() {
        var a = Gradient.Linear(CssColor.Red, CssColor.Blue);
        var b = Gradient.Linear(CssColor.Red, CssColor.Blue);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_object_returns_false_for_wrong_type() {
        var g = Gradient.Linear(CssColor.Red, CssColor.Blue);
        Assert.False(g.Equals("not a gradient"));
    }
}
