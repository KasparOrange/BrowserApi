namespace BrowserApi.Css.Authoring;

/// <summary>
/// A selector that has had a pseudo-element (<c>::before</c>, <c>::after</c>,
/// <c>::placeholder</c>, …) attached. Constrained on purpose: CSS forbids
/// further pseudo-elements, descendant/child/sibling combinators, and structural
/// pseudo-classes after a pseudo-element. The type system enforces this — the
/// disallowed members simply don't exist on this struct.
/// </summary>
/// <remarks>
/// <para>
/// Pseudo-classes ARE legal after a pseudo-element (<c>::before:hover</c> is valid
/// CSS), so <see cref="Hover"/>, <see cref="Focus"/>, etc. remain available and
/// return another <see cref="PseudoElementSelector"/>.
/// </para>
/// <para>
/// What's deliberately NOT here: <c>.Before</c>/<c>.After</c> (would be two
/// pseudo-elements); the combinator operators (<c>&gt;</c>, <c>&gt;&gt;</c>,
/// <c>+</c>, <c>-</c>); structural pseudo-classes like <c>.NthChild</c>
/// (no structure past a pseudo-element). Attempting any of those is a C#
/// compile error rather than an SCSS or runtime CSS validity error — the
/// invalid combinations never type-check.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Valid — pseudo-class on a pseudo-element:
/// Card.After.Hover           // .card::after:hover
/// Card.Hover.After           // .card:hover::after
///
/// // Compile errors:
/// Card.After.Before          // CS1061: no member 'Before' on PseudoElementSelector
/// Card.After &gt; El.Span        // CS0019: '&gt;' on (PseudoElementSelector, Selector) not defined
/// </code>
/// </example>
public readonly struct PseudoElementSelector {
    /// <summary>The raw CSS selector text this instance represents.</summary>
    public string Css { get; }

    /// <summary>Wraps an already-pseudo-elemented selector string.</summary>
    public PseudoElementSelector(string css) {
        Css = css ?? throw new ArgumentNullException(nameof(css));
    }

    /// <summary>The <c>:hover</c> pseudo-class on this pseudo-element.</summary>
    public PseudoElementSelector Hover => new($"{Css}:hover");

    /// <summary>The <c>:focus</c> pseudo-class on this pseudo-element.</summary>
    public PseudoElementSelector Focus => new($"{Css}:focus");

    /// <summary>The <c>:active</c> pseudo-class on this pseudo-element.</summary>
    public PseudoElementSelector Active => new($"{Css}:active");

    /// <summary>Implicit upcast to <see cref="Selector"/> so a
    /// <see cref="PseudoElementSelector"/> can be used anywhere a selector is
    /// accepted (e.g. as a key in a nesting indexer). The conversion is one-way:
    /// once you have a <see cref="Selector"/> the wider API is available, but
    /// invalid combinations like <c>.After.Before</c> are still caught earlier.</summary>
    public static implicit operator Selector(PseudoElementSelector p) => new(p.Css);

    /// <summary>Returns the CSS selector string.</summary>
    public override string ToString() => Css;
}
