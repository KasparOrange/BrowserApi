namespace BrowserApi.Css;

/// <summary>
/// CSS function-style constructors for <see cref="Length"/> values:
/// <c>clamp()</c>, <c>min()</c>, <c>max()</c>, <c>fit-content()</c>.
/// All produce a <see cref="Length"/> whose <c>ToCss()</c> emits the
/// corresponding CSS function call.
/// </summary>
/// <remarks>
/// Spec §17. <c>calc()</c> arithmetic is already covered by the
/// <c>+</c> / <c>-</c> operator overloads on <see cref="Length"/> in
/// <see cref="Length.Calc(string)"/>; these functions cover the multi-argument
/// CSS functions that don't fit operator syntax.
/// </remarks>
public readonly partial struct Length {
    /// <summary>Builds a CSS <c>clamp(min, preferred, max)</c> expression.</summary>
    /// <param name="min">Lower bound.</param>
    /// <param name="preferred">Preferred (center) value.</param>
    /// <param name="max">Upper bound.</param>
    public static Length Clamp(Length min, Length preferred, Length max)
        => new($"clamp({min.ToCss()}, {preferred.ToCss()}, {max.ToCss()})");

    /// <summary>Builds a CSS <c>min(a, b)</c> expression — picks the smaller of the two.</summary>
    public static Length Min(Length a, Length b)
        => new($"min({a.ToCss()}, {b.ToCss()})");

    /// <summary>Builds a CSS <c>min(...)</c> expression with N arguments.</summary>
    public static Length Min(params Length[] values)
        => new($"min({string.Join(", ", System.Array.ConvertAll(values, v => v.ToCss()))})");

    /// <summary>Builds a CSS <c>max(a, b)</c> expression — picks the larger of the two.</summary>
    public static Length Max(Length a, Length b)
        => new($"max({a.ToCss()}, {b.ToCss()})");

    /// <summary>Builds a CSS <c>max(...)</c> expression with N arguments.</summary>
    public static Length Max(params Length[] values)
        => new($"max({string.Join(", ", System.Array.ConvertAll(values, v => v.ToCss()))})");

    /// <summary>The CSS <c>fit-content</c> keyword without an argument.</summary>
    public static Length FitContent { get; } = new("fit-content");

    /// <summary>Builds a CSS <c>fit-content(max)</c> expression — fits content
    /// up to the given maximum.</summary>
    public static Length FitContentLimit(Length max)
        => new($"fit-content({max.ToCss()})");

    /// <summary>The CSS <c>min-content</c> keyword.</summary>
    public static Length MinContent { get; } = new("min-content");

    /// <summary>The CSS <c>max-content</c> keyword.</summary>
    public static Length MaxContent { get; } = new("max-content");
}
