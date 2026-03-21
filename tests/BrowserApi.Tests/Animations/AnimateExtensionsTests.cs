using BrowserApi.Animations;
using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Animations;

[Collection("JsObject")]
public class AnimateExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly Element _element;

    public AnimateExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _element = new Element { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void FadeIn_calls_animate() {
        _element.FadeIn(500);

        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "animate");
    }

    [Fact]
    public void FadeOut_calls_animate() {
        _element.FadeOut(300);

        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "animate");
    }

    [Fact]
    public void SlideIn_calls_animate() {
        _element.SlideIn(400, "right");

        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "animate");
    }

    [Fact]
    public void Animate_with_builders_calls_animate() {
        _element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { opacity = 0 })
                .AddFrame(new { opacity = 1 }),
            new AnimationOptionsBuilder()
                .Duration(500)
                .Easing(Easing.EaseInOut));

        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "animate");
    }

    [Fact]
    public void Animate_with_duration_shorthand() {
        _element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { opacity = 0 })
                .AddFrame(new { opacity = 1 }),
            200);

        Assert.Contains(_mock.Calls, c => c.Method == "Invoke" && c.Name == "animate");
    }
}
