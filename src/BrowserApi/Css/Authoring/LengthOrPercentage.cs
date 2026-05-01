using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Spec §17 primitive union wrapper. A small struct that <see cref="Length"/>
/// and <see cref="Percentage"/> implicitly convert TO, so properties and
/// functions that genuinely accept both kinds can take a single typed
/// parameter while properties that don't (like <c>FontWeight</c>) still
/// reject percentages at compile time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Length"/> and <see cref="Percentage"/> deliberately do NOT
/// implicitly convert to each other — that would let
/// <c>FontWeight = 50.Percent()</c> compile, which is invalid CSS. Instead,
/// they each implicitly convert to <see cref="LengthOrPercentage"/>, and
/// only properties whose CSS grammar accepts both expose
/// <see cref="LengthOrPercentage"/> in their setter.
/// </para>
/// </remarks>
public readonly struct LengthOrPercentage : ICssValue {
    private readonly string _css;

    /// <summary>Wraps a pre-rendered CSS string.</summary>
    public LengthOrPercentage(string css) { _css = css; }

    /// <inheritdoc/>
    public string ToCss() => _css;

    /// <summary>Length → LengthOrPercentage.</summary>
    public static implicit operator LengthOrPercentage(Length length) => new(length.ToCss());

    /// <summary>Percentage → LengthOrPercentage.</summary>
    public static implicit operator LengthOrPercentage(Percentage pct) => new(pct.ToCss());

    /// <summary>CssVar&lt;Length&gt; → LengthOrPercentage (var(--name) is length-or-percentage).</summary>
    public static implicit operator LengthOrPercentage(CssVar<Length> v) => new(v.ToCss());

    /// <summary>CssVar&lt;Percentage&gt; → LengthOrPercentage.</summary>
    public static implicit operator LengthOrPercentage(CssVar<Percentage> v) => new(v.ToCss());
}
