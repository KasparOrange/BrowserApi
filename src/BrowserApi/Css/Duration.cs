using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS time/duration value (e.g., <c>0.3s</c>, <c>200ms</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Duration"/> is a hand-written extension of the generated partial struct that adds
/// factory methods for seconds and milliseconds, a zero constant, and a <c>calc()</c> factory.
/// </para>
/// <para>
/// Duration values are commonly used with CSS transitions and animations to specify how long
/// an effect takes to complete.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Duration fast = Duration.Ms(200);   // "200ms"
/// Duration slow = Duration.S(1.5);    // "1.5s"
/// Duration zero = Duration.Zero;      // "0s"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Transition"/>
/// <seealso cref="Easing"/>
/// <seealso cref="CssUnitExtensions"/>
public readonly partial struct Duration : IEquatable<Duration> {
    /// <summary>Gets a <see cref="Duration"/> representing zero seconds (<c>0s</c>).</summary>
    public static Duration Zero { get; } = new("0s");

    /// <summary>
    /// Creates a duration in seconds (<c>s</c>).
    /// </summary>
    /// <param name="value">The numeric value in seconds.</param>
    /// <returns>A <see cref="Duration"/> that serializes to <c>{value}s</c>.</returns>
    /// <example>
    /// <code>
    /// Duration.S(0.3).ToCss(); // "0.3s"
    /// </code>
    /// </example>
    public static Duration S(double value) => new($"{FormatNumber(value)}s");

    /// <summary>
    /// Creates a duration in milliseconds (<c>ms</c>).
    /// </summary>
    /// <param name="value">The numeric value in milliseconds.</param>
    /// <returns>A <see cref="Duration"/> that serializes to <c>{value}ms</c>.</returns>
    /// <example>
    /// <code>
    /// Duration.Ms(200).ToCss(); // "200ms"
    /// </code>
    /// </example>
    public static Duration Ms(double value) => new($"{FormatNumber(value)}ms");

    /// <summary>
    /// Creates a duration using a CSS <c>calc()</c> expression.
    /// </summary>
    /// <param name="expression">The calc expression content (without the surrounding <c>calc()</c>).</param>
    /// <returns>A <see cref="Duration"/> that serializes to <c>calc({expression})</c>.</returns>
    public static Duration Calc(string expression) => new($"calc({expression})");

    /// <inheritdoc />
    public bool Equals(Duration other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Duration other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Duration"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Duration left, Duration right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Duration"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Duration left, Duration right) => !left.Equals(right);
}
