namespace BrowserApi.Animations;

/// <summary>
/// A fluent builder for constructing <see cref="KeyframeAnimationOptions"/> used with the
/// Web Animations API.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides a clean, chainable API for specifying animation timing properties
/// such as duration, delay, easing, fill mode, iteration count, and direction. Call
/// <see cref="Build"/> to produce the final <see cref="KeyframeAnimationOptions"/>, or rely
/// on the implicit conversion operator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Build options with method chaining
/// var options = new AnimationOptionsBuilder()
///     .Duration(500)
///     .Easing(Animations.Easing.EaseInOut)
///     .Fill(FillMode.Forwards)
///     .Iterations(3)
///     .Direction(PlaybackDirection.Alternate)
///     .Build();
///
/// element.Animate(keyframes, options);
///
/// // Implicit conversion -- no need to call Build()
/// KeyframeAnimationOptions opts = new AnimationOptionsBuilder()
///     .Duration(200)
///     .Easing(Animations.Easing.EaseOut);
/// </code>
/// </example>
/// <seealso cref="KeyframeAnimationOptions"/>
/// <seealso cref="KeyframeBuilder"/>
/// <seealso cref="AnimateExtensions"/>
/// <seealso cref="Easing"/>
public sealed class AnimationOptionsBuilder {
    private object? _duration;
    private double? _delay;
    private double? _endDelay;
    private string? _easing;
    private FillMode? _fill;
    private PlaybackDirection? _direction;
    private double? _iterations;
    private double? _iterationStart;
    private string? _id;
    private CompositeOperation? _composite;

    /// <summary>
    /// Sets the animation duration in milliseconds.
    /// </summary>
    /// <param name="ms">The duration in milliseconds.</param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Duration(double ms) {
        _duration = ms;
        return this;
    }

    /// <summary>
    /// Sets the delay before the animation starts, in milliseconds.
    /// </summary>
    /// <param name="ms">The delay in milliseconds. Negative values cause the animation to start partway through.</param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Delay(double ms) {
        _delay = ms;
        return this;
    }

    /// <summary>
    /// Sets the delay after the animation ends before the <c>finish</c> event fires, in milliseconds.
    /// </summary>
    /// <param name="ms">The end delay in milliseconds.</param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder EndDelay(double ms) {
        _endDelay = ms;
        return this;
    }

    /// <summary>
    /// Sets the easing function for the animation.
    /// </summary>
    /// <param name="easing">
    /// A CSS easing string. Use constants from <see cref="Animations.Easing"/> (e.g.,
    /// <see cref="Animations.Easing.EaseInOut"/>) or a custom <c>cubic-bezier(...)</c> value.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Easing(string easing) {
        _easing = easing;
        return this;
    }

    /// <summary>
    /// Sets the fill mode, which determines how the element is styled before the animation starts
    /// and after it ends.
    /// </summary>
    /// <param name="fill">
    /// The fill mode (<c>None</c>, <c>Forwards</c>, <c>Backwards</c>, or <c>Both</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Fill(FillMode fill) {
        _fill = fill;
        return this;
    }

    /// <summary>
    /// Sets the playback direction of the animation.
    /// </summary>
    /// <param name="direction">
    /// The playback direction (<c>Normal</c>, <c>Reverse</c>, <c>Alternate</c>, or <c>AlternateReverse</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Direction(PlaybackDirection direction) {
        _direction = direction;
        return this;
    }

    /// <summary>
    /// Sets the number of times the animation should repeat.
    /// </summary>
    /// <param name="count">
    /// The iteration count. Use <see cref="double.PositiveInfinity"/> for infinite looping.
    /// Fractional values (e.g., <c>2.5</c>) cause the animation to end partway through an iteration.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Iterations(double count) {
        _iterations = count;
        return this;
    }

    /// <summary>
    /// Sets the point in the iteration at which the animation should start.
    /// </summary>
    /// <param name="start">
    /// A value from <c>0.0</c> to the total iteration count. For example, <c>0.5</c> starts
    /// the animation halfway through the first iteration.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder IterationStart(double start) {
        _iterationStart = start;
        return this;
    }

    /// <summary>
    /// Sets a developer-readable identifier for the animation.
    /// </summary>
    /// <param name="id">The animation ID string.</param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Id(string id) {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the composite operation that determines how animated values are combined with
    /// existing property values.
    /// </summary>
    /// <param name="composite">
    /// The composite operation (<c>Replace</c>, <c>Add</c>, or <c>Accumulate</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public AnimationOptionsBuilder Composite(CompositeOperation composite) {
        _composite = composite;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="KeyframeAnimationOptions"/> from the configured values.
    /// </summary>
    /// <returns>A new <see cref="KeyframeAnimationOptions"/> instance with all configured properties.</returns>
    public KeyframeAnimationOptions Build() => new() {
        Duration = _duration,
        Delay = _delay,
        EndDelay = _endDelay,
        Easing = _easing,
        Fill = _fill,
        Direction = _direction,
        Iterations = _iterations,
        IterationStart = _iterationStart,
        Id = _id,
        Composite = _composite
    };

    /// <summary>
    /// Implicitly converts an <see cref="AnimationOptionsBuilder"/> to
    /// <see cref="KeyframeAnimationOptions"/>, allowing the builder to be used directly
    /// wherever animation options are expected.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The built <see cref="KeyframeAnimationOptions"/>.</returns>
    public static implicit operator KeyframeAnimationOptions(AnimationOptionsBuilder builder) =>
        builder.Build();
}
