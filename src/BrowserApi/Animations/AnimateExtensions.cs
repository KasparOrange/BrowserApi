using BrowserApi.Dom;

namespace BrowserApi.Animations;

public static class AnimateExtensions {
    public static Animation Animate(this Element element, KeyframeBuilder keyframes, AnimationOptionsBuilder options) =>
        element.Animate(keyframes.Build(), options.Build());

    public static Animation Animate(this Element element, KeyframeBuilder keyframes, double durationMs) =>
        element.Animate(keyframes.Build(), durationMs);

    public static Animation FadeIn(this Element element, double durationMs = 300) =>
        element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { opacity = 0 })
                .AddFrame(new { opacity = 1 }),
            new AnimationOptionsBuilder()
                .Duration(durationMs)
                .Fill(FillMode.Forwards));

    public static Animation FadeOut(this Element element, double durationMs = 300) =>
        element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { opacity = 1 })
                .AddFrame(new { opacity = 0 }),
            new AnimationOptionsBuilder()
                .Duration(durationMs)
                .Fill(FillMode.Forwards));

    public static Animation SlideIn(this Element element, double durationMs = 300, string direction = "left") {
        var from = direction switch {
            "left" => "translateX(-100%)",
            "right" => "translateX(100%)",
            "top" => "translateY(-100%)",
            "bottom" => "translateY(100%)",
            _ => "translateX(-100%)"
        };

        return element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { transform = from, opacity = 0 })
                .AddFrame(new { transform = "translate(0)", opacity = 1 }),
            new AnimationOptionsBuilder()
                .Duration(durationMs)
                .Easing(Easing.EaseOut)
                .Fill(FillMode.Forwards));
    }
}
