using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS flexible length value in the <c>fr</c> (fraction) unit, used in CSS Grid
/// layouts to distribute available space proportionally (e.g., <c>1fr</c>, <c>2.5fr</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Flex"/> is a hand-written extension of the generated partial struct that adds
/// the <see cref="Fr"/> factory method. The <c>fr</c> unit represents a fraction of the
/// free space in a grid container and is only valid in grid-related properties like
/// <c>grid-template-columns</c> and <c>grid-template-rows</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Flex one = Flex.Fr(1);      // "1fr"
/// Flex two = Flex.Fr(2);      // "2fr"
/// Flex half = Flex.Fr(0.5);   // "0.5fr"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Length"/>
/// <seealso cref="CssUnitExtensions"/>
public readonly partial struct Flex : IEquatable<Flex> {
    /// <summary>
    /// Creates a flex value in fractional units (<c>fr</c>).
    /// </summary>
    /// <param name="value">The numeric value representing the proportion of available space.</param>
    /// <returns>A <see cref="Flex"/> that serializes to <c>{value}fr</c>.</returns>
    /// <example>
    /// <code>
    /// // A 3-column grid: 1fr 2fr 1fr
    /// Flex.Fr(1).ToCss(); // "1fr"
    /// Flex.Fr(2).ToCss(); // "2fr"
    /// </code>
    /// </example>
    public static Flex Fr(double value) => new($"{FormatNumber(value)}fr");

    /// <inheritdoc />
    public bool Equals(Flex other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Flex other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Flex"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Flex left, Flex right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Flex"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Flex left, Flex right) => !left.Equals(right);
}
