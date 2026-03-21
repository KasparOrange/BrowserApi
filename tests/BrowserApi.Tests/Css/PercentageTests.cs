using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class PercentageTests {
    [Theory]
    [InlineData(50, "50%")]
    [InlineData(100, "100%")]
    [InlineData(0, "0%")]
    [InlineData(33.333, "33.333%")]
    [InlineData(0.5, "0.5%")]
    public void Of_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Percentage.Of(value).ToCss());
    }

    [Fact]
    public void Zero_outputs_zero_percent() {
        Assert.Equal("0%", Percentage.Zero.ToCss());
    }

    [Fact]
    public void Calc_wraps_expression() {
        Assert.Equal("calc(50% + 10%)", Percentage.Calc("50% + 10%").ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("50%", Percentage.Of(50).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Percentage.Of(50), Percentage.Of(50));
        Assert.True(Percentage.Of(50) == Percentage.Of(50));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Percentage.Of(50) != Percentage.Of(75));
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Percentage.Of(50).GetHashCode(), Percentage.Of(50).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Percentage).ToCss());
    }
}
