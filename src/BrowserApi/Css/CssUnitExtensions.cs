namespace BrowserApi.Css;

/// <summary>
/// Provides extension methods on numeric types (<see cref="int"/> and <see cref="double"/>)
/// for creating CSS value types with a fluent, unit-suffix syntax.
/// </summary>
/// <remarks>
/// <para>
/// These extensions allow you to write CSS values in a natural, readable style that mirrors
/// CSS syntax. For example, <c>16.Px()</c> instead of <c>Length.Px(16)</c>, or
/// <c>200.Ms()</c> instead of <c>Duration.Ms(200)</c>.
/// </para>
/// <para>
/// Each extension method delegates to the corresponding static factory method on the
/// appropriate CSS value type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Length extensions
/// Length margin = 16.Px();
/// Length fontSize = 1.5.Rem();
/// Length width = 100.0.Vw();
///
/// // Duration extensions
/// Duration fast = 200.Ms();
/// Duration slow = 0.5.S();
///
/// // Angle extensions
/// Angle rotation = 45.Deg();
///
/// // Percentage extensions
/// Percentage half = 50.Percent();
///
/// // Flex extensions
/// Flex column = 1.Fr();
/// </code>
/// </example>
/// <seealso cref="Length"/>
/// <seealso cref="Duration"/>
/// <seealso cref="Angle"/>
/// <seealso cref="Percentage"/>
/// <seealso cref="Flex"/>
public static class CssUnitExtensions {
    // Length

    /// <summary>
    /// Creates a <see cref="Length"/> in pixels from this integer value.
    /// </summary>
    /// <param name="value">The pixel value.</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Length.Px"/>.</returns>
    public static Length Px(this int value) => Length.Px(value);

    /// <summary>
    /// Creates a <see cref="Length"/> in pixels from this double value.
    /// </summary>
    /// <param name="value">The pixel value.</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Length.Px"/>.</returns>
    public static Length Px(this double value) => Length.Px(value);

    /// <summary>
    /// Creates a <see cref="Length"/> in em units from this double value.
    /// </summary>
    /// <param name="value">The em value, relative to the element's font size.</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Length.Em"/>.</returns>
    public static Length Em(this double value) => Length.Em(value);

    /// <summary>
    /// Creates a <see cref="Length"/> in root em units from this double value.
    /// </summary>
    /// <param name="value">The rem value, relative to the root element's font size.</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Length.Rem"/>.</returns>
    public static Length Rem(this double value) => Length.Rem(value);

    /// <summary>
    /// Creates a <see cref="Length"/> in viewport height units from this double value.
    /// </summary>
    /// <param name="value">The vh value (1vh = 1% of viewport height).</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Length.Vh"/>.</returns>
    public static Length Vh(this double value) => Length.Vh(value);

    /// <summary>
    /// Creates a <see cref="Length"/> in viewport width units from this double value.
    /// </summary>
    /// <param name="value">The vw value (1vw = 1% of viewport width).</param>
    /// <returns>A <see cref="Length"/> equivalent to <see cref="Length.Vw"/>.</returns>
    public static Length Vw(this double value) => Length.Vw(value);

    // Duration

    /// <summary>
    /// Creates a <see cref="Duration"/> in milliseconds from this integer value.
    /// </summary>
    /// <param name="value">The duration in milliseconds.</param>
    /// <returns>A <see cref="Duration"/> equivalent to <see cref="Duration.Ms"/>.</returns>
    public static Duration Ms(this int value) => Duration.Ms(value);

    /// <summary>
    /// Creates a <see cref="Duration"/> in milliseconds from this double value.
    /// </summary>
    /// <param name="value">The duration in milliseconds.</param>
    /// <returns>A <see cref="Duration"/> equivalent to <see cref="Duration.Ms"/>.</returns>
    public static Duration Ms(this double value) => Duration.Ms(value);

    /// <summary>
    /// Creates a <see cref="Duration"/> in seconds from this double value.
    /// </summary>
    /// <param name="value">The duration in seconds.</param>
    /// <returns>A <see cref="Duration"/> equivalent to <see cref="Duration.S"/>.</returns>
    public static Duration S(this double value) => Duration.S(value);

    // Angle

    /// <summary>
    /// Creates an <see cref="Angle"/> in degrees from this integer value.
    /// </summary>
    /// <param name="value">The angle in degrees.</param>
    /// <returns>An <see cref="Angle"/> equivalent to <see cref="Angle.Deg"/>.</returns>
    public static Angle Deg(this int value) => Angle.Deg(value);

    /// <summary>
    /// Creates an <see cref="Angle"/> in degrees from this double value.
    /// </summary>
    /// <param name="value">The angle in degrees.</param>
    /// <returns>An <see cref="Angle"/> equivalent to <see cref="Angle.Deg"/>.</returns>
    public static Angle Deg(this double value) => Angle.Deg(value);

    // Percentage

    /// <summary>
    /// Creates a <see cref="Percentage"/> from this integer value.
    /// </summary>
    /// <param name="value">The percentage value (e.g., 50 for 50%).</param>
    /// <returns>A <see cref="Percentage"/> equivalent to <see cref="Percentage.Of"/>.</returns>
    public static Percentage Percent(this int value) => Percentage.Of(value);

    /// <summary>
    /// Creates a <see cref="Percentage"/> from this double value.
    /// </summary>
    /// <param name="value">The percentage value (e.g., 33.3 for 33.3%).</param>
    /// <returns>A <see cref="Percentage"/> equivalent to <see cref="Percentage.Of"/>.</returns>
    public static Percentage Percent(this double value) => Percentage.Of(value);

    // Flex

    /// <summary>
    /// Creates a <see cref="Flex"/> in fractional units from this integer value.
    /// </summary>
    /// <param name="value">The fractional unit value for CSS Grid layouts.</param>
    /// <returns>A <see cref="Flex"/> equivalent to <see cref="Flex.Fr"/>.</returns>
    public static Flex Fr(this int value) => Flex.Fr(value);

    /// <summary>
    /// Creates a <see cref="Flex"/> in fractional units from this double value.
    /// </summary>
    /// <param name="value">The fractional unit value for CSS Grid layouts.</param>
    /// <returns>A <see cref="Flex"/> equivalent to <see cref="Flex.Fr"/>.</returns>
    public static Flex Fr(this double value) => Flex.Fr(value);
}
