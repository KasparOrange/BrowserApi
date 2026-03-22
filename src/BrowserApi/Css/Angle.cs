using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

/// <summary>
/// Represents a CSS angle value (e.g., <c>45deg</c>, <c>1.57rad</c>, <c>0.25turn</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Angle"/> is a hand-written extension of the generated partial struct that adds
/// factory methods for degrees, radians, gradians, and turns, plus a zero constant and
/// a <c>calc()</c> factory.
/// </para>
/// <para>
/// Angle values are used with CSS transforms (<c>rotate()</c>, <c>skew()</c>), gradients
/// (<c>linear-gradient()</c>, <c>conic-gradient()</c>), and other properties that accept
/// angular measurements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Angle degrees = Angle.Deg(45);      // "45deg"
/// Angle radians = Angle.Rad(3.14);    // "3.14rad"
/// Angle quarter = Angle.Turn(0.25);   // "0.25turn"
/// Angle gradians = Angle.Grad(100);   // "100grad"
/// </code>
/// </example>
/// <seealso cref="ICssValue"/>
/// <seealso cref="Transform"/>
/// <seealso cref="Gradient"/>
/// <seealso cref="CssUnitExtensions"/>
public readonly partial struct Angle : IEquatable<Angle> {
    /// <summary>Gets an <see cref="Angle"/> representing zero degrees (<c>0deg</c>).</summary>
    public static Angle Zero { get; } = new("0deg");

    /// <summary>
    /// Creates an angle in degrees (<c>deg</c>). A full circle is 360 degrees.
    /// </summary>
    /// <param name="value">The numeric value in degrees.</param>
    /// <returns>An <see cref="Angle"/> that serializes to <c>{value}deg</c>.</returns>
    /// <example>
    /// <code>
    /// Angle.Deg(90).ToCss(); // "90deg"
    /// </code>
    /// </example>
    public static Angle Deg(double value) => new($"{FormatNumber(value)}deg");

    /// <summary>
    /// Creates an angle in radians (<c>rad</c>). A full circle is approximately 6.2832 radians (2*pi).
    /// </summary>
    /// <param name="value">The numeric value in radians.</param>
    /// <returns>An <see cref="Angle"/> that serializes to <c>{value}rad</c>.</returns>
    public static Angle Rad(double value) => new($"{FormatNumber(value)}rad");

    /// <summary>
    /// Creates an angle in gradians (<c>grad</c>). A full circle is 400 gradians.
    /// </summary>
    /// <param name="value">The numeric value in gradians.</param>
    /// <returns>An <see cref="Angle"/> that serializes to <c>{value}grad</c>.</returns>
    public static Angle Grad(double value) => new($"{FormatNumber(value)}grad");

    /// <summary>
    /// Creates an angle in turns (<c>turn</c>). A full circle is 1 turn.
    /// </summary>
    /// <param name="value">The numeric value in turns (e.g., 0.5 = half turn = 180 degrees).</param>
    /// <returns>An <see cref="Angle"/> that serializes to <c>{value}turn</c>.</returns>
    /// <example>
    /// <code>
    /// Angle.Turn(0.25).ToCss(); // "0.25turn"
    /// </code>
    /// </example>
    public static Angle Turn(double value) => new($"{FormatNumber(value)}turn");

    /// <summary>
    /// Creates an angle using a CSS <c>calc()</c> expression.
    /// </summary>
    /// <param name="expression">The calc expression content (without the surrounding <c>calc()</c>).</param>
    /// <returns>An <see cref="Angle"/> that serializes to <c>calc({expression})</c>.</returns>
    public static Angle Calc(string expression) => new($"calc({expression})");

    /// <inheritdoc />
    public bool Equals(Angle other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Angle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>Determines whether two <see cref="Angle"/> values are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Angle left, Angle right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="Angle"/> values are not equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the two values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Angle left, Angle right) => !left.Equals(right);
}
