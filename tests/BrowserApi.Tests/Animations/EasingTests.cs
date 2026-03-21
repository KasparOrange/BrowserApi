using BrowserApi.Animations;

namespace BrowserApi.Tests.Animations;

public class EasingTests {
    [Fact]
    public void Linear() => Assert.Equal("linear", Easing.Linear);

    [Fact]
    public void Ease() => Assert.Equal("ease", Easing.Ease);

    [Fact]
    public void EaseIn() => Assert.Equal("ease-in", Easing.EaseIn);

    [Fact]
    public void EaseOut() => Assert.Equal("ease-out", Easing.EaseOut);

    [Fact]
    public void EaseInOut() => Assert.Equal("ease-in-out", Easing.EaseInOut);

    [Fact]
    public void CubicBezier_formats_correctly() {
        Assert.Equal("cubic-bezier(0.25, 0.1, 0.25, 1)", Easing.CubicBezier(0.25, 0.1, 0.25, 1));
    }

    [Fact]
    public void Steps_without_jump_term() {
        Assert.Equal("steps(4)", Easing.Steps(4));
    }

    [Fact]
    public void Steps_with_jump_term() {
        Assert.Equal("steps(4, jump-end)", Easing.Steps(4, "jump-end"));
    }

    [Fact]
    public void Common_curves_are_cubic_bezier() {
        Assert.StartsWith("cubic-bezier(", Easing.EaseInSine);
        Assert.StartsWith("cubic-bezier(", Easing.EaseOutQuad);
        Assert.StartsWith("cubic-bezier(", Easing.EaseInOutCubic);
    }
}
