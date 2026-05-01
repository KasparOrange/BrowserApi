namespace BrowserApi.Css;

/// <summary>
/// Container query length unit constructors. These complement the existing
/// viewport units (Vh, Vw) and absolute units (Px, Em, Rem) in
/// <see cref="CssUnitExtensions"/> with the container-relative units
/// (<c>cqw</c>, <c>cqh</c>, <c>cqi</c>, <c>cqb</c>, <c>cqmin</c>, <c>cqmax</c>).
/// </summary>
/// <remarks>
/// Spec §32. Container units are evaluated against the nearest containing
/// element with a <c>container-type</c> declaration; without that, they
/// fall back to the small viewport equivalents.
/// </remarks>
public readonly partial struct Length {
    /// <summary>1% of the containing query container's width
    /// (<c>cqw</c>).</summary>
    public static Length Cqw(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqw");

    /// <summary>1% of the containing query container's height
    /// (<c>cqh</c>).</summary>
    public static Length Cqh(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqh");

    /// <summary>1% of the containing query container's inline size
    /// (<c>cqi</c>) — width in horizontal writing modes.</summary>
    public static Length Cqi(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqi");

    /// <summary>1% of the containing query container's block size
    /// (<c>cqb</c>) — height in horizontal writing modes.</summary>
    public static Length Cqb(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqb");

    /// <summary>1% of the smaller of <c>cqi</c> and <c>cqb</c>
    /// (<c>cqmin</c>).</summary>
    public static Length Cqmin(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqmin");

    /// <summary>1% of the larger of <c>cqi</c> and <c>cqb</c>
    /// (<c>cqmax</c>).</summary>
    public static Length Cqmax(double value)
        => new($"{System.FormattableString.Invariant($"{value}")}cqmax");
}

/// <summary>
/// Extension methods on numeric types for container query units, mirroring
/// the existing <see cref="CssUnitExtensions"/> pattern: <c>50.Cqw()</c>
/// instead of <c>Length.Cqw(50)</c>.
/// </summary>
public static class ContainerUnitExtensions {
    /// <summary>1% of the container's width as a <see cref="Length"/>.</summary>
    public static Length Cqw(this int value) => Length.Cqw(value);
    /// <summary>1% of the container's width as a <see cref="Length"/>.</summary>
    public static Length Cqw(this double value) => Length.Cqw(value);

    /// <summary>1% of the container's height as a <see cref="Length"/>.</summary>
    public static Length Cqh(this int value) => Length.Cqh(value);
    /// <summary>1% of the container's height as a <see cref="Length"/>.</summary>
    public static Length Cqh(this double value) => Length.Cqh(value);

    /// <summary>1% of the container's inline size as a <see cref="Length"/>.</summary>
    public static Length Cqi(this int value) => Length.Cqi(value);
    /// <summary>1% of the container's inline size as a <see cref="Length"/>.</summary>
    public static Length Cqi(this double value) => Length.Cqi(value);

    /// <summary>1% of the container's block size as a <see cref="Length"/>.</summary>
    public static Length Cqb(this int value) => Length.Cqb(value);
    /// <summary>1% of the container's block size as a <see cref="Length"/>.</summary>
    public static Length Cqb(this double value) => Length.Cqb(value);
}
