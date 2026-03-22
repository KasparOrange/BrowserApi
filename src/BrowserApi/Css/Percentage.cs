using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS percentage value (e.g., <c>50%</c>, <c>100%</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Percentage"/> is a hand-written extension of the generated partial struct that adds
/// the <see cref="Of"/> factory method, a zero constant, and a <c>calc()</c> factory.
/// </para>
/// <para>
/// Unlike <see cref="Length.Percent"/>, which returns a <see cref="Length"/>, this type is used
/// in contexts where the CSS grammar specifically requires a <c>&lt;percentage&gt;</c> rather than
/// a <c>&lt;length-percentage&gt;</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Percentage half = Percentage.Of(50);    // "50%"
/// Percentage full = Percentage.Of(100);   // "100%"
/// Percentage zero = Percentage.Zero;      // "0%"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Length"/>
/// <seealso cref="CssUnitExtensions"/>
public readonly partial struct Percentage : IEquatable<Percentage> {
    /// <summary>Gets a <see cref="Percentage"/> representing zero percent (<c>0%</c>).</summary>
    public static Percentage Zero { get; } = new("0%");

    /// <summary>
    /// Creates a percentage value.
    /// </summary>
    /// <param name="value">The numeric percentage value (e.g., 50 for <c>50%</c>).</param>
    /// <returns>A <see cref="Percentage"/> that serializes to <c>{value}%</c>.</returns>
    /// <example>
    /// <code>
    /// Percentage.Of(75).ToCss(); // "75%"
    /// </code>
    /// </example>
    public static Percentage Of(double value) => new($"{FormatNumber(value)}%");

    /// <summary>
    /// Creates a percentage using a CSS <c>calc()</c> expression.
    /// </summary>
    /// <param name="expression">The calc expression content (without the surrounding <c>calc()</c>).</param>
    /// <returns>A <see cref="Percentage"/> that serializes to <c>calc({expression})</c>.</returns>
    public static Percentage Calc(string expression) => new($"calc({expression})");

    /// <inheritdoc />
    public bool Equals(Percentage other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Percentage other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Percentage"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Percentage left, Percentage right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Percentage"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Percentage left, Percentage right) => !left.Equals(right);
}
