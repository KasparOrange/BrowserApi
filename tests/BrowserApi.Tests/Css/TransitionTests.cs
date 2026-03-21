using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class TransitionTests {
    [Fact]
    public void None_outputs_none() {
        Assert.Equal("none", Transition.None.ToCss());
    }

    [Fact]
    public void For_with_property_and_duration() {
        var result = Transition.For("opacity", Duration.Ms(300));
        Assert.Equal("opacity 300ms", result.ToCss());
    }

    [Fact]
    public void For_with_timing_function() {
        var result = Transition.For("opacity", Duration.Ms(300), Easing.EaseInOut);
        Assert.Equal("opacity 300ms ease-in-out", result.ToCss());
    }

    [Fact]
    public void For_with_timing_function_and_delay() {
        var result = Transition.For("opacity", Duration.Ms(300), Easing.EaseInOut, Duration.Ms(100));
        Assert.Equal("opacity 300ms ease-in-out 100ms", result.ToCss());
    }

    [Fact]
    public void All_with_duration_and_timing() {
        var result = Transition.All(Duration.Ms(300), Easing.Ease);
        Assert.Equal("all 300ms ease", result.ToCss());
    }

    [Fact]
    public void Combine_multiple_transitions() {
        var result = Transition.Combine(
            Transition.For("opacity", Duration.Ms(300), Easing.EaseInOut),
            Transition.For("transform", Duration.Ms(500), Easing.Ease));
        Assert.Equal("opacity 300ms ease-in-out, transform 500ms ease", result.ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("none", Transition.None.ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Transition.None, Transition.None);
        Assert.True(Transition.For("opacity", Duration.Ms(300)) == Transition.For("opacity", Duration.Ms(300)));
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Transition.For("opacity", Duration.Ms(300)) != Transition.None);
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Transition).ToCss());
    }
}
