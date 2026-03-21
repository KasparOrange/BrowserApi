using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class ResolutionTests {
    [Theory]
    [InlineData(96, "96dpi")]
    [InlineData(300, "300dpi")]
    [InlineData(72.5, "72.5dpi")]
    public void Dpi_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Resolution.Dpi(value).ToCss());
    }

    [Theory]
    [InlineData(38, "38dpcm")]
    [InlineData(118.11, "118.11dpcm")]
    public void Dpcm_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Resolution.Dpcm(value).ToCss());
    }

    [Theory]
    [InlineData(2, "2dppx")]
    [InlineData(1.5, "1.5dppx")]
    [InlineData(1, "1dppx")]
    public void Dppx_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Resolution.Dppx(value).ToCss());
    }

    [Fact]
    public void Calc_wraps_expression() {
        Assert.Equal("calc(96dpi * 2)", Resolution.Calc("96dpi * 2").ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("96dpi", Resolution.Dpi(96).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Resolution.Dpi(96), Resolution.Dpi(96));
        Assert.True(Resolution.Dpi(96) == Resolution.Dpi(96));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Resolution.Dpi(96) != Resolution.Dppx(1));
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Resolution.Dpi(96).GetHashCode(), Resolution.Dpi(96).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Resolution).ToCss());
    }
}
