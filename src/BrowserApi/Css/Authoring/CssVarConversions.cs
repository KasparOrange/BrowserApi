namespace BrowserApi.Css;

/// <summary>
/// Implicit conversions from <see cref="Authoring.CssVar{T}"/> to the corresponding
/// CSS primitive types. These let users assign a variable directly to a typed
/// declaration setter — e.g. <c>BorderRadius = Tokens.Radius</c> — without an
/// explicit unwrap step. The emitted CSS is <c>var(--name)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Per spec §16, a <see cref="Authoring.CssVar{T}"/> "is" a value of <c>T</c> for
/// usage purposes — referencing a variable in a declaration position emits
/// <c>var(--name)</c>. The conversions live here, in <c>BrowserApi.Css</c>, so
/// they're discovered as conversion candidates by the C# compiler when the
/// destination type is one of these primitives.
/// </para>
/// <para>
/// The conversions construct a fresh primitive whose internal CSS string is
/// <c>var(--name)</c>. Per spec §29, every value carries an <c>IsVariable</c>
/// flag — these conversions effectively flip that flag from the variable's
/// reference. Once full taint propagation lands, downstream calc/color operations
/// will route through the CSS branch automatically.
/// </para>
/// </remarks>
public readonly partial struct Length {
    /// <summary>Implicitly converts a <see cref="Authoring.CssVar{Length}"/> reference
    /// into a <see cref="Length"/> whose CSS form is <c>var(--name)</c>.</summary>
    public static implicit operator Length(Authoring.CssVar<Length> variable)
        => new Length(variable.ToCss());
}

/// <inheritdoc cref="Length"/>
public readonly partial struct CssColor {
    /// <summary>Implicitly converts a <see cref="Authoring.CssVar{CssColor}"/> reference
    /// into a <see cref="CssColor"/> whose CSS form is <c>var(--name)</c>.</summary>
    public static implicit operator CssColor(Authoring.CssVar<CssColor> variable)
        => new CssColor(variable.ToCss());
}

/// <inheritdoc cref="Length"/>
public readonly partial struct Percentage {
    /// <summary>Implicitly converts a <see cref="Authoring.CssVar{Percentage}"/> reference
    /// into a <see cref="Percentage"/> whose CSS form is <c>var(--name)</c>.</summary>
    public static implicit operator Percentage(Authoring.CssVar<Percentage> variable)
        => new Percentage(variable.ToCss());
}
