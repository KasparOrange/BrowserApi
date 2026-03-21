using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class FlexTests {
    [Theory]
    [InlineData(1, "1fr")]
    [InlineData(2, "2fr")]
    [InlineData(0.5, "0.5fr")]
    [InlineData(1.5, "1.5fr")]
    public void Fr_formats_correctly(double value, string expected) {
        Assert.Equal(expected, Flex.Fr(value).ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("1fr", Flex.Fr(1).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Flex.Fr(1), Flex.Fr(1));
        Assert.True(Flex.Fr(1) == Flex.Fr(1));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Flex.Fr(1) != Flex.Fr(2));
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Flex.Fr(1).GetHashCode(), Flex.Fr(1).GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Flex).ToCss());
    }
}
