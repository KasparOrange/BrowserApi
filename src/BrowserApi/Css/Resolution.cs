using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS resolution value used in media queries and image-related properties
/// (e.g., <c>96dpi</c>, <c>2dppx</c>, <c>300dpcm</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Resolution"/> is a hand-written extension of the generated partial struct that
/// adds factory methods for dots per inch (<see cref="Dpi"/>), dots per centimeter
/// (<see cref="Dpcm"/>), and dots per pixel (<see cref="Dppx"/>), plus a <c>calc()</c> factory.
/// </para>
/// <para>
/// Resolution values are primarily used in CSS media queries to target displays with specific
/// pixel densities. For example, <c>@media (min-resolution: 2dppx)</c> targets high-DPI
/// (Retina) displays.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Resolution standard = Resolution.Dpi(96);      // "96dpi"
/// Resolution retina = Resolution.Dppx(2);        // "2dppx"
/// Resolution metric = Resolution.Dpcm(300);      // "300dpcm"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
public readonly partial struct Resolution : IEquatable<Resolution> {
    /// <summary>
    /// Creates a resolution in dots per inch (<c>dpi</c>).
    /// </summary>
    /// <param name="value">The numeric value in dots per inch (e.g., 96 for standard displays).</param>
    /// <returns>A <see cref="Resolution"/> that serializes to <c>{value}dpi</c>.</returns>
    /// <example>
    /// <code>
    /// Resolution.Dpi(96).ToCss(); // "96dpi"
    /// </code>
    /// </example>
    public static Resolution Dpi(double value) => new($"{FormatNumber(value)}dpi");

    /// <summary>
    /// Creates a resolution in dots per centimeter (<c>dpcm</c>).
    /// </summary>
    /// <param name="value">The numeric value in dots per centimeter.</param>
    /// <returns>A <see cref="Resolution"/> that serializes to <c>{value}dpcm</c>.</returns>
    public static Resolution Dpcm(double value) => new($"{FormatNumber(value)}dpcm");

    /// <summary>
    /// Creates a resolution in dots per pixel unit (<c>dppx</c>), also known as device pixel ratio.
    /// </summary>
    /// <param name="value">The numeric value in dots per pixel (e.g., 2 for Retina/HiDPI displays).</param>
    /// <returns>A <see cref="Resolution"/> that serializes to <c>{value}dppx</c>.</returns>
    /// <remarks>
    /// <c>1dppx</c> is equivalent to <c>96dpi</c>. This is the most commonly used resolution unit
    /// for targeting high-DPI displays in media queries.
    /// </remarks>
    /// <example>
    /// <code>
    /// Resolution.Dppx(2).ToCss(); // "2dppx" (targets Retina displays)
    /// </code>
    /// </example>
    public static Resolution Dppx(double value) => new($"{FormatNumber(value)}dppx");

    /// <summary>
    /// Creates a resolution using a CSS <c>calc()</c> expression.
    /// </summary>
    /// <param name="expression">The calc expression content (without the surrounding <c>calc()</c>).</param>
    /// <returns>A <see cref="Resolution"/> that serializes to <c>calc({expression})</c>.</returns>
    public static Resolution Calc(string expression) => new($"calc({expression})");

    /// <inheritdoc />
    public bool Equals(Resolution other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Resolution other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Resolution"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Resolution left, Resolution right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Resolution"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Resolution left, Resolution right) => !left.Equals(right);
}
