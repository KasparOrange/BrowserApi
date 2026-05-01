using System;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Per-stylesheet class-name prefix. Combined with the global prefix configured
/// via <c>AddBrowserApiCss(opts =&gt; opts.GlobalPrefix = "...")</c> the final
/// class name is <c>{global}-{stylesheet-prefix}-{class-name}</c>.
/// </summary>
/// <remarks>
/// <para>
/// Prefixing isolates one stylesheet's classes from another's so naming a class
/// <c>Card</c> in two different feature areas doesn't collide. The renderer
/// applies the prefix transparently — Razor markup
/// <c>class="@ShiftPlannerStyles.PeopleList"</c> emits the fully-prefixed
/// class name automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Prefix("sp")]
/// public class ShiftPlannerStyles : StyleSheet {
///     public static readonly Class PeopleList = new() { ... };
///     // → ".mw-sp-people-list" with global prefix "mw"
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PrefixAttribute : Attribute {
    /// <summary>The per-stylesheet prefix (without trailing dash).</summary>
    public string Value { get; }

    /// <summary>Constructs the attribute with the supplied prefix value.</summary>
    /// <param name="value">The prefix (e.g. <c>"sp"</c>); must not contain spaces or dashes
    /// at the boundaries — the renderer adds the connecting dashes itself.</param>
    public PrefixAttribute(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Prefix value must be a non-empty identifier.", nameof(value));
        }
        Value = value;
    }
}
