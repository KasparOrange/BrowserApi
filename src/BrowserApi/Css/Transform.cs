using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS transform value composed of one or more transform functions
/// (e.g., <c>translate(10px, 20px)</c>, <c>rotate(45deg) scale(1.5)</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Transform"/> provides static factory methods for all standard 2D CSS transform
/// functions: <see cref="Translate"/>, <see cref="Rotate"/>, <see cref="Scale"/>,
/// <see cref="SkewX"/>, <see cref="SkewY"/>, <see cref="Skew"/>, and <see cref="Matrix"/>.
/// </para>
/// <para>
/// Multiple transforms can be chained together using the fluent <see cref="Then"/> method
/// (or its convenience overloads like <see cref="ThenRotate"/>, <see cref="ThenScale"/>, etc.),
/// which concatenates transform functions with a space separator as required by CSS.
/// </para>
/// <para>
/// Unlike other CSS value types in this library, <see cref="Transform"/> is fully self-contained
/// (not a partial struct extended from a generated counterpart).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Single transform
/// Transform t = Transform.Rotate(Angle.Deg(45));
/// t.ToCss(); // "rotate(45deg)"
///
/// // Chained transforms
/// Transform chained = Transform.Translate(Length.Px(10), Length.Px(20))
///     .ThenRotate(Angle.Deg(45))
///     .ThenScale(1.5);
/// chained.ToCss(); // "translate(10px, 20px) rotate(45deg) scale(1.5)"
///
/// // No transform
/// Transform none = Transform.None;
/// none.ToCss(); // "none"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Angle"/>
/// <seealso cref="Length"/>
public readonly partial struct Transform : ICssValue, IEquatable<Transform> {
    private readonly string _value;

    /// <summary>
    /// Initializes a new <see cref="Transform"/> with a raw CSS transform string.
    /// </summary>
    /// <param name="value">The CSS transform string (e.g., <c>"rotate(45deg)"</c>).</param>
    public Transform(string value) => _value = value;

    /// <summary>
    /// Serializes this transform to its CSS string representation.
    /// </summary>
    /// <returns>The CSS transform string.</returns>
    public string ToCss() => _value;

    /// <inheritdoc />
    public override string ToString() => _value;

    // Sentinel

    /// <summary>Gets a <see cref="Transform"/> representing the CSS <c>none</c> keyword, which applies no transform.</summary>
    public static Transform None { get; } = new("none");

    // Static factories

    /// <summary>
    /// Creates a <c>translate(x, y)</c> transform that moves an element along both axes.
    /// </summary>
    /// <param name="x">The horizontal translation distance.</param>
    /// <param name="y">The vertical translation distance.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>translate({x}, {y})</c>.</returns>
    public static Transform Translate(Length x, Length y) =>
        new($"translate({x.ToCss()}, {y.ToCss()})");

    /// <summary>
    /// Creates a <c>translateX(x)</c> transform that moves an element horizontally.
    /// </summary>
    /// <param name="x">The horizontal translation distance.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>translateX({x})</c>.</returns>
    public static Transform TranslateX(Length x) =>
        new($"translateX({x.ToCss()})");

    /// <summary>
    /// Creates a <c>translateY(y)</c> transform that moves an element vertically.
    /// </summary>
    /// <param name="y">The vertical translation distance.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>translateY({y})</c>.</returns>
    public static Transform TranslateY(Length y) =>
        new($"translateY({y.ToCss()})");

    /// <summary>
    /// Creates a <c>rotate(angle)</c> transform that rotates an element around its origin.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>rotate({angle})</c>.</returns>
    /// <example>
    /// <code>
    /// Transform.Rotate(Angle.Deg(90)).ToCss(); // "rotate(90deg)"
    /// </code>
    /// </example>
    public static Transform Rotate(Angle angle) =>
        new($"rotate({angle.ToCss()})");

    /// <summary>
    /// Creates a <c>scale(factor)</c> transform that uniformly scales an element along both axes.
    /// </summary>
    /// <param name="factor">The uniform scale factor (e.g., 2.0 doubles the size, 0.5 halves it).</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>scale({factor})</c>.</returns>
    public static Transform Scale(double factor) =>
        new($"scale({FormatNumber(factor)})");

    /// <summary>
    /// Creates a <c>scale(x, y)</c> transform that scales an element independently along each axis.
    /// </summary>
    /// <param name="x">The horizontal scale factor.</param>
    /// <param name="y">The vertical scale factor.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>scale({x}, {y})</c>.</returns>
    public static Transform Scale(double x, double y) =>
        new($"scale({FormatNumber(x)}, {FormatNumber(y)})");

    /// <summary>
    /// Creates a <c>scaleX(x)</c> transform that scales an element horizontally.
    /// </summary>
    /// <param name="x">The horizontal scale factor.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>scaleX({x})</c>.</returns>
    public static Transform ScaleX(double x) =>
        new($"scaleX({FormatNumber(x)})");

    /// <summary>
    /// Creates a <c>scaleY(y)</c> transform that scales an element vertically.
    /// </summary>
    /// <param name="y">The vertical scale factor.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>scaleY({y})</c>.</returns>
    public static Transform ScaleY(double y) =>
        new($"scaleY({FormatNumber(y)})");

    /// <summary>
    /// Creates a <c>skewX(angle)</c> transform that skews an element along the X axis.
    /// </summary>
    /// <param name="angle">The skew angle.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>skewX({angle})</c>.</returns>
    public static Transform SkewX(Angle angle) =>
        new($"skewX({angle.ToCss()})");

    /// <summary>
    /// Creates a <c>skewY(angle)</c> transform that skews an element along the Y axis.
    /// </summary>
    /// <param name="angle">The skew angle.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>skewY({angle})</c>.</returns>
    public static Transform SkewY(Angle angle) =>
        new($"skewY({angle.ToCss()})");

    /// <summary>
    /// Creates a <c>skew(x, y)</c> transform that skews an element along both axes.
    /// </summary>
    /// <param name="x">The skew angle along the X axis.</param>
    /// <param name="y">The skew angle along the Y axis.</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>skew({x}, {y})</c>.</returns>
    public static Transform Skew(Angle x, Angle y) =>
        new($"skew({x.ToCss()}, {y.ToCss()})");

    /// <summary>
    /// Creates a <c>matrix(a, b, c, d, e, f)</c> transform using a 2D transformation matrix.
    /// </summary>
    /// <param name="a">The value at position (1,1) in the matrix (horizontal scaling).</param>
    /// <param name="b">The value at position (1,2) in the matrix (vertical skewing).</param>
    /// <param name="c">The value at position (2,1) in the matrix (horizontal skewing).</param>
    /// <param name="d">The value at position (2,2) in the matrix (vertical scaling).</param>
    /// <param name="e">The value at position (3,1) in the matrix (horizontal translation).</param>
    /// <param name="f">The value at position (3,2) in the matrix (vertical translation).</param>
    /// <returns>A <see cref="Transform"/> that serializes to <c>matrix(a, b, c, d, e, f)</c>.</returns>
    public static Transform Matrix(double a, double b, double c, double d, double e, double f) =>
        new($"matrix({FormatNumber(a)}, {FormatNumber(b)}, {FormatNumber(c)}, {FormatNumber(d)}, {FormatNumber(e)}, {FormatNumber(f)})");

    // Chaining

    /// <summary>
    /// Appends another transform function to this transform, creating a space-separated
    /// list of transform functions as required by the CSS <c>transform</c> property.
    /// </summary>
    /// <param name="other">The transform to append.</param>
    /// <returns>A new <see cref="Transform"/> containing both transforms separated by a space.</returns>
    /// <remarks>
    /// CSS applies transforms in the order they are listed, from right to left.
    /// Use this method or its convenience overloads to build multi-step transforms fluently.
    /// </remarks>
    /// <example>
    /// <code>
    /// Transform.Translate(Length.Px(10), Length.Px(0))
    ///     .Then(Transform.Rotate(Angle.Deg(45)))
    ///     .ToCss(); // "translate(10px, 0) rotate(45deg)"
    /// </code>
    /// </example>
    public Transform Then(Transform other) => new($"{_value} {other._value}");

    /// <summary>
    /// Appends a <c>translate(x, y)</c> transform to this transform.
    /// </summary>
    /// <param name="x">The horizontal translation distance.</param>
    /// <param name="y">The vertical translation distance.</param>
    /// <returns>A new <see cref="Transform"/> with the translation appended.</returns>
    public Transform ThenTranslate(Length x, Length y) => Then(Translate(x, y));

    /// <summary>
    /// Appends a <c>rotate(angle)</c> transform to this transform.
    /// </summary>
    /// <param name="angle">The rotation angle.</param>
    /// <returns>A new <see cref="Transform"/> with the rotation appended.</returns>
    public Transform ThenRotate(Angle angle) => Then(Rotate(angle));

    /// <summary>
    /// Appends a uniform <c>scale(factor)</c> transform to this transform.
    /// </summary>
    /// <param name="factor">The uniform scale factor.</param>
    /// <returns>A new <see cref="Transform"/> with the scaling appended.</returns>
    public Transform ThenScale(double factor) => Then(Scale(factor));

    /// <summary>
    /// Appends a <c>scale(x, y)</c> transform to this transform.
    /// </summary>
    /// <param name="x">The horizontal scale factor.</param>
    /// <param name="y">The vertical scale factor.</param>
    /// <returns>A new <see cref="Transform"/> with the scaling appended.</returns>
    public Transform ThenScale(double x, double y) => Then(Scale(x, y));

    /// <summary>
    /// Appends a <c>skewX(angle)</c> transform to this transform.
    /// </summary>
    /// <param name="angle">The skew angle along the X axis.</param>
    /// <returns>A new <see cref="Transform"/> with the skew appended.</returns>
    public Transform ThenSkewX(Angle angle) => Then(SkewX(angle));

    /// <summary>
    /// Appends a <c>skewY(angle)</c> transform to this transform.
    /// </summary>
    /// <param name="angle">The skew angle along the Y axis.</param>
    /// <returns>A new <see cref="Transform"/> with the skew appended.</returns>
    public Transform ThenSkewY(Angle angle) => Then(SkewY(angle));

    // Equality

    /// <inheritdoc />
    public bool Equals(Transform other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Transform other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Transform"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Transform left, Transform right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Transform"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Transform left, Transform right) => !left.Equals(right);
}
