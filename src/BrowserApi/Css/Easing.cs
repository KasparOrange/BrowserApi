using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS easing function (timing function) used to control the rate of change
/// in transitions and animations (e.g., <c>ease</c>, <c>linear</c>, <c>cubic-bezier(0.4, 0, 0.2, 1)</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Easing"/> provides both named keyword constants (such as <see cref="Ease"/>,
/// <see cref="Linear"/>, <see cref="EaseInOut"/>) and parametric factory methods
/// (<see cref="CubicBezier"/> and <see cref="Steps"/>) for creating custom timing functions.
/// </para>
/// <para>
/// Unlike other CSS value types in this library, <see cref="Easing"/> is fully self-contained
/// (not a partial struct extended from a generated counterpart). It stores its own backing
/// <c>_value</c> field, implements <see cref="ICssValue"/>, and provides its own constructor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Named easing keywords
/// Easing timing = Easing.EaseInOut;
/// timing.ToCss(); // "ease-in-out"
///
/// // Custom cubic bezier (Material Design standard curve)
/// Easing material = Easing.CubicBezier(0.4, 0, 0.2, 1);
/// material.ToCss(); // "cubic-bezier(0.4, 0, 0.2, 1)"
///
/// // Step function
/// Easing steps = Easing.Steps(4, "jump-end");
/// steps.ToCss(); // "steps(4, jump-end)"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Transition"/>
/// <seealso cref="Duration"/>
public readonly partial struct Easing : ICssValue, IEquatable<Easing> {
    private readonly string _value;

    /// <summary>
    /// Initializes a new <see cref="Easing"/> with a raw CSS easing function string.
    /// </summary>
    /// <param name="value">The CSS easing function string (e.g., <c>"ease-in-out"</c>).</param>
    public Easing(string value) => _value = value;

    /// <summary>
    /// Serializes this easing function to its CSS string representation.
    /// </summary>
    /// <returns>The CSS easing function string.</returns>
    public string ToCss() => _value;

    /// <inheritdoc />
    public override string ToString() => _value;

    // Named keywords

    /// <summary>Gets the CSS <c>ease</c> timing function (default easing; equivalent to <c>cubic-bezier(0.25, 0.1, 0.25, 1)</c>).</summary>
    public static Easing Ease { get; } = new("ease");

    /// <summary>Gets the CSS <c>linear</c> timing function (constant speed from start to finish).</summary>
    public static Easing Linear { get; } = new("linear");

    /// <summary>Gets the CSS <c>ease-in</c> timing function (starts slowly, accelerates toward the end).</summary>
    public static Easing EaseIn { get; } = new("ease-in");

    /// <summary>Gets the CSS <c>ease-out</c> timing function (starts quickly, decelerates toward the end).</summary>
    public static Easing EaseOut { get; } = new("ease-out");

    /// <summary>Gets the CSS <c>ease-in-out</c> timing function (starts and ends slowly, faster in the middle).</summary>
    public static Easing EaseInOut { get; } = new("ease-in-out");

    /// <summary>Gets the CSS <c>step-start</c> timing function (equivalent to <c>steps(1, jump-start)</c>).</summary>
    public static Easing StepStart { get; } = new("step-start");

    /// <summary>Gets the CSS <c>step-end</c> timing function (equivalent to <c>steps(1, jump-end)</c>).</summary>
    public static Easing StepEnd { get; } = new("step-end");

    // Parametric factories

    /// <summary>
    /// Creates a custom easing function using a CSS <c>cubic-bezier()</c> curve.
    /// </summary>
    /// <param name="x1">The x-coordinate of the first control point (must be between 0 and 1).</param>
    /// <param name="y1">The y-coordinate of the first control point (can exceed the 0-1 range for overshoot effects).</param>
    /// <param name="x2">The x-coordinate of the second control point (must be between 0 and 1).</param>
    /// <param name="y2">The y-coordinate of the second control point (can exceed the 0-1 range for overshoot effects).</param>
    /// <returns>An <see cref="Easing"/> that serializes to <c>cubic-bezier(x1, y1, x2, y2)</c>.</returns>
    /// <example>
    /// <code>
    /// // Material Design standard curve
    /// Easing.CubicBezier(0.4, 0, 0.2, 1).ToCss(); // "cubic-bezier(0.4, 0, 0.2, 1)"
    /// </code>
    /// </example>
    public static Easing CubicBezier(double x1, double y1, double x2, double y2) =>
        new($"cubic-bezier({FormatNumber(x1)}, {FormatNumber(y1)}, {FormatNumber(x2)}, {FormatNumber(y2)})");

    /// <summary>
    /// Creates a step easing function using the CSS <c>steps()</c> notation.
    /// </summary>
    /// <param name="count">The number of intervals (steps) in the function.</param>
    /// <param name="jumpTerm">
    /// An optional jump term that specifies when the change occurs within each interval.
    /// Valid values include <c>"jump-start"</c>, <c>"jump-end"</c>, <c>"jump-none"</c>,
    /// <c>"jump-both"</c>, <c>"start"</c>, and <c>"end"</c>.
    /// When <see langword="null"/>, the browser default (<c>jump-end</c>) is used.
    /// </param>
    /// <returns>An <see cref="Easing"/> that serializes to <c>steps(count)</c> or <c>steps(count, jumpTerm)</c>.</returns>
    /// <example>
    /// <code>
    /// Easing.Steps(5).ToCss();                // "steps(5)"
    /// Easing.Steps(3, "jump-start").ToCss();  // "steps(3, jump-start)"
    /// </code>
    /// </example>
    public static Easing Steps(int count, string? jumpTerm = null) =>
        jumpTerm is null ? new($"steps({count})") : new($"steps({count}, {jumpTerm})");

    // Equality

    /// <inheritdoc />
    public bool Equals(Easing other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Easing other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Easing"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Easing left, Easing right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Easing"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Easing left, Easing right) => !left.Equals(right);
}
