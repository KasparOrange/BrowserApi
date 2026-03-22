using BrowserApi.Common;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS shadow value for use with the <c>box-shadow</c> or <c>text-shadow</c> properties
/// (e.g., <c>2px 4px 6px rgba(0, 0, 0, 0.5)</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Shadow"/> provides separate factory methods for box shadows (<see cref="Box"/>)
/// and text shadows (<see cref="Text"/>), reflecting the different CSS property syntaxes.
/// Box shadows support a spread radius and the <c>inset</c> keyword; text shadows do not.
/// </para>
/// <para>
/// Multiple shadows can be combined into a comma-separated list using <see cref="Combine"/>,
/// which is the standard way to apply multiple shadows to a single element.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Box shadow with blur and color
/// var shadow = Shadow.Box(Length.Px(2), Length.Px(4),
///     blur: Length.Px(6),
///     color: CssColor.Rgba(0, 0, 0, 0.3));
/// shadow.ToCss(); // "2px 4px 6px rgba(0, 0, 0, 0.3)"
///
/// // Inset box shadow
/// var inset = Shadow.Box(Length.Px(0), Length.Px(0),
///     blur: Length.Px(10),
///     spread: Length.Px(2),
///     color: CssColor.Blue,
///     inset: true);
/// inset.ToCss(); // "inset 0 0 10px 2px blue"
///
/// // Text shadow
/// var text = Shadow.Text(Length.Px(1), Length.Px(1),
///     blur: Length.Px(2),
///     color: CssColor.Gray);
///
/// // Combine multiple shadows
/// var multi = Shadow.Combine(shadow, inset);
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="CssColor"/>
/// <seealso cref="Length"/>
public readonly partial struct Shadow : ICssValue, IEquatable<Shadow> {
    private readonly string _value;

    /// <summary>
    /// Initializes a new <see cref="Shadow"/> with a raw CSS shadow string.
    /// </summary>
    /// <param name="value">The CSS shadow string (e.g., <c>"2px 4px 6px black"</c>).</param>
    public Shadow(string value) => _value = value;

    /// <summary>
    /// Serializes this shadow to its CSS string representation.
    /// </summary>
    /// <returns>The CSS shadow string.</returns>
    public string ToCss() => _value;

    /// <inheritdoc />
    public override string ToString() => _value;

    // Sentinel

    /// <summary>Gets a <see cref="Shadow"/> representing the CSS <c>none</c> keyword, which removes all shadows.</summary>
    public static Shadow None { get; } = new("none");

    // Box shadow

    /// <summary>
    /// Creates a CSS box shadow value for use with the <c>box-shadow</c> property.
    /// </summary>
    /// <param name="offsetX">The horizontal offset of the shadow. Positive values place the shadow to the right.</param>
    /// <param name="offsetY">The vertical offset of the shadow. Positive values place the shadow below.</param>
    /// <param name="blur">
    /// The blur radius. A larger value creates a more diffused shadow. Defaults to <see langword="null"/> (no blur).
    /// </param>
    /// <param name="spread">
    /// The spread radius. Positive values expand the shadow; negative values shrink it.
    /// Defaults to <see langword="null"/> (no spread). Only valid for box shadows, not text shadows.
    /// </param>
    /// <param name="color">
    /// The shadow color. Defaults to <see langword="null"/>, which uses the element's text color.
    /// </param>
    /// <param name="inset">
    /// When <see langword="true"/>, creates an inner shadow instead of an outer shadow.
    /// Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>A <see cref="Shadow"/> that serializes to a valid CSS <c>box-shadow</c> value.</returns>
    /// <example>
    /// <code>
    /// Shadow.Box(Length.Px(0), Length.Px(4),
    ///     blur: Length.Px(8),
    ///     color: CssColor.Rgba(0, 0, 0, 0.2)).ToCss();
    /// // "0 4px 8px rgba(0, 0, 0, 0.2)"
    /// </code>
    /// </example>
    public static Shadow Box(Length offsetX, Length offsetY,
        Length? blur = null, Length? spread = null,
        CssColor? color = null, bool inset = false) {
        var parts = new List<string>();
        if (inset) parts.Add("inset");
        parts.Add(offsetX.ToCss());
        parts.Add(offsetY.ToCss());
        if (blur is not null) parts.Add(blur.Value.ToCss());
        if (spread is not null) parts.Add(spread.Value.ToCss());
        if (color is not null) parts.Add(color.Value.ToCss());
        return new(string.Join(' ', parts));
    }

    // Text shadow

    /// <summary>
    /// Creates a CSS text shadow value for use with the <c>text-shadow</c> property.
    /// </summary>
    /// <param name="offsetX">The horizontal offset of the shadow.</param>
    /// <param name="offsetY">The vertical offset of the shadow.</param>
    /// <param name="blur">
    /// The blur radius. Defaults to <see langword="null"/> (no blur).
    /// </param>
    /// <param name="color">
    /// The shadow color. Defaults to <see langword="null"/>, which uses the element's text color.
    /// </param>
    /// <returns>A <see cref="Shadow"/> that serializes to a valid CSS <c>text-shadow</c> value.</returns>
    /// <remarks>
    /// Unlike <see cref="Box"/>, text shadows do not support the <c>spread</c> or <c>inset</c> parameters.
    /// </remarks>
    public static Shadow Text(Length offsetX, Length offsetY,
        Length? blur = null, CssColor? color = null) {
        var parts = new List<string>();
        parts.Add(offsetX.ToCss());
        parts.Add(offsetY.ToCss());
        if (blur is not null) parts.Add(blur.Value.ToCss());
        if (color is not null) parts.Add(color.Value.ToCss());
        return new(string.Join(' ', parts));
    }

    // Combine multiple shadows

    /// <summary>
    /// Combines multiple shadow values into a single comma-separated shadow list.
    /// </summary>
    /// <param name="shadows">The shadow values to combine.</param>
    /// <returns>A <see cref="Shadow"/> that serializes to a comma-separated list of the given shadows.</returns>
    /// <remarks>
    /// CSS <c>box-shadow</c> and <c>text-shadow</c> properties accept comma-separated lists
    /// of shadows. The first shadow in the list is rendered on top.
    /// </remarks>
    /// <example>
    /// <code>
    /// var combined = Shadow.Combine(
    ///     Shadow.Box(Length.Px(0), Length.Px(2), blur: Length.Px(4), color: CssColor.Rgba(0, 0, 0, 0.1)),
    ///     Shadow.Box(Length.Px(0), Length.Px(4), blur: Length.Px(8), color: CssColor.Rgba(0, 0, 0, 0.2)));
    /// // "0 2px 4px rgba(0, 0, 0, 0.1), 0 4px 8px rgba(0, 0, 0, 0.2)"
    /// </code>
    /// </example>
    public static Shadow Combine(params ReadOnlySpan<Shadow> shadows) {
        var parts = new string[shadows.Length];
        for (var i = 0; i < shadows.Length; i++)
            parts[i] = shadows[i].ToCss();
        return new(string.Join(", ", parts));
    }

    // Equality

    /// <inheritdoc />
    public bool Equals(Shadow other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Shadow other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Shadow"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Shadow left, Shadow right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Shadow"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Shadow left, Shadow right) => !left.Equals(right);
}
