using BrowserApi.Common;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS transition value that defines how a property animates between states
/// (e.g., <c>opacity 0.3s ease-in-out</c>, <c>all 200ms</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Transition"/> provides factory methods to create CSS transition declarations
/// for specific properties (<see cref="For"/>) or for all properties (<see cref="All"/>).
/// Each transition specifies at minimum a property name and a duration, with optional
/// easing function and delay.
/// </para>
/// <para>
/// Multiple transitions can be combined into a comma-separated list using <see cref="Combine"/>,
/// which is the standard way to animate multiple properties with different timings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Transition a single property
/// var fade = Transition.For("opacity", Duration.S(0.3), Easing.EaseInOut);
/// fade.ToCss(); // "opacity 0.3s ease-in-out"
///
/// // Transition all properties
/// var all = Transition.All(Duration.Ms(200));
/// all.ToCss(); // "all 200ms"
///
/// // Transition with delay
/// var delayed = Transition.For("transform", Duration.S(0.5), Easing.Ease, Duration.Ms(100));
/// delayed.ToCss(); // "transform 0.5s ease 100ms"
///
/// // Combine multiple transitions
/// var multi = Transition.Combine(
///     Transition.For("opacity", Duration.S(0.3)),
///     Transition.For("transform", Duration.S(0.5)));
/// multi.ToCss(); // "opacity 0.3s, transform 0.5s"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Duration"/>
/// <seealso cref="Easing"/>
public readonly partial struct Transition : ICssValue, IEquatable<Transition> {
    private readonly string _value;

    /// <summary>
    /// Initializes a new <see cref="Transition"/> with a raw CSS transition string.
    /// </summary>
    /// <param name="value">The CSS transition string (e.g., <c>"opacity 0.3s ease"</c>).</param>
    public Transition(string value) => _value = value;

    /// <summary>
    /// Serializes this transition to its CSS string representation.
    /// </summary>
    /// <returns>The CSS transition string.</returns>
    public string ToCss() => _value;

    /// <inheritdoc />
    public override string ToString() => _value;

    // Sentinel

    /// <summary>Gets a <see cref="Transition"/> representing the CSS <c>none</c> keyword, which disables all transitions.</summary>
    public static Transition None { get; } = new("none");

    // Factories

    /// <summary>
    /// Creates a transition for a specific CSS property.
    /// </summary>
    /// <param name="property">The CSS property name to transition (e.g., <c>"opacity"</c>, <c>"transform"</c>, <c>"background-color"</c>).</param>
    /// <param name="duration">The duration of the transition.</param>
    /// <param name="timingFunction">
    /// The easing/timing function that controls the transition's rate of change.
    /// Defaults to <see langword="null"/>, which uses the browser default (<c>ease</c>).
    /// </param>
    /// <param name="delay">
    /// The delay before the transition starts. Defaults to <see langword="null"/> (no delay).
    /// </param>
    /// <returns>A <see cref="Transition"/> that serializes to a valid CSS transition shorthand value.</returns>
    /// <example>
    /// <code>
    /// Transition.For("opacity", Duration.S(0.3), Easing.EaseInOut).ToCss();
    /// // "opacity 0.3s ease-in-out"
    /// </code>
    /// </example>
    public static Transition For(string property, Duration duration,
        Easing? timingFunction = null, Duration? delay = null) {
        var parts = new List<string> { property, duration.ToCss() };
        if (timingFunction is not null) parts.Add(timingFunction.Value.ToCss());
        if (delay is not null) parts.Add(delay.Value.ToCss());
        return new(string.Join(' ', parts));
    }

    /// <summary>
    /// Creates a transition for all animatable CSS properties.
    /// </summary>
    /// <param name="duration">The duration of the transition.</param>
    /// <param name="timingFunction">
    /// The easing/timing function. Defaults to <see langword="null"/> (browser default).
    /// </param>
    /// <param name="delay">
    /// The delay before the transition starts. Defaults to <see langword="null"/> (no delay).
    /// </param>
    /// <returns>A <see cref="Transition"/> equivalent to calling <see cref="For"/> with <c>"all"</c> as the property.</returns>
    /// <example>
    /// <code>
    /// Transition.All(Duration.Ms(200), Easing.Linear).ToCss();
    /// // "all 200ms linear"
    /// </code>
    /// </example>
    public static Transition All(Duration duration,
        Easing? timingFunction = null, Duration? delay = null) =>
        For("all", duration, timingFunction, delay);

    // Combine multiple transitions

    /// <summary>
    /// Combines multiple transition values into a single comma-separated transition list.
    /// </summary>
    /// <param name="transitions">The transitions to combine.</param>
    /// <returns>A <see cref="Transition"/> that serializes to a comma-separated list of transitions.</returns>
    /// <remarks>
    /// The CSS <c>transition</c> property accepts comma-separated lists, allowing different
    /// properties to animate with independent durations, easing functions, and delays.
    /// </remarks>
    /// <example>
    /// <code>
    /// var combined = Transition.Combine(
    ///     Transition.For("opacity", Duration.S(0.3), Easing.EaseIn),
    ///     Transition.For("transform", Duration.S(0.5), Easing.EaseOut));
    /// combined.ToCss(); // "opacity 0.3s ease-in, transform 0.5s ease-out"
    /// </code>
    /// </example>
    public static Transition Combine(params ReadOnlySpan<Transition> transitions) {
        var parts = new string[transitions.Length];
        for (var i = 0; i < transitions.Length; i++)
            parts[i] = transitions[i].ToCss();
        return new(string.Join(", ", parts));
    }

    // Equality

    /// <inheritdoc />
    public bool Equals(Transition other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Transition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Transition"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Transition left, Transition right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Transition"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Transition left, Transition right) => !left.Equals(right);
}
