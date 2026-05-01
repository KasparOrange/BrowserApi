using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A CSS custom property — what most developers call a "CSS variable" — typed by
/// the value type it holds. Created via <c>new CssVar&lt;T&gt;(default)</c> or
/// <c>CssVar.External(name)</c>; referenced anywhere a value of <typeparamref name="T"/>
/// is expected (implicit conversion emits <c>var(--name)</c>).
/// </summary>
/// <remarks>
/// <para>
/// "Variable" matches developer mental models — see philosophy point §9 in the spec.
/// W3C calls this a "custom property"; we keep "property" reserved for regular CSS
/// properties (<c>color</c>, <c>display</c>) on the <see cref="Declarations"/> type.
/// </para>
/// <para>
/// In the source-gen path, a <see cref="CssVar{T}"/> field's name is derived from the
/// C# field identifier (PascalCase → kebab-case) and emitted as <c>--name</c> on the
/// stylesheet's chosen declaration scope (typically <c>:root</c>). The MVP ships the
/// type and the implicit conversion; default-emission and conditional overrides are
/// added when the source generator lands.
/// </para>
/// </remarks>
/// <typeparam name="T">The value type carried — typically <c>Length</c>, <c>CssColor</c>,
/// <c>Percentage</c>, <c>Angle</c>, etc.</typeparam>
/// <example>
/// <code>
/// public static partial class Tokens : StyleSheet {
///     public static readonly CssVar&lt;Length&gt; Radius = new(8.Px());
///     public static readonly CssVar&lt;CssColor&gt; Bg = new(CssColor.White);
/// }
///
/// // Used inside another stylesheet — emits var(--radius) etc.:
/// public static readonly Class Card = new() {
///     BorderRadius = Tokens.Radius,
///     Background = Tokens.Bg,
/// };
/// </code>
/// </example>
public sealed class CssVar<T> : ICssValue where T : ICssValue {
    /// <summary>The default value, emitted on the stylesheet's <c>:root</c>
    /// declaration block (or whichever scope the stylesheet chooses).</summary>
    public T DefaultValue { get; }

    /// <summary>The CSS variable name including the leading <c>--</c>
    /// (e.g. <c>"--radius"</c>). Set by the source generator from the C# field name,
    /// or supplied explicitly via <see cref="CssVar.External(string)"/>.</summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>Constructs a variable with a default value.</summary>
    /// <param name="defaultValue">Emitted in the stylesheet's <c>:root</c> block.</param>
    public CssVar(T defaultValue) {
        DefaultValue = defaultValue;
    }

    /// <summary>Variables are runtime-resolved by the browser — they carry through
    /// the <c>IsVariable</c>-style taint that the spec defines on
    /// <see cref="ICssValue"/> (§29). Referencing this variable means any expression
    /// it participates in must emit through the CSS branch, not SCSS.</summary>
    /// <returns><c>var(--{Name})</c>.</returns>
    /// <remarks>
    /// If <see cref="Name"/> hasn't been populated yet (e.g. another stylesheet
    /// references this variable before the variable's stylesheet has been
    /// rendered), <see cref="CssRegistry.EnsureScanned"/> is triggered to walk the
    /// AppDomain and populate names from field identifiers. Lazy and idempotent.
    /// </remarks>
    public string ToCss() {
        if (string.IsNullOrEmpty(Name)) CssRegistry.EnsureScanned();
        return $"var({Name})";
    }
}

/// <summary>
/// Static entry points for <see cref="CssVar{T}"/> that don't fit on the generic type itself.
/// </summary>
public static class CssVar {
    /// <summary>
    /// Binds to a custom property defined outside this stylesheet — for example
    /// <c>--mud-palette-primary</c> from MudBlazor — by literal name. The auto-generated
    /// external-class hierarchy (e.g. <c>Mud.Palette.Primary</c>) is the preferred path;
    /// this is the escape hatch when the parser can't resolve the variable.
    /// </summary>
    /// <param name="name">The literal CSS custom-property name including <c>--</c>.</param>
    public static CssVar<T> External<T>(string name) where T : ICssValue, new() {
        return new CssVar<T>(default(T)!) { Name = name };
    }
}
