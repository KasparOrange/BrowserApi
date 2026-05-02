using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS color value that serializes to a valid CSS color string.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CssColor"/> is a hand-written extension of the generated partial struct that adds
/// named color constants, factory methods for functional color notations (RGB, HSL, hex), and
/// CSS keywords like <c>inherit</c> and <c>currentcolor</c>.
/// </para>
/// <para>
/// This type implements value equality based on the underlying CSS string representation.
/// Two <see cref="CssColor"/> instances are considered equal if and only if their
/// <see cref="ICssValue.ToCss"/> output is identical.
/// </para>
/// <para>
/// The generated partial struct provides the <see cref="ICssValue.ToCss"/> and
/// <see cref="object.ToString"/> implementations, the backing <c>_value</c> field,
/// and the constructor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Named colors
/// CssColor color = CssColor.Red;
///
/// // Functional notation
/// CssColor custom = CssColor.Rgb(128, 0, 255);
/// CssColor semiTransparent = CssColor.Rgba(0, 0, 0, 0.5);
///
/// // Hex notation
/// CssColor hex = CssColor.Hex("#ff0080");
///
/// // Serialization
/// string css = CssColor.Hsl(200, 50, 70).ToCss(); // "hsl(200, 50%, 70%)"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="GradientStop"/>
/// <seealso cref="Shadow"/>
public readonly partial struct CssColor : IEquatable<CssColor> {
    // Named colors

    /// <summary>Gets the CSS <c>transparent</c> color keyword.</summary>
    public static CssColor Transparent { get; } = new("transparent");

    /// <summary>Gets the CSS <c>black</c> named color (<c>#000000</c>).</summary>
    public static CssColor Black { get; } = new("black");

    /// <summary>Gets the CSS <c>white</c> named color (<c>#ffffff</c>).</summary>
    public static CssColor White { get; } = new("white");

    /// <summary>Gets the CSS <c>red</c> named color (<c>#ff0000</c>).</summary>
    public static CssColor Red { get; } = new("red");

    /// <summary>Gets the CSS <c>green</c> named color (<c>#008000</c>).</summary>
    public static CssColor Green { get; } = new("green");

    /// <summary>Gets the CSS <c>blue</c> named color (<c>#0000ff</c>).</summary>
    public static CssColor Blue { get; } = new("blue");

    /// <summary>Gets the CSS <c>yellow</c> named color (<c>#ffff00</c>).</summary>
    public static CssColor Yellow { get; } = new("yellow");

    /// <summary>Gets the CSS <c>cyan</c> named color (<c>#00ffff</c>).</summary>
    public static CssColor Cyan { get; } = new("cyan");

    /// <summary>Gets the CSS <c>magenta</c> named color (<c>#ff00ff</c>).</summary>
    public static CssColor Magenta { get; } = new("magenta");

    /// <summary>Gets the CSS <c>orange</c> named color (<c>#ffa500</c>).</summary>
    public static CssColor Orange { get; } = new("orange");

    /// <summary>Gets the CSS <c>purple</c> named color (<c>#800080</c>).</summary>
    public static CssColor Purple { get; } = new("purple");

    /// <summary>Gets the CSS <c>gray</c> named color (<c>#808080</c>).</summary>
    public static CssColor Gray { get; } = new("gray");

    // CSS keywords

    /// <summary>Gets the CSS <c>inherit</c> keyword, which inherits the color from the parent element.</summary>
    public static CssColor Inherit { get; } = new("inherit");

    /// <summary>Gets the CSS <c>currentcolor</c> keyword, which refers to the element's computed <c>color</c> property value.</summary>
    public static CssColor CurrentColor { get; } = new("currentcolor");

    // Factories

    /// <summary>
    /// Creates a color using the CSS <c>rgb()</c> functional notation.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <returns>A <see cref="CssColor"/> that serializes to <c>rgb(r, g, b)</c>.</returns>
    /// <example>
    /// <code>
    /// var color = CssColor.Rgb(255, 128, 0);
    /// color.ToCss(); // "rgb(255, 128, 0)"
    /// </code>
    /// </example>
    public static CssColor Rgb(int r, int g, int b) =>
        new($"rgb({r}, {g}, {b})");

    /// <summary>
    /// Creates a color using the CSS <c>rgba()</c> functional notation with an alpha channel.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <param name="a">The alpha (opacity) value (0.0 = fully transparent, 1.0 = fully opaque).</param>
    /// <returns>A <see cref="CssColor"/> that serializes to <c>rgba(r, g, b, a)</c>.</returns>
    /// <example>
    /// <code>
    /// var color = CssColor.Rgba(0, 0, 0, 0.5);
    /// color.ToCss(); // "rgba(0, 0, 0, 0.5)"
    /// </code>
    /// </example>
    public static CssColor Rgba(int r, int g, int b, double a) =>
        new($"rgba({r}, {g}, {b}, {FormatNumber(a)})");

    /// <summary>
    /// Creates a color using the CSS <c>hsl()</c> functional notation.
    /// </summary>
    /// <param name="h">The hue angle in degrees (0-360).</param>
    /// <param name="s">The saturation percentage (0-100).</param>
    /// <param name="l">The lightness percentage (0-100).</param>
    /// <returns>A <see cref="CssColor"/> that serializes to <c>hsl(h, s%, l%)</c>.</returns>
    /// <example>
    /// <code>
    /// var color = CssColor.Hsl(200, 50, 70);
    /// color.ToCss(); // "hsl(200, 50%, 70%)"
    /// </code>
    /// </example>
    public static CssColor Hsl(int h, int s, int l) =>
        new($"hsl({h}, {s}%, {l}%)");

    /// <summary>
    /// Creates a color using the CSS <c>hsla()</c> functional notation with an alpha channel.
    /// </summary>
    /// <param name="h">The hue angle in degrees (0-360).</param>
    /// <param name="s">The saturation percentage (0-100).</param>
    /// <param name="l">The lightness percentage (0-100).</param>
    /// <param name="a">The alpha (opacity) value (0.0 = fully transparent, 1.0 = fully opaque).</param>
    /// <returns>A <see cref="CssColor"/> that serializes to <c>hsla(h, s%, l%, a)</c>.</returns>
    /// <example>
    /// <code>
    /// var color = CssColor.Hsla(120, 100, 50, 0.75);
    /// color.ToCss(); // "hsla(120, 100%, 50%, 0.75)"
    /// </code>
    /// </example>
    public static CssColor Hsla(int h, int s, int l, double a) =>
        new($"hsla({h}, {s}%, {l}%, {FormatNumber(a)})");

    /// <summary>
    /// Creates a color from a CSS hex color string.
    /// </summary>
    /// <param name="hex">
    /// A hex color string in the format <c>#rgb</c> (3-digit shorthand) or <c>#rrggbb</c> (6-digit).
    /// The leading <c>#</c> is required.
    /// </param>
    /// <returns>A <see cref="CssColor"/> that serializes to the given hex string.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="hex"/> is null, whitespace, does not start with <c>#</c>,
    /// or is not a valid length (4, 5, 7, or 9 characters including the <c>#</c>).
    /// </exception>
    /// <example>
    /// <code>
    /// var color = CssColor.Hex("#ff0080");
    /// color.ToCss(); // "#ff0080"
    ///
    /// var withAlpha = CssColor.Hex("#ff008080");
    /// withAlpha.ToCss(); // "#ff008080"
    ///
    /// var short3 = CssColor.Hex("#f08");
    /// short3.ToCss(); // "#f08"
    ///
    /// var short4 = CssColor.Hex("#f088");
    /// short4.ToCss(); // "#f088"
    /// </code>
    /// </example>
    public static CssColor Hex(string hex) {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        if (hex[0] != '#' || (hex.Length != 4 && hex.Length != 5 && hex.Length != 7 && hex.Length != 9))
            throw new ArgumentException($"Invalid hex color format: '{hex}'. Expected '#rgb', '#rgba', '#rrggbb', or '#rrggbbaa'.", nameof(hex));
        return new(hex);
    }

    // Equality

    /// <inheritdoc />
    public bool Equals(CssColor other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CssColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="CssColor"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(CssColor left, CssColor right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="CssColor"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(CssColor left, CssColor right) => !left.Equals(right);

}
