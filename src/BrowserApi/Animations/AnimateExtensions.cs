using BrowserApi.Dom;

namespace BrowserApi.Animations;

/// <summary>
/// Provides convenience extension methods on <see cref="Element"/> for common Web Animations API
/// operations such as fade and slide transitions.
/// </summary>
/// <remarks>
/// <para>
/// These extensions wrap <see cref="Element.Animate(object, object)"/> with builder-based overloads
/// and pre-built animation presets. For full control over keyframes and timing, use
/// <see cref="KeyframeBuilder"/> and <see cref="AnimationOptionsBuilder"/> directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Pre-built fade animations
/// element.FadeIn(durationMs: 500);
/// element.FadeOut(durationMs: 300);
///
/// // Slide in from the right
/// element.SlideIn(durationMs: 400, direction: "right");
///
/// // Custom animation using builders
/// element.Animate(
///     new KeyframeBuilder()
///         .AddFrame(new { transform = "rotate(0deg)" })
///         .AddFrame(new { transform = "rotate(360deg)" }),
///     new AnimationOptionsBuilder()
///         .Duration(1000)
///         .Iterations(double.PositiveInfinity));
/// </code>
/// </example>
/// <seealso cref="KeyframeBuilder"/>
/// <seealso cref="AnimationOptionsBuilder"/>
/// <seealso cref="Easing"/>
public static class AnimateExtensions {
    /// <summary>
    /// Starts an animation on the element using a <see cref="KeyframeBuilder"/> and an
    /// <see cref="AnimationOptionsBuilder"/>.
    /// </summary>
    /// <param name="element">The element to animate.</param>
    /// <param name="keyframes">The keyframe builder defining the animation states.</param>
    /// <param name="options">The animation options builder defining timing and behavior.</param>
    /// <returns>An <see cref="Animation"/> object representing the running animation.</returns>
    public static Animation Animate(this Element element, KeyframeBuilder keyframes, AnimationOptionsBuilder options) =>
        element.Animate(keyframes.Build(), options.Build());

    /// <summary>
    /// Starts an animation on the element using a <see cref="KeyframeBuilder"/> and a simple
    /// duration in milliseconds.
    /// </summary>
    /// <param name="element">The element to animate.</param>
    /// <param name="keyframes">The keyframe builder defining the animation states.</param>
    /// <param name="durationMs">The animation duration in milliseconds.</param>
    /// <returns>An <see cref="Animation"/> object representing the running animation.</returns>
    public static Animation Animate(this Element element, KeyframeBuilder keyframes, double durationMs) =>
        element.Animate(keyframes.Build(), durationMs);

    /// <summary>
    /// Fades the element in from fully transparent (<c>opacity: 0</c>) to fully opaque
    /// (<c>opacity: 1</c>).
    /// </summary>
    /// <param name="element">The element to fade in.</param>
    /// <param name="durationMs">The animation duration in milliseconds. Defaults to 300.</param>
    /// <returns>An <see cref="Animation"/> object representing the running fade-in animation.</returns>
    /// <remarks>
    /// The animation uses <see cref="FillMode.Forwards"/> so the element remains visible after
    /// the animation completes.
    /// </remarks>
    public static Animation FadeIn(this Element element, double durationMs = 300) =>
        element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { opacity = 0 })
                .AddFrame(new { opacity = 1 }),
            new AnimationOptionsBuilder()
                .Duration(durationMs)
                .Fill(FillMode.Forwards));

    /// <summary>
    /// Fades the element out from fully opaque (<c>opacity: 1</c>) to fully transparent
    /// (<c>opacity: 0</c>).
    /// </summary>
    /// <param name="element">The element to fade out.</param>
    /// <param name="durationMs">The animation duration in milliseconds. Defaults to 300.</param>
    /// <returns>An <see cref="Animation"/> object representing the running fade-out animation.</returns>
    /// <remarks>
    /// The animation uses <see cref="FillMode.Forwards"/> so the element remains hidden after
    /// the animation completes.
    /// </remarks>
    public static Animation FadeOut(this Element element, double durationMs = 300) =>
        element.Animate(
            new KeyframeBuilder()
                .AddFrame(new { opacity = 1 })
                .AddFrame(new { opacity = 0 }),
            new AnimationOptionsBuilder()
                .Duration(durationMs)
                .Fill(FillMode.Forwards));

    /// <summary>
    /// Slides the element into view from the specified direction with a simultaneous fade-in effect.
    /// </summary>
    /// <param name="element">The element to slide in.</param>
    /// <param name="durationMs">The animation duration in milliseconds. Defaults to 300.</param>
    /// <param name="direction">
    /// The direction to slide from: <c>"left"</c>, <c>"right"</c>, <c>"top"</c>, or <c>"bottom"</c>.
    /// Defaults to <c>"left"</c>.
    /// </param>
    /// <returns>An <see cref="Animation"/> object representing the running slide-in animation.</returns>
    /// <remarks>
    /// The animation uses <see cref="Easing.EaseOut"/> easing and <see cref="FillMode.Forwards"/>
    /// fill mode. Unrecognized direction values fall back to sliding from the left.
    /// </remarks>
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
