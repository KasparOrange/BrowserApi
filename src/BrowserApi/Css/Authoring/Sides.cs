using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Four-sides CSS shorthand for properties like <c>padding</c>, <c>margin</c>,
/// <c>border-width</c>, and <c>inset</c>. Implicitly converts from a single
/// <see cref="Length"/> (all sides), a 2-tuple (vertical / horizontal), or a
/// 4-tuple (top / right / bottom / left).
/// </summary>
/// <remarks>
/// <para>
/// The 3-value form (<c>padding: 10px 20px 30px</c>) is deliberately NOT supported —
/// it puts "horizontal" in the middle position which is confusing to read and
/// equally easy to express via the 4-tuple form (spec §18, §26).
/// </para>
/// <para>
/// For the 4-tuple form, the <see cref="Css.Sides(Length, Length, Length, Length)"/>
/// factory takes named arguments (<c>top:</c>, <c>right:</c>, …) so source reads
/// unambiguously. A future analyzer (BCA001) will warn on unnamed 4-tuples.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Padding = 10.Px(),                                      // → "10px"
/// Padding = (10.Px(), 20.Px()),                            // → "10px 20px"
/// Padding = (top: 10.Px(), right: 20.Px(), bottom: 30.Px(), left: 40.Px()),
///                                                          // → "10px 20px 30px 40px"
/// Padding = Css.Sides(top: 10.Px(), right: 20.Px(), bottom: 30.Px(), left: 40.Px()),
/// </code>
/// </example>
public readonly struct Sides : ICssValue {
    private readonly string _css;
    private Sides(string css) { _css = css; }

    /// <inheritdoc/>
    public string ToCss() => _css;

    /// <summary>One length applied to all four sides.</summary>
    public static implicit operator Sides(Length all) => new(all.ToCss());

    /// <summary>One percentage applied to all four sides — CSS allows
    /// <c>padding: 5%</c> for example.</summary>
    public static implicit operator Sides(Percentage all) => new(all.ToCss());

    /// <summary>Length-or-percentage applied to all four sides.</summary>
    public static implicit operator Sides(LengthOrPercentage all) => new(all.ToCss());

    /// <summary>Two-length tuple — first is vertical (top/bottom), second is
    /// horizontal (left/right). Matches CSS shorthand semantics.</summary>
    public static implicit operator Sides((Length vertical, Length horizontal) pair) =>
        new($"{pair.vertical.ToCss()} {pair.horizontal.ToCss()}");

    /// <summary>Two length-or-percentage tuple (vertical, horizontal).</summary>
    public static implicit operator Sides((LengthOrPercentage vertical, LengthOrPercentage horizontal) pair) =>
        new($"{pair.vertical.ToCss()} {pair.horizontal.ToCss()}");

    /// <summary>Four-length tuple — explicit top, right, bottom, left.</summary>
    public static implicit operator Sides((Length top, Length right, Length bottom, Length left) quad) =>
        new($"{quad.top.ToCss()} {quad.right.ToCss()} {quad.bottom.ToCss()} {quad.left.ToCss()}");

    /// <summary>A <see cref="CssVar{Length}"/> reference (e.g. design-token spacing)
    /// implicitly becomes a Sides value applied to all four sides — emits as
    /// <c>var(--spacing)</c>.</summary>
    public static implicit operator Sides(CssVar<Length> variable) =>
        new(variable.ToCss());

    /// <summary>A <see cref="CssVar{Percentage}"/> reference applied to all four sides.</summary>
    public static implicit operator Sides(CssVar<Percentage> variable) =>
        new(variable.ToCss());

    // ─────────────────────────────────── Factories ──────────────────────────────────

    /// <summary>Builds a four-sides shorthand value with named arguments — the
    /// analyzer-recommended way to write four explicit sides (resolves spec §18
    /// BCA001 by construction).</summary>
    public static Sides Of(Length top, Length right, Length bottom, Length left)
        => (top, right, bottom, left);

    /// <summary>Builds a two-axis shorthand value with named arguments.</summary>
    public static Sides Of(Length vertical, Length horizontal)
        => (vertical, horizontal);

    /// <summary>Returns the CSS string. Used for debugging.</summary>
    public override string ToString() => _css;
}

/// <summary>
/// Wraps a raw CSS string as an <see cref="ICssValue"/>. Use sparingly via
/// <see cref="Css.Raw(string)"/> when no typed entry exists for what you need.
/// </summary>
public readonly struct RawValue : ICssValue {
    private readonly string _raw;
    /// <summary>Wraps the supplied string verbatim.</summary>
    public RawValue(string raw) { _raw = raw; }
    /// <inheritdoc/>
    public string ToCss() => _raw;
}
