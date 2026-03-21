using BrowserApi.Animations;

namespace BrowserApi.Tests.Animations;

public class KeyframeBuilderTests {
    [Fact]
    public void AddFrame_creates_dictionary_from_anonymous_object() {
        var result = new KeyframeBuilder()
            .AddFrame(new { opacity = 0 })
            .Build();

        var frames = Assert.IsType<Dictionary<string, object>[]>(result);
        Assert.Single(frames);
        Assert.Equal(0, frames[0]["opacity"]);
    }

    [Fact]
    public void Multiple_frames_accumulate() {
        var result = new KeyframeBuilder()
            .AddFrame(new { opacity = 0 })
            .AddFrame(new { opacity = 1 })
            .Build();

        var frames = Assert.IsType<Dictionary<string, object>[]>(result);
        Assert.Equal(2, frames.Length);
    }

    [Fact]
    public void AddFrame_with_offset() {
        var result = new KeyframeBuilder()
            .AddFrame(new { opacity = 0.5 }, 0.5)
            .Build();

        var frames = Assert.IsType<Dictionary<string, object>[]>(result);
        Assert.Equal(0.5, frames[0]["offset"]);
    }

    [Fact]
    public void AddFrame_with_easing() {
        var result = new KeyframeBuilder()
            .AddFrame(new { opacity = 1 }, "ease-in")
            .Build();

        var frames = Assert.IsType<Dictionary<string, object>[]>(result);
        Assert.Equal("ease-in", frames[0]["easing"]);
    }

    [Fact]
    public void AddFrame_with_offset_and_easing() {
        var result = new KeyframeBuilder()
            .AddFrame(new { opacity = 1 }, 0.75, "ease-out")
            .Build();

        var frames = Assert.IsType<Dictionary<string, object>[]>(result);
        Assert.Equal(0.75, frames[0]["offset"]);
        Assert.Equal("ease-out", frames[0]["easing"]);
    }

    [Fact]
    public void Multiple_properties_in_frame() {
        var result = new KeyframeBuilder()
            .AddFrame(new { opacity = 0, transform = "scale(0)" })
            .Build();

        var frames = Assert.IsType<Dictionary<string, object>[]>(result);
        Assert.Equal(0, frames[0]["opacity"]);
        Assert.Equal("scale(0)", frames[0]["transform"]);
    }

    [Fact]
    public void Method_chaining_returns_same_builder() {
        var builder = new KeyframeBuilder();
        var result = builder.AddFrame(new { opacity = 0 });
        Assert.Same(builder, result);
    }
}
