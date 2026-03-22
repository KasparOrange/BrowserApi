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

    // ── Sine curves ────────────────────────────────────────────────────

    [Fact]
    public void EaseInSine() =>
        Assert.Equal("cubic-bezier(0.12, 0, 0.39, 0)", Easing.EaseInSine);

    [Fact]
    public void EaseOutSine() =>
        Assert.Equal("cubic-bezier(0.61, 1, 0.88, 1)", Easing.EaseOutSine);

    [Fact]
    public void EaseInOutSine() =>
        Assert.Equal("cubic-bezier(0.37, 0, 0.63, 1)", Easing.EaseInOutSine);

    // ── Quad curves ────────────────────────────────────────────────────

    [Fact]
    public void EaseInQuad() =>
        Assert.Equal("cubic-bezier(0.11, 0, 0.5, 0)", Easing.EaseInQuad);

    [Fact]
    public void EaseOutQuad() =>
        Assert.Equal("cubic-bezier(0.5, 1, 0.89, 1)", Easing.EaseOutQuad);

    [Fact]
    public void EaseInOutQuad() =>
        Assert.Equal("cubic-bezier(0.45, 0, 0.55, 1)", Easing.EaseInOutQuad);

    // ── Cubic curves ───────────────────────────────────────────────────

    [Fact]
    public void EaseInCubic() =>
        Assert.Equal("cubic-bezier(0.32, 0, 0.67, 0)", Easing.EaseInCubic);

    [Fact]
    public void EaseOutCubic() =>
        Assert.Equal("cubic-bezier(0.33, 1, 0.68, 1)", Easing.EaseOutCubic);

    [Fact]
    public void EaseInOutCubic() =>
        Assert.Equal("cubic-bezier(0.65, 0, 0.35, 1)", Easing.EaseInOutCubic);

    // ── CubicBezier with decimal values ────────────────────────────────

    [Fact]
    public void CubicBezier_with_negative_values() =>
        Assert.Equal("cubic-bezier(0.68, -0.55, 0.27, 1.55)", Easing.CubicBezier(0.68, -0.55, 0.27, 1.55));

    [Fact]
    public void CubicBezier_with_zero_values() =>
        Assert.Equal("cubic-bezier(0, 0, 1, 1)", Easing.CubicBezier(0, 0, 1, 1));

    [Fact]
    public void All_sine_curves_start_with_cubic_bezier() {
        Assert.StartsWith("cubic-bezier(", Easing.EaseInSine);
        Assert.StartsWith("cubic-bezier(", Easing.EaseOutSine);
        Assert.StartsWith("cubic-bezier(", Easing.EaseInOutSine);
    }

    [Fact]
    public void All_quad_curves_start_with_cubic_bezier() {
        Assert.StartsWith("cubic-bezier(", Easing.EaseInQuad);
        Assert.StartsWith("cubic-bezier(", Easing.EaseOutQuad);
        Assert.StartsWith("cubic-bezier(", Easing.EaseInOutQuad);
    }

    [Fact]
    public void All_cubic_curves_start_with_cubic_bezier() {
        Assert.StartsWith("cubic-bezier(", Easing.EaseInCubic);
        Assert.StartsWith("cubic-bezier(", Easing.EaseOutCubic);
        Assert.StartsWith("cubic-bezier(", Easing.EaseInOutCubic);
    }
}
