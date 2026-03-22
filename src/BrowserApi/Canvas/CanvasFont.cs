using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Canvas;

/// <summary>
/// Represents a canvas font specification as an immutable value type that serializes to CSS
/// font shorthand syntax (e.g., <c>"bold italic 16px Arial"</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CanvasFont"/> follows the CSS font shorthand format expected by
/// <see cref="Dom.CanvasRenderingContext2D"/>. It is constructed via the <see cref="Of"/> factory
/// method and customized through fluent methods such as <see cref="Bold"/>, <see cref="Italic"/>,
/// <see cref="WithWeight"/>, and <see cref="WithFamily"/>. Each method returns a new instance,
/// preserving immutability.
/// </para>
/// <para>
/// The type implements <see cref="ICssValue"/> for consistent CSS serialization and provides
/// an implicit conversion to <see cref="string"/> so it can be assigned directly to the
/// canvas context's <c>Font</c> property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic font
/// var font = CanvasFont.Of(16, "Arial");
/// ctx.Font = font; // "16px Arial"
///
/// // Styled font
/// var boldItalic = CanvasFont.Of(24, "Georgia").Bold().Italic();
/// ctx.Font = boldItalic; // "italic bold 24px Georgia"
///
/// // Custom weight
/// var light = CanvasFont.Of(14, "Roboto").WithWeight("300");
/// ctx.Font = light; // "300 14px Roboto"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Dom.CanvasRenderingContext2D"/>
public readonly struct CanvasFont : ICssValue, IEquatable<CanvasFont> {
    private readonly double _sizePx;
    private readonly string _family;
    private readonly string? _weight;
    private readonly string? _style;

    private CanvasFont(double sizePx, string family, string? weight, string? style) {
        _sizePx = sizePx;
        _family = family;
        _weight = weight;
        _style = style;
    }

    /// <summary>
    /// Creates a new <see cref="CanvasFont"/> with the specified pixel size and font family.
    /// </summary>
    /// <param name="sizePx">The font size in pixels.</param>
    /// <param name="family">The font family name (e.g., <c>"Arial"</c>, <c>"sans-serif"</c>).</param>
    /// <returns>A new <see cref="CanvasFont"/> with normal weight and style.</returns>
    /// <example>
    /// <code>
    /// var font = CanvasFont.Of(16, "Helvetica");
    /// </code>
    /// </example>
    public static CanvasFont Of(double sizePx, string family) => new(sizePx, family, null, null);

    /// <summary>
    /// Returns a new <see cref="CanvasFont"/> with <c>bold</c> weight applied.
    /// </summary>
    /// <returns>A new font instance with bold weight.</returns>
    public CanvasFont Bold() => new(_sizePx, _family, "bold", _style);

    /// <summary>
    /// Returns a new <see cref="CanvasFont"/> with <c>italic</c> style applied.
    /// </summary>
    /// <returns>A new font instance with italic style.</returns>
    public CanvasFont Italic() => new(_sizePx, _family, _weight, "italic");

    /// <summary>
    /// Returns a new <see cref="CanvasFont"/> with the specified font weight.
    /// </summary>
    /// <param name="weight">
    /// The font weight as a CSS value (e.g., <c>"bold"</c>, <c>"normal"</c>, <c>"300"</c>, <c>"700"</c>).
    /// </param>
    /// <returns>A new font instance with the given weight.</returns>
    public CanvasFont WithWeight(string weight) => new(_sizePx, _family, weight, _style);

    /// <summary>
    /// Returns a new <see cref="CanvasFont"/> with the specified font style.
    /// </summary>
    /// <param name="style">
    /// The font style as a CSS value (e.g., <c>"italic"</c>, <c>"oblique"</c>, <c>"normal"</c>).
    /// </param>
    /// <returns>A new font instance with the given style.</returns>
    public CanvasFont WithStyle(string style) => new(_sizePx, _family, _weight, style);

    /// <summary>
    /// Returns a new <see cref="CanvasFont"/> with the specified pixel size.
    /// </summary>
    /// <param name="sizePx">The new font size in pixels.</param>
    /// <returns>A new font instance with the given size.</returns>
    public CanvasFont WithSize(double sizePx) => new(sizePx, _family, _weight, _style);

    /// <summary>
    /// Returns a new <see cref="CanvasFont"/> with the specified font family.
    /// </summary>
    /// <param name="family">The new font family name.</param>
    /// <returns>A new font instance with the given family.</returns>
    public CanvasFont WithFamily(string family) => new(_sizePx, family, _weight, _style);

    /// <summary>
    /// Serializes this font to the CSS font shorthand format.
    /// </summary>
    /// <returns>
    /// A string in the format <c>"[style] [weight] sizepx family"</c>,
    /// e.g., <c>"italic bold 16px Arial"</c>.
    /// </returns>
    public string ToCss() {
        var parts = new List<string>(4);
        if (_style != null) parts.Add(_style);
        if (_weight != null) parts.Add(_weight);
        parts.Add($"{FormatNumber(_sizePx)}px");
        parts.Add(_family);
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Returns the CSS font shorthand representation of this font.
    /// </summary>
    /// <returns>The same value as <see cref="ToCss"/>.</returns>
    public override string ToString() => ToCss();

    /// <summary>
    /// Implicitly converts a <see cref="CanvasFont"/> to its CSS string representation,
    /// allowing direct assignment to string-typed font properties.
    /// </summary>
    /// <param name="font">The font to convert.</param>
    /// <returns>The CSS font shorthand string.</returns>
    public static implicit operator string(CanvasFont font) => font.ToCss();

    // ── Equality ────────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether this font is equal to another <see cref="CanvasFont"/>.
    /// </summary>
    /// <param name="other">The font to compare with.</param>
    /// <returns><see langword="true"/> if all font properties (size, family, weight, style) are equal.</returns>
    public bool Equals(CanvasFont other) =>
        _sizePx == other._sizePx && _family == other._family &&
        _weight == other._weight && _style == other._style;

    /// <summary>
    /// Determines whether this font is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="CanvasFont"/> with equal properties.</returns>
    public override bool Equals(object? obj) => obj is CanvasFont other && Equals(other);

    /// <summary>
    /// Returns a hash code based on all font properties.
    /// </summary>
    /// <returns>A combined hash of size, family, weight, and style.</returns>
    public override int GetHashCode() => HashCode.Combine(_sizePx, _family, _weight, _style);

    /// <summary>
    /// Determines whether two <see cref="CanvasFont"/> values are equal.
    /// </summary>
    /// <param name="left">The first font.</param>
    /// <param name="right">The second font.</param>
    /// <returns><see langword="true"/> if both fonts have identical properties.</returns>
    public static bool operator ==(CanvasFont left, CanvasFont right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="CanvasFont"/> values are not equal.
    /// </summary>
    /// <param name="left">The first font.</param>
    /// <param name="right">The second font.</param>
    /// <returns><see langword="true"/> if the fonts differ in any property.</returns>
    public static bool operator !=(CanvasFont left, CanvasFont right) => !left.Equals(right);
}
