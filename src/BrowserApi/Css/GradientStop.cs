namespace BrowserApi.Css;

/// <summary>
/// Represents a single color stop within a CSS gradient, consisting of a color and an optional position.
/// </summary>
/// <remarks>
/// <para>
/// A gradient stop maps a <see cref="CssColor"/> to an optional position (expressed as a
/// <see cref="Length"/> or <see cref="Percentage"/>). When no position is specified, the browser
/// distributes the stop evenly between its neighbors.
/// </para>
/// <para>
/// <see cref="GradientStop"/> supports implicit conversion from <see cref="CssColor"/>, so you
/// can pass color values directly where gradient stops are expected.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple stop (position determined automatically)
/// GradientStop stop1 = CssColor.Red; // implicit conversion
///
/// // Stop at a specific position
/// GradientStop stop2 = GradientStop.At(CssColor.Blue, Percentage.Of(50));
/// stop2.ToCss(); // "blue 50%"
///
/// // Stop at a length position
/// GradientStop stop3 = GradientStop.At(CssColor.Green, Length.Px(100));
/// stop3.ToCss(); // "green 100px"
/// </code>
/// </example>
/// <seealso cref="Gradient"/>
/// <seealso cref="CssColor"/>
/// <param name="Color">The color at this gradient stop.</param>
/// <param name="Position">
/// The optional position of this stop within the gradient, expressed as a serialized CSS value
/// (e.g., <c>"50%"</c>, <c>"100px"</c>). When <see langword="null"/>, the browser auto-distributes
/// the stop.
/// </param>
public readonly record struct GradientStop(CssColor Color, string? Position = null) {
    /// <summary>
    /// Serializes this gradient stop to its CSS string representation.
    /// </summary>
    /// <returns>
    /// The color's CSS string if no position is set; otherwise, the color and position
    /// separated by a space (e.g., <c>"red 50%"</c>).
    /// </returns>
    public string ToCss() => Position is null
        ? Color.ToCss()
        : $"{Color.ToCss()} {Position}";

    /// <summary>
    /// Implicitly converts a <see cref="CssColor"/> to a <see cref="GradientStop"/> with no position.
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>A <see cref="GradientStop"/> with the specified color and no position.</returns>
    public static implicit operator GradientStop(CssColor color) => new(color);

    /// <summary>
    /// Creates a gradient stop at a specific length position.
    /// </summary>
    /// <param name="color">The color at this stop.</param>
    /// <param name="position">The position of this stop as a <see cref="Length"/>.</param>
    /// <returns>A <see cref="GradientStop"/> with the given color and position.</returns>
    /// <example>
    /// <code>
    /// GradientStop.At(CssColor.Red, Length.Px(100)).ToCss(); // "red 100px"
    /// </code>
    /// </example>
    public static GradientStop At(CssColor color, Length position) =>
        new(color, position.ToCss());

    /// <summary>
    /// Creates a gradient stop at a specific percentage position.
    /// </summary>
    /// <param name="color">The color at this stop.</param>
    /// <param name="position">The position of this stop as a <see cref="Percentage"/>.</param>
    /// <returns>A <see cref="GradientStop"/> with the given color and position.</returns>
    /// <example>
    /// <code>
    /// GradientStop.At(CssColor.Blue, Percentage.Of(75)).ToCss(); // "blue 75%"
    /// </code>
    /// </example>
    public static GradientStop At(CssColor color, Percentage position) =>
        new(color, position.ToCss());
}
