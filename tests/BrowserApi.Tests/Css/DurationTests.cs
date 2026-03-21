using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class DurationTests {
    [Theory]
    [InlineData(0.3, "0.3s")]
    [InlineData(1, "1s")]
    [InlineData(0, "0s")]
    [InlineData(2.5, "2.5s")]
    public void S_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Duration.S(value).ToCss());
    }

    [Theory]
    [InlineData(300, "300ms")]
    [InlineData(100.5, "100.5ms")]
    [InlineData(0, "0ms")]
    public void Ms_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Duration.Ms(value).ToCss());
    }

    [Fact]
    public void Zero_outputs_zero_seconds() {
        Assert.Equal("0s", Duration.Zero.ToCss());
    }

    [Fact]
    public void Calc_wraps_expression() {
        Assert.Equal("calc(100ms + 200ms)", Duration.Calc("100ms + 200ms").ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("300ms", Duration.Ms(300).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Duration.Ms(300), Duration.Ms(300));
        Assert.True(Duration.Ms(300) == Duration.Ms(300));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Duration.Ms(300) != Duration.S(0.3));
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Duration.Ms(300).GetHashCode(), Duration.Ms(300).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Duration).ToCss());
    }
}
