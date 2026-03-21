using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class LengthTests {
    [Theory]
    [InlineData(16, "16px")]
    [InlineData(1.5, "1.5px")]
    [InlineData(0, "0px")]
    [InlineData(-10, "-10px")]
    public void Px_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Length.Px(value).ToCss());
    }

    [Theory]
    [InlineData(2, "2em")]
    [InlineData(1.5, "1.5em")]
    public void Em_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Length.Em(value).ToCss());
    }

    [Theory]
    [InlineData(1, "1rem")]
    [InlineData(1.5, "1.5rem")]
    [InlineData(0.5, "0.5rem")]
    public void Rem_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Length.Rem(value).ToCss());
    }

    [Theory]
    [InlineData(100, "100vh")]
    [InlineData(50.5, "50.5vh")]
    public void Vh_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Length.Vh(value).ToCss());
    }

    [Theory]
    [InlineData(50, "50vw")]
    [InlineData(33.333, "33.333vw")]
    public void Vw_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Length.Vw(value).ToCss());
    }

    [Theory]
    [InlineData(50, "50%")]
    [InlineData(100, "100%")]
    [InlineData(33.333, "33.333%")]
    public void Percent_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Length.Percent(value).ToCss());
    }

    [Theory]
    [InlineData("100% - 2rem", "calc(100% - 2rem)")]
    [InlineData("50vw + 20px", "calc(50vw + 20px)")]
    public void Calc_wraps_expression(string expression, string expected) {
        Assert.Equal(expected, Length.Calc(expression).ToCss());
    }

    [Fact]
    public void Zero_outputs_unitless_zero() {
        Assert.Equal("0", Length.Zero.ToCss());
    }

    [Fact]
    public void Auto_outputs_auto() {
        Assert.Equal("auto", Length.Auto.ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("1.5rem", Length.Rem(1.5).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Length.Px(16), Length.Px(16));
        Assert.True(Length.Px(16) == Length.Px(16));
        Assert.True(Length.Px(16).Equals(Length.Px(16)));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.NotEqual(Length.Px(16), Length.Em(16));
        Assert.True(Length.Px(16) != Length.Em(16));
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Length.Px(16).GetHashCode(), Length.Px(16).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Length).ToCss());
    }
}
