namespace BrowserApi.Css.Authoring;

/// <summary>
/// A CSS rule that is NOT referenced from Razor markup — purely an entry in the
/// stylesheet. Used for <c>:root</c> custom-property definitions, element resets,
/// complex multi-selector targets, and anywhere a class identifier would be the
/// wrong abstraction.
/// </summary>
/// <remarks>
/// <para>
/// The split between <see cref="Class"/> and <see cref="Rule"/> exists because
/// Razor markup needs a string from a class (<c>class="@Card"</c>), while a
/// <see cref="Rule"/> has no meaningful string representation — it's CSS-only.
/// In CSS itself, both compile to the same kind of declaration block; the
/// distinction is C#-side, by usage.
/// </para>
/// <para>
/// A <see cref="Rule"/> takes its selector via constructor (single, or many for a
/// selector list) and exposes the same nested-block and typed-property surface as
/// <see cref="Class"/> via the shared <see cref="Declarations"/> base.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public static readonly Rule ResetBody = new(El.Body) {
///     Margin = 0.Px(),
///     Padding = 0.Px(),
/// };
///
/// // Selector list — applies to all of them:
/// public static readonly Rule HeadingReset = new(El.H1, El.H2, El.H3) {
///     LineHeight = 1.2,
///     FontWeight = 600,
/// };
/// </code>
/// </example>
/// <seealso cref="Class"/>
/// <seealso cref="Declarations"/>
public sealed class Rule : Declarations {
    /// <summary>The selector this rule applies to. For a selector-list rule, it's
    /// the joined comma-separated form (<c>".a, .b, .c"</c>).</summary>
    public Selector Selector { get; }

    /// <summary>Constructs a rule for a single selector.</summary>
    public Rule(Selector selector) {
        Selector = selector;
    }

    /// <summary>Constructs a rule for a selector list (comma-separated). Convenient
    /// for "any of these" rules without building the selector list manually.</summary>
    /// <param name="selectors">Two or more selectors that should all share these declarations.</param>
    public Rule(params Selector[] selectors) {
        if (selectors is null || selectors.Length == 0) {
            throw new System.ArgumentException("At least one selector is required.", nameof(selectors));
        }
        if (selectors.Length == 1) {
            Selector = selectors[0];
            return;
        }
        var joined = string.Join(", ", System.Array.ConvertAll(selectors, s => s.Css));
        Selector = new Selector(joined);
    }
}
