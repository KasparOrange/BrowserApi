using BrowserApi.Common;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS gradient image value, including linear, radial, conic, and their
/// repeating variants (e.g., <c>linear-gradient(red, blue)</c>, <c>radial-gradient(circle, red, blue)</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Gradient"/> provides static factory methods for all standard CSS gradient types:
/// <see cref="Linear(ReadOnlySpan{GradientStop})">Linear</see>,
/// <see cref="Radial(ReadOnlySpan{GradientStop})">Radial</see>,
/// <see cref="Conic(ReadOnlySpan{GradientStop})">Conic</see>,
/// and their repeating counterparts.
/// </para>
/// <para>
/// Each factory accepts a variable number of <see cref="GradientStop"/> values via
/// <see cref="ReadOnlySpan{T}"/> params. Thanks to the implicit conversion from
/// <see cref="CssColor"/> to <see cref="GradientStop"/>, you can pass colors directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple linear gradient
/// var gradient = Gradient.Linear(CssColor.Red, CssColor.Blue);
/// gradient.ToCss(); // "linear-gradient(red, blue)"
///
/// // Linear gradient with angle
/// var angled = Gradient.Linear(Angle.Deg(45), CssColor.Red, CssColor.Blue);
/// angled.ToCss(); // "linear-gradient(45deg, red, blue)"
///
/// // Radial gradient with positioned stops
/// var radial = Gradient.Radial("circle",
///     GradientStop.At(CssColor.White, Percentage.Of(0)),
///     GradientStop.At(CssColor.Black, Percentage.Of(100)));
/// radial.ToCss(); // "radial-gradient(circle, white 0%, black 100%)"
///
/// // Repeating linear gradient
/// var repeating = Gradient.RepeatingLinear(Angle.Deg(45),
///     GradientStop.At(CssColor.Red, Length.Px(0)),
///     GradientStop.At(CssColor.Blue, Length.Px(20)));
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="GradientStop"/>
/// <seealso cref="CssColor"/>
/// <seealso cref="Angle"/>
public readonly partial struct Gradient : ICssValue, IEquatable<Gradient> {
    private readonly string _value;

    /// <summary>
    /// Initializes a new <see cref="Gradient"/> with a raw CSS gradient string.
    /// </summary>
    /// <param name="value">The CSS gradient string (e.g., <c>"linear-gradient(red, blue)"</c>).</param>
    public Gradient(string value) => _value = value;

    /// <summary>
    /// Serializes this gradient to its CSS string representation.
    /// </summary>
    /// <returns>The CSS gradient string.</returns>
    public string ToCss() => _value;

    /// <inheritdoc />
    public override string ToString() => _value;

    // Linear

    /// <summary>
    /// Creates a <c>linear-gradient()</c> with automatically determined direction (top to bottom).
    /// </summary>
    /// <param name="stops">The color stops that define the gradient.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>linear-gradient({stops})</c>.</returns>
    public static Gradient Linear(params ReadOnlySpan<GradientStop> stops) =>
        new($"linear-gradient({FormatStops(stops)})");

    /// <summary>
    /// Creates a <c>linear-gradient()</c> with a specified angle direction.
    /// </summary>
    /// <param name="angle">The gradient line angle (e.g., <c>Angle.Deg(45)</c> for a diagonal gradient).</param>
    /// <param name="stops">The color stops that define the gradient.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>linear-gradient({angle}, {stops})</c>.</returns>
    /// <example>
    /// <code>
    /// Gradient.Linear(Angle.Deg(90), CssColor.Red, CssColor.Blue).ToCss();
    /// // "linear-gradient(90deg, red, blue)"
    /// </code>
    /// </example>
    public static Gradient Linear(Angle angle, params ReadOnlySpan<GradientStop> stops) =>
        new($"linear-gradient({angle.ToCss()}, {FormatStops(stops)})");

    // Radial

    /// <summary>
    /// Creates a <c>radial-gradient()</c> with default shape and size (ellipse farthest-corner).
    /// </summary>
    /// <param name="stops">The color stops that define the gradient.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>radial-gradient({stops})</c>.</returns>
    public static Gradient Radial(params ReadOnlySpan<GradientStop> stops) =>
        new($"radial-gradient({FormatStops(stops)})");

    /// <summary>
    /// Creates a <c>radial-gradient()</c> with a specified shape descriptor.
    /// </summary>
    /// <param name="shape">
    /// The shape and/or size of the gradient (e.g., <c>"circle"</c>, <c>"ellipse closest-side"</c>,
    /// <c>"circle at center"</c>).
    /// </param>
    /// <param name="stops">The color stops that define the gradient.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>radial-gradient({shape}, {stops})</c>.</returns>
    public static Gradient Radial(string shape, params ReadOnlySpan<GradientStop> stops) =>
        new($"radial-gradient({shape}, {FormatStops(stops)})");

    // Conic

    /// <summary>
    /// Creates a <c>conic-gradient()</c> with default starting angle (from the top).
    /// </summary>
    /// <param name="stops">The color stops that define the gradient.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>conic-gradient({stops})</c>.</returns>
    public static Gradient Conic(params ReadOnlySpan<GradientStop> stops) =>
        new($"conic-gradient({FormatStops(stops)})");

    /// <summary>
    /// Creates a <c>conic-gradient()</c> with a specified starting angle.
    /// </summary>
    /// <param name="fromAngle">The starting angle of the conic gradient.</param>
    /// <param name="stops">The color stops that define the gradient.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>conic-gradient(from {angle}, {stops})</c>.</returns>
    public static Gradient Conic(Angle fromAngle, params ReadOnlySpan<GradientStop> stops) =>
        new($"conic-gradient(from {fromAngle.ToCss()}, {FormatStops(stops)})");

    // Repeating variants

    /// <summary>
    /// Creates a <c>repeating-linear-gradient()</c> that repeats the gradient pattern infinitely.
    /// </summary>
    /// <param name="angle">The gradient line angle.</param>
    /// <param name="stops">The color stops that define the repeating pattern.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>repeating-linear-gradient({angle}, {stops})</c>.</returns>
    public static Gradient RepeatingLinear(Angle angle, params ReadOnlySpan<GradientStop> stops) =>
        new($"repeating-linear-gradient({angle.ToCss()}, {FormatStops(stops)})");

    /// <summary>
    /// Creates a <c>repeating-radial-gradient()</c> that repeats the gradient pattern infinitely.
    /// </summary>
    /// <param name="shape">The shape and/or size descriptor for the gradient.</param>
    /// <param name="stops">The color stops that define the repeating pattern.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>repeating-radial-gradient({shape}, {stops})</c>.</returns>
    public static Gradient RepeatingRadial(string shape, params ReadOnlySpan<GradientStop> stops) =>
        new($"repeating-radial-gradient({shape}, {FormatStops(stops)})");

    /// <summary>
    /// Creates a <c>repeating-conic-gradient()</c> that repeats the gradient pattern infinitely.
    /// </summary>
    /// <param name="fromAngle">The starting angle of the conic gradient.</param>
    /// <param name="stops">The color stops that define the repeating pattern.</param>
    /// <returns>A <see cref="Gradient"/> that serializes to <c>repeating-conic-gradient(from {angle}, {stops})</c>.</returns>
    public static Gradient RepeatingConic(Angle fromAngle, params ReadOnlySpan<GradientStop> stops) =>
        new($"repeating-conic-gradient(from {fromAngle.ToCss()}, {FormatStops(stops)})");

    // Helper
    private static string FormatStops(ReadOnlySpan<GradientStop> stops) {
        var parts = new string[stops.Length];
        for (var i = 0; i < stops.Length; i++)
            parts[i] = stops[i].ToCss();
        return string.Join(", ", parts);
    }

    // Equality

    /// <inheritdoc />
    public bool Equals(Gradient other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Gradient other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Gradient"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Gradient left, Gradient right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Gradient"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Gradient left, Gradient right) => !left.Equals(right);
}
