namespace BrowserApi.Css;

/// <summary>
/// Container-query length factories on <see cref="Length"/>. Mirror of the
/// existing <see cref="Length.Px(double)"/>/<see cref="Length.Em(double)"/>
/// pattern for <c>cqw</c>/<c>cqh</c>/<c>cqi</c>/<c>cqb</c>/<c>cqmin</c>/<c>cqmax</c>.
/// The numeric extension-property surface (<c>50.Cqw</c>) lives in
/// <see cref="CssUnitExtensions"/> alongside the other unit shorthands;
/// this file just provides the static factory methods those properties
/// delegate to.
/// </summary>
/// <remarks>
/// Spec §32. Container units resolve against the nearest containing element
/// with a <c>container-type</c> declaration; without one they fall back to
/// the small viewport equivalents.
/// </remarks>
public readonly partial struct Length {
    /// <summary>1% of the containing query container's width (<c>cqw</c>).</summary>
    public static Length Cqw(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqw");

    /// <summary>1% of the containing query container's height (<c>cqh</c>).</summary>
    public static Length Cqh(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqh");

    /// <summary>1% of the containing query container's inline size (<c>cqi</c>).</summary>
    public static Length Cqi(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqi");

    /// <summary>1% of the containing query container's block size (<c>cqb</c>).</summary>
    public static Length Cqb(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqb");

    /// <summary>1% of the smaller of <c>cqi</c> and <c>cqb</c> (<c>cqmin</c>).</summary>
    public static Length Cqmin(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqmin");

    /// <summary>1% of the larger of <c>cqi</c> and <c>cqb</c> (<c>cqmax</c>).</summary>
    public static Length Cqmax(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqmax");
}
