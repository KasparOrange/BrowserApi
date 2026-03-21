using BrowserApi.Animations;

namespace BrowserApi.Tests.Animations;

public class AnimationOptionsBuilderTests {
    [Fact]
    public void Duration_sets_duration() {
        var options = new AnimationOptionsBuilder().Duration(500).Build();
        Assert.Equal(500.0, options.Duration);
    }

    [Fact]
    public void Delay_sets_delay() {
        var options = new AnimationOptionsBuilder().Delay(100).Build();
        Assert.Equal(100.0, options.Delay);
    }

    [Fact]
    public void EndDelay_sets_end_delay() {
        var options = new AnimationOptionsBuilder().EndDelay(200).Build();
        Assert.Equal(200.0, options.EndDelay);
    }

    [Fact]
    public void Easing_sets_easing() {
        var options = new AnimationOptionsBuilder().Easing(Easing.EaseInOut).Build();
        Assert.Equal("ease-in-out", options.Easing);
    }

    [Fact]
    public void Fill_sets_fill_mode() {
        var options = new AnimationOptionsBuilder().Fill(FillMode.Forwards).Build();
        Assert.Equal(FillMode.Forwards, options.Fill);
    }

    [Fact]
    public void Direction_sets_direction() {
        var options = new AnimationOptionsBuilder().Direction(PlaybackDirection.Alternate).Build();
        Assert.Equal(PlaybackDirection.Alternate, options.Direction);
    }

    [Fact]
    public void Iterations_sets_iterations() {
        var options = new AnimationOptionsBuilder().Iterations(double.PositiveInfinity).Build();
        Assert.Equal(double.PositiveInfinity, options.Iterations);
    }

    [Fact]
    public void Id_sets_id() {
        var options = new AnimationOptionsBuilder().Id("fade").Build();
        Assert.Equal("fade", options.Id);
    }

    [Fact]
    public void Composite_sets_composite() {
        var options = new AnimationOptionsBuilder().Composite(CompositeOperation.Add).Build();
        Assert.Equal(CompositeOperation.Add, options.Composite);
    }

    [Fact]
    public void Full_chain_builds_complete_options() {
        var options = new AnimationOptionsBuilder()
            .Duration(500)
            .Delay(100)
            .Easing(Easing.EaseInOut)
            .Fill(FillMode.Forwards)
            .Direction(PlaybackDirection.Alternate)
            .Iterations(3)
            .Id("bounce")
            .Build();

        Assert.Equal(500.0, options.Duration);
        Assert.Equal(100.0, options.Delay);
        Assert.Equal("ease-in-out", options.Easing);
        Assert.Equal(FillMode.Forwards, options.Fill);
        Assert.Equal(PlaybackDirection.Alternate, options.Direction);
        Assert.Equal(3.0, options.Iterations);
        Assert.Equal("bounce", options.Id);
    }

    [Fact]
    public void Method_chaining_returns_same_builder() {
        var builder = new AnimationOptionsBuilder();
        Assert.Same(builder, builder.Duration(100));
        Assert.Same(builder, builder.Easing("linear"));
    }

    [Fact]
    public void Implicit_conversion_to_KeyframeAnimationOptions() {
        KeyframeAnimationOptions options = new AnimationOptionsBuilder().Duration(300);
        Assert.Equal(300.0, options.Duration);
    }
}
