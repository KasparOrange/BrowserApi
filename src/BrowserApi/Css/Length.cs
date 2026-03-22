using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS length value (e.g., <c>10px</c>, <c>2rem</c>, <c>50%</c>, <c>auto</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Length"/> is a hand-written extension of the generated partial struct that adds
/// unit-specific factory methods, the <c>auto</c> and <c>zero</c> keywords, implicit conversions
/// from numeric types, and arithmetic operators that produce <c>calc()</c> expressions.
/// </para>
/// <para>
/// Implicit conversions from <see cref="int"/> and <see cref="double"/> use pixels (<c>px</c>)
/// as the default unit, allowing you to write <c>Length margin = 16;</c> as shorthand for
/// <c>Length.Px(16)</c>.
/// </para>
/// <para>
/// Arithmetic operators (<c>+</c>, <c>-</c>, unary <c>-</c>) produce CSS <c>calc()</c>
/// expressions, enabling mixed-unit calculations like <c>Length.Percent(100) - Length.Px(20)</c>,
/// which serializes to <c>calc(100% - 20px)</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Factory methods
/// Length px = Length.Px(16);       // "16px"
/// Length rem = Length.Rem(1.5);    // "1.5rem"
/// Length pct = Length.Percent(50); // "50%"
///
/// // Implicit conversion (defaults to px)
/// Length margin = 8;              // "8px"
///
/// // Calc expressions via operators
/// Length mixed = Length.Percent(100) - Length.Px(20); // "calc(100% - 20px)"
///
/// // Special values
/// Length auto = Length.Auto;      // "auto"
/// Length zero = Length.Zero;      // "0"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="CssUnitExtensions"/>
/// <seealso cref="Percentage"/>
public readonly partial struct Length : IEquatable<Length> {
    /// <summary>Gets a <see cref="Length"/> representing the CSS value <c>0</c> (zero, no unit).</summary>
    public static Length Zero { get; } = new("0");

    /// <summary>Gets a <see cref="Length"/> representing the CSS <c>auto</c> keyword.</summary>
    /// <remarks>
    /// The <c>auto</c> keyword allows the browser to calculate the length automatically.
    /// Its behavior depends on the property it is applied to (e.g., <c>margin: auto</c> centers a block element).
    /// </remarks>
    public static Length Auto { get; } = new("auto");

    /// <summary>
    /// Creates a length in pixels (<c>px</c>).
    /// </summary>
    /// <param name="value">The numeric value in pixels.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>{value}px</c>.</returns>
    public static Length Px(double value) => new($"{FormatNumber(value)}px");

    /// <summary>
    /// Creates a length in em units (<c>em</c>), relative to the element's font size.
    /// </summary>
    /// <param name="value">The numeric value in em units.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>{value}em</c>.</returns>
    public static Length Em(double value) => new($"{FormatNumber(value)}em");

    /// <summary>
    /// Creates a length in root em units (<c>rem</c>), relative to the root element's font size.
    /// </summary>
    /// <param name="value">The numeric value in rem units.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>{value}rem</c>.</returns>
    /// <example>
    /// <code>
    /// Length.Rem(1.5).ToCss(); // "1.5rem"
    /// </code>
    /// </example>
    public static Length Rem(double value) => new($"{FormatNumber(value)}rem");

    /// <summary>
    /// Creates a length in viewport height units (<c>vh</c>), where <c>1vh</c> equals 1% of the viewport height.
    /// </summary>
    /// <param name="value">The numeric value in vh units.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>{value}vh</c>.</returns>
    public static Length Vh(double value) => new($"{FormatNumber(value)}vh");

    /// <summary>
    /// Creates a length in viewport width units (<c>vw</c>), where <c>1vw</c> equals 1% of the viewport width.
    /// </summary>
    /// <param name="value">The numeric value in vw units.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>{value}vw</c>.</returns>
    public static Length Vw(double value) => new($"{FormatNumber(value)}vw");

    /// <summary>
    /// Creates a length as a percentage (<c>%</c>) of the containing element's corresponding dimension.
    /// </summary>
    /// <param name="value">The numeric percentage value.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>{value}%</c>.</returns>
    public static Length Percent(double value) => new($"{FormatNumber(value)}%");

    /// <summary>
    /// Creates a length using a CSS <c>calc()</c> expression for dynamic calculations.
    /// </summary>
    /// <param name="expression">The calc expression content (without the surrounding <c>calc()</c>).</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>calc({expression})</c>.</returns>
    /// <example>
    /// <code>
    /// Length.Calc("100% - 20px").ToCss(); // "calc(100% - 20px)"
    /// </code>
    /// </example>
    public static Length Calc(string expression) => new($"calc({expression})");

    /// <summary>
    /// Implicitly converts an <see cref="int"/> to a <see cref="Length"/> in pixels.
    /// </summary>
    /// <param name="value">The pixel value.</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Px"/>.</returns>
    public static implicit operator Length(int value) => Px(value);

    /// <summary>
    /// Implicitly converts a <see cref="double"/> to a <see cref="Length"/> in pixels.
    /// </summary>
    /// <param name="value">The pixel value.</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Px"/>.</returns>
    public static implicit operator Length(double value) => Px(value);

    /// <summary>
    /// Adds two lengths, producing a CSS <c>calc()</c> expression.
    /// </summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>calc({a} + {b})</c>.</returns>
    /// <example>
    /// <code>
    /// var result = Length.Rem(2) + Length.Px(10);
    /// result.ToCss(); // "calc(2rem + 10px)"
    /// </code>
    /// </example>
    public static Length operator +(Length a, Length b) => new($"calc({a.ToCss()} + {b.ToCss()})");

    /// <summary>
    /// Subtracts one length from another, producing a CSS <c>calc()</c> expression.
    /// </summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>calc({a} - {b})</c>.</returns>
    /// <example>
    /// <code>
    /// var result = Length.Percent(100) - Length.Px(20);
    /// result.ToCss(); // "calc(100% - 20px)"
    /// </code>
    /// </example>
    public static Length operator -(Length a, Length b) => new($"calc({a.ToCss()} - {b.ToCss()})");

    /// <summary>
    /// Negates a length, producing a CSS <c>calc()</c> expression that multiplies by <c>-1</c>.
    /// </summary>
    /// <param name="a">The length to negate.</param>
    /// <returns>A <see cref="Length"/> that serializes to <c>calc(-1 * {a})</c>.</returns>
    public static Length operator -(Length a) => new($"calc(-1 * {a.ToCss()})");

    // Equality

    /// <inheritdoc />
    public bool Equals(Length other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Length other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Length"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Length left, Length right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Length"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Length left, Length right) => !left.Equals(right);
}
