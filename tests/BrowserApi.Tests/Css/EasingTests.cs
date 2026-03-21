using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class EasingTests {
    [Theory]
    [InlineData("ease")]
    [InlineData("linear")]
    [InlineData("ease-in")]
    [InlineData("ease-out")]
    [InlineData("ease-in-out")]
    [InlineData("step-start")]
    [InlineData("step-end")]
    public void Named_keywords_output_correctly(string expected) {
        var easing = expected switch {
            "ease" => Easing.Ease,
            "linear" => Easing.Linear,
            "ease-in" => Easing.EaseIn,
            "ease-out" => Easing.EaseOut,
            "ease-in-out" => Easing.EaseInOut,
            "step-start" => Easing.StepStart,
            "step-end" => Easing.StepEnd,
            _ => throw new ArgumentException()
        };
        Assert.Equal(expected, easing.ToCss());
    }

    [Fact]
    public void CubicBezier_formats_correctly() {
        Assert.Equal("cubic-bezier(0.42, 0, 0.58, 1)", Easing.CubicBezier(0.42, 0, 0.58, 1).ToCss());
    }

    [Fact]
    public void Steps_without_jump_term() {
        Assert.Equal("steps(4)", Easing.Steps(4).ToCss());
    }

    [Fact]
    public void Steps_with_jump_term() {
        Assert.Equal("steps(4, jump-end)", Easing.Steps(4, "jump-end").ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("ease", Easing.Ease.ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Easing.Ease, Easing.Ease);
        Assert.True(Easing.Ease == Easing.Ease);
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Easing.Ease != Easing.Linear);
    }

    [Fact]
    public void Equal_values_have_same_hash_code() {
        Assert.Equal(Easing.Ease.GetHashCode(), Easing.Ease.GetHashCode());
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Easing).ToCss());
    }
}
