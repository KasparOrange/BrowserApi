using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class AngleTests {
    [Theory]
    [InlineData(45, "45deg")]
    [InlineData(0, "0deg")]
    [InlineData(360, "360deg")]
    [InlineData(22.5, "22.5deg")]
    [InlineData(-90, "-90deg")]
    public void Deg_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Angle.Deg(value).ToCss());
    }

    [Theory]
    [InlineData(3.1416, "3.1416rad")]
    [InlineData(0, "0rad")]
    [InlineData(1.5708, "1.5708rad")]
    public void Rad_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Angle.Rad(value).ToCss());
    }

    [Theory]
    [InlineData(100, "100grad")]
    [InlineData(50.5, "50.5grad")]
    public void Grad_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Angle.Grad(value).ToCss());
    }

    [Theory]
    [InlineData(0.5, "0.5turn")]
    [InlineData(1, "1turn")]
    [InlineData(0.25, "0.25turn")]
    public void Turn_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Angle.Turn(value).ToCss());
    }

    [Fact]
    public void Zero_outputs_zero_degrees() {
        Assert.Equal("0deg", Angle.Zero.ToCss());
    }

    [Fact]
    public void Calc_wraps_expression() {
        Assert.Equal("calc(90deg + 45deg)", Angle.Calc("90deg + 45deg").ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("45deg", Angle.Deg(45).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Angle.Deg(45), Angle.Deg(45));
        Assert.True(Angle.Deg(45) == Angle.Deg(45));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Angle.Deg(45) != Angle.Rad(0.785));
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Angle.Deg(45).GetHashCode(), Angle.Deg(45).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Angle).ToCss());
    }
}
