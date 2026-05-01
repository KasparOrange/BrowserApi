namespace BrowserApi.Css;

/// <summary>
/// Color-manipulation methods for the CSS-in-C# authoring API. Each method
/// emits a CSS expression using <a href="https://www.w3.org/TR/css-color-5/#relative-colors">CSS
/// relative color syntax</a> or <c>color-mix()</c> — both well-supported in
/// every modern browser. The same path works for literal colors AND for
/// custom-property references, so spec §29's auto-dispatch is automatic
/// here: if you give it <c>var(--primary)</c> the relative-color expression
/// resolves at the browser; if you give it <c>#3498db</c> the same expression
/// resolves to a fixed value.
/// </summary>
/// <remarks>
/// <para>
/// The spec §29 distinguishes "SCSS path" (literal colors → cleaner static
/// output) from "CSS path" (variable colors → relative-color syntax). This
/// implementation uses the CSS path for everything because the runtime
/// pipeline has no sass step. When the source-generator path lands and adds
/// a sass intermediate, we can switch literal-only inputs to
/// <c>lighten(#3498db, 20%)</c> for cleaner output. The user-visible API
/// signature does not change between the two paths.
/// </para>
/// </remarks>
public readonly partial struct CssColor {
    /// <summary>Lighten the color by <paramref name="percent"/> percentage points.</summary>
    /// <param name="percent">Percentage of lightness to add (0..100).</param>
    public CssColor Lighten(double percent)
        => new($"hsl(from {ToCss()} h s calc(l + {Fmt(percent)}%))");

    /// <summary>Darken the color by <paramref name="percent"/> percentage points.</summary>
    public CssColor Darken(double percent)
        => new($"hsl(from {ToCss()} h s calc(l - {Fmt(percent)}%))");

    /// <summary>Increase saturation by <paramref name="percent"/> percentage points.</summary>
    public CssColor Saturate(double percent)
        => new($"hsl(from {ToCss()} h calc(s + {Fmt(percent)}%) l)");

    /// <summary>Decrease saturation by <paramref name="percent"/> percentage points.</summary>
    public CssColor Desaturate(double percent)
        => new($"hsl(from {ToCss()} h calc(s - {Fmt(percent)}%) l)");

    /// <summary>Rotate the hue by <paramref name="degrees"/>.</summary>
    public CssColor AdjustHue(double degrees)
        => new($"hsl(from {ToCss()} calc(h + {Fmt(degrees)}deg) s l)");

    /// <summary>Returns the complementary color (hue rotated 180°).</summary>
    public CssColor Complement
        => new($"hsl(from {ToCss()} calc(h + 180deg) s l)");

    /// <summary>Strips saturation entirely — returns the grayscale equivalent.</summary>
    public CssColor Grayscale
        => new($"hsl(from {ToCss()} h 0% l)");

    /// <summary>Inverts the color — flips hue 180° and inverts lightness.</summary>
    public CssColor Invert
        => new($"hsl(from {ToCss()} calc(h + 180deg) s calc(100% - l))");

    /// <summary>Returns the color with the specified alpha (0..1).</summary>
    /// <param name="alpha">Opacity in the 0..1 range.</param>
    public CssColor WithAlpha(double alpha)
        => new($"hsl(from {ToCss()} h s l / {Fmt(alpha)})");

    /// <summary>Mixes this color with <paramref name="other"/> using
    /// <c>color-mix()</c> — <paramref name="weight"/> is the percentage of
    /// THIS color in the mix.</summary>
    /// <param name="other">The color to blend with.</param>
    /// <param name="weight">This color's weight in the mix (0..100).</param>
    public CssColor Mix(CssColor other, double weight)
        => new($"color-mix(in srgb, {ToCss()} {Fmt(weight)}%, {other.ToCss()})");

    private static string Fmt(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
