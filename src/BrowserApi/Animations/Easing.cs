using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Animations;

/// <summary>
/// Provides CSS easing function constants and factory methods for use with the Web Animations API.
/// </summary>
/// <remarks>
/// <para>
/// This class contains two kinds of members:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Named easings</b> -- the five CSS keyword easings (<see cref="Linear"/>,
///       <see cref="Ease"/>, <see cref="EaseIn"/>, <see cref="EaseOut"/>, <see cref="EaseInOut"/>).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Common curves</b> -- pre-defined <c>cubic-bezier</c> values for popular easing
///       functions (sine, quad, cubic variants), matching the curves from easings.net.
///     </description>
///   </item>
/// </list>
/// <para>
/// For custom easing curves, use <see cref="CubicBezier"/> or <see cref="Steps"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use a named easing
/// var options = new AnimationOptionsBuilder()
///     .Duration(500)
///     .Easing(Easing.EaseInOut);
///
/// // Use a custom cubic-bezier
/// var bounce = Easing.CubicBezier(0.68, -0.55, 0.27, 1.55);
///
/// // Use steps for a sprite animation
/// var sprite = Easing.Steps(8, "end");
/// </code>
/// </example>
/// <seealso cref="AnimationOptionsBuilder"/>
/// <seealso cref="KeyframeBuilder"/>
public static class Easing {
    // ── Named easings ───────────────────────────────────────────────────

    /// <summary>
    /// A linear easing with no acceleration or deceleration. Equivalent to <c>cubic-bezier(0, 0, 1, 1)</c>.
    /// </summary>
    public static string Linear => "linear";

    /// <summary>
    /// The default CSS easing function with a slow start, fast middle, and slow end.
    /// Equivalent to <c>cubic-bezier(0.25, 0.1, 0.25, 1)</c>.
    /// </summary>
    public static string Ease => "ease";

    /// <summary>
    /// An easing function with a slow start. Equivalent to <c>cubic-bezier(0.42, 0, 1, 1)</c>.
    /// </summary>
    public static string EaseIn => "ease-in";

    /// <summary>
    /// An easing function with a slow end. Equivalent to <c>cubic-bezier(0, 0, 0.58, 1)</c>.
    /// </summary>
    public static string EaseOut => "ease-out";

    /// <summary>
    /// An easing function with both a slow start and slow end.
    /// Equivalent to <c>cubic-bezier(0.42, 0, 0.58, 1)</c>.
    /// </summary>
    public static string EaseInOut => "ease-in-out";

    // ── Factories ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a custom <c>cubic-bezier</c> easing function string.
    /// </summary>
    /// <param name="x1">The x-coordinate of the first control point (0 to 1).</param>
    /// <param name="y1">The y-coordinate of the first control point (may exceed 0-1 for overshoot).</param>
    /// <param name="x2">The x-coordinate of the second control point (0 to 1).</param>
    /// <param name="y2">The y-coordinate of the second control point (may exceed 0-1 for overshoot).</param>
    /// <returns>A CSS <c>cubic-bezier(...)</c> string.</returns>
    /// <example>
    /// <code>
    /// // Bouncy easing that overshoots then settles
    /// var bouncy = Easing.CubicBezier(0.68, -0.55, 0.27, 1.55);
    /// </code>
    /// </example>
    public static string CubicBezier(double x1, double y1, double x2, double y2) =>
        $"cubic-bezier({FormatNumber(x1)}, {FormatNumber(y1)}, {FormatNumber(x2)}, {FormatNumber(y2)})";

    /// <summary>
    /// Creates a <c>steps</c> easing function string for staircase-style animations.
    /// </summary>
    /// <param name="count">The number of equal-duration steps.</param>
    /// <param name="jumpTerm">
    /// Optional jump term (<c>"start"</c>, <c>"end"</c>, <c>"both"</c>, or <c>"none"</c>).
    /// When <see langword="null"/>, the browser default (<c>"end"</c>) is used.
    /// </param>
    /// <returns>A CSS <c>steps(...)</c> string.</returns>
    /// <example>
    /// <code>
    /// // 10-frame sprite sheet animation stepping at each frame boundary
    /// var spriteEasing = Easing.Steps(10, "end");
    /// </code>
    /// </example>
    public static string Steps(int count, string? jumpTerm = null) =>
        jumpTerm is null ? $"steps({count})" : $"steps({count}, {jumpTerm})";

    // ── Common curves (cubic-bezier values) ─────────────────────────────

    /// <summary>Sine ease-in curve: <c>cubic-bezier(0.12, 0, 0.39, 0)</c>.</summary>
    public static string EaseInSine => "cubic-bezier(0.12, 0, 0.39, 0)";

    /// <summary>Sine ease-out curve: <c>cubic-bezier(0.61, 1, 0.88, 1)</c>.</summary>
    public static string EaseOutSine => "cubic-bezier(0.61, 1, 0.88, 1)";

    /// <summary>Sine ease-in-out curve: <c>cubic-bezier(0.37, 0, 0.63, 1)</c>.</summary>
    public static string EaseInOutSine => "cubic-bezier(0.37, 0, 0.63, 1)";

    /// <summary>Quadratic ease-in curve: <c>cubic-bezier(0.11, 0, 0.5, 0)</c>.</summary>
    public static string EaseInQuad => "cubic-bezier(0.11, 0, 0.5, 0)";

    /// <summary>Quadratic ease-out curve: <c>cubic-bezier(0.5, 1, 0.89, 1)</c>.</summary>
    public static string EaseOutQuad => "cubic-bezier(0.5, 1, 0.89, 1)";

    /// <summary>Quadratic ease-in-out curve: <c>cubic-bezier(0.45, 0, 0.55, 1)</c>.</summary>
    public static string EaseInOutQuad => "cubic-bezier(0.45, 0, 0.55, 1)";

    /// <summary>Cubic ease-in curve: <c>cubic-bezier(0.32, 0, 0.67, 0)</c>.</summary>
    public static string EaseInCubic => "cubic-bezier(0.32, 0, 0.67, 0)";

    /// <summary>Cubic ease-out curve: <c>cubic-bezier(0.33, 1, 0.68, 1)</c>.</summary>
    public static string EaseOutCubic => "cubic-bezier(0.33, 1, 0.68, 1)";

    /// <summary>Cubic ease-in-out curve: <c>cubic-bezier(0.65, 0, 0.35, 1)</c>.</summary>
    public static string EaseInOutCubic => "cubic-bezier(0.65, 0, 0.35, 1)";
}
