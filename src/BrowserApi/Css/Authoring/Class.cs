using System.Collections.Generic;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A CSS class — both a stylesheet rule (declarations attached) and a Razor identifier
/// (renders as a class name). The split between <see cref="Class"/> and <see cref="Rule"/>
/// is by usage in C#, not by CSS semantics: <see cref="Class"/> is the one referenced
/// from markup; <see cref="Rule"/> is the one only present in the stylesheet.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Class"/> instance carries the class's local name (derived from the
/// stylesheet's prefix conventions and the C# field name) plus the declarations
/// attached to it. Implicit conversion to <see cref="string"/> yields the bare class
/// name (e.g. <c>"card"</c>) suitable for <c>class="…"</c> attributes; the
/// <see cref="Selector"/> conversion yields the dotted form (e.g. <c>".card"</c>)
/// for use in CSS selector contexts.
/// </para>
/// <para>
/// The <c>+</c> operator composes classes into a <see cref="ClassList"/> for use in
/// Razor markup (<c>&lt;div class="@(Card + Active)"&gt;</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public static partial class AppStyles : StyleSheet {
///     public static readonly Class Card = new() {
///         // declarations populated via init-only properties on Declarations
///     };
/// }
///
/// // In Razor:
/// // &lt;div class="@AppStyles.Card"&gt;...&lt;/div&gt;       → class="card"
/// // &lt;div class="@(AppStyles.Card + AppStyles.Active)"&gt; → class="card active"
/// </code>
/// </example>
/// <seealso cref="Rule"/>
/// <seealso cref="ClassList"/>
/// <seealso cref="Declarations"/>
public sealed class Class : Declarations {
    /// <summary>
    /// The class's local name as it appears in the rendered CSS and HTML — for example
    /// <c>"card"</c> in <c>.card { ... }</c> and <c>class="card"</c>.
    /// </summary>
    /// <remarks>
    /// The runtime value is set by the source generator from the C# field name
    /// (PascalCase → kebab-case) plus any configured prefix. When a <see cref="Class"/>
    /// is constructed via <c>new()</c> in user code without source-gen post-processing,
    /// <see cref="Name"/> defaults to <see cref="string.Empty"/> until the field is
    /// scanned by <see cref="StyleSheet.Render(System.Type)"/>.
    /// </remarks>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// The CSS selector form of this class, with leading dot (e.g. <c>".card"</c>).
    /// Use this when composing selectors; the bare-name conversion is for HTML
    /// <c>class</c> attributes.
    /// </summary>
    public Selector Selector => new($".{Name}");

    /// <summary>An empty/sentinel <see cref="Class"/> used in conditional Razor expressions.</summary>
    public static Class None { get; } = new() { Name = string.Empty };

    /// <summary>
    /// Returns this class if <paramref name="condition"/> is <see langword="true"/>;
    /// otherwise returns <see cref="None"/>. Renders to the empty string when not
    /// applied — pairs cleanly with <c>@class="..."</c> attributes in Razor.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;div class="@(Card + Active.When(isActive))"&gt;
    /// </code>
    /// </example>
    public Class When(bool condition) => condition ? this : None;

    /// <summary>
    /// BEM-style modifier — produces a selector for <c>&amp;--{slug}</c> nesting.
    /// </summary>
    /// <param name="slug">The variant suffix without leading <c>--</c>.</param>
    public Selector Variant(string slug) => new($"{Selector.Css}--{slug}");

    /// <summary>Implicit conversion to the bare class name (e.g. <c>"card"</c>).</summary>
    /// <remarks>This is what's emitted in HTML <c>class="…"</c> attributes.</remarks>
    public static implicit operator string(Class c) => c?.Name ?? string.Empty;

    /// <summary>Implicit conversion to a CSS selector (e.g. <c>.card</c>).</summary>
    public static implicit operator Selector(Class c) => c.Selector;

    /// <summary>Composes two classes into a <see cref="ClassList"/> for Razor markup.</summary>
    public static ClassList operator +(Class a, Class b) => new ClassList().Add(a).Add(b);

    /// <summary>Composes a class with a raw class-name string. The string is appended
    /// verbatim — this is the documented escape hatch for framework classes that don't
    /// have a typed counterpart yet.</summary>
    public static ClassList operator +(Class a, string raw) => new ClassList().Add(a).Add(raw);

    /// <summary>Escape hatch for an external class name we don't have a typed binding for.
    /// The supplied string is treated as the literal class name with no prefix or transformation.</summary>
    /// <param name="name">The literal class name as it appears in the external CSS.</param>
    /// <remarks>Prefer typed bindings (the auto-generated <c>Mud.</c>, <c>Bs.</c>, etc.
    /// surfaces) when they exist. Use this only when no typed entry point covers the case.</remarks>
    public static Class External(string name) => new() { Name = name };
}
