namespace BrowserApi.Css.Authoring;

/// <summary>
/// Represents a CSS selector — the "which elements?" half of a CSS rule. Composes via
/// C# operators that map to CSS combinators with carefully-chosen precedence.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Selector"/> is the universal currency for any rule's left-hand side: simple
/// selectors (class, type, attribute), pseudo-classes (<c>:hover</c>, <c>:focus</c>),
/// compound selectors (<c>.card.active</c>), complex selectors (<c>.card &gt; .title</c>),
/// and selector lists (<c>.card, .panel</c>). Every operator and fluent method returns
/// a new <see cref="Selector"/> so chains compose without intermediate types.
/// </para>
/// <para>
/// Operator precedence is chosen so that natural-looking expressions parse the way a
/// CSS author expects. Compound (<c>*</c>) binds tightest — <c>A * B &gt; C</c> means
/// <c>(A * B) &gt; C</c>, i.e. <c>.a.b &gt; .c</c>. Selector list (<c>|</c>) binds
/// loosest — <c>A | B.Hover</c> means <c>A | (B.Hover)</c>, i.e. <c>.a, .b:hover</c>.
/// </para>
/// <para>
/// Pseudo-classes are exposed as instance properties (<c>.Hover</c>, <c>.Focus</c>, …)
/// because in CSS they always attach to a selector, never stand alone. Pseudo-elements
/// (<c>.Before</c>, <c>.After</c>) return a <see cref="PseudoElementSelector"/> — a
/// constrained type that disallows further pseudo-elements or descendants, since those
/// would produce invalid CSS.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Combinators via operators:
/// Card * Active                  // .card.active        (compound)
/// Card.Hover                     // .card:hover         (pseudo-class)
/// Card &gt; El.A                    // .card &gt; a           (child)
/// Card &gt;&gt; El.Span                // .card span          (descendant)
/// Card + Sibling                 // .card + .sibling    (adjacent sibling)
/// Card - Sibling                 // .card ~ .sibling    (general sibling)
/// Card | Panel | Dialog          // .card, .panel, .dialog (selector list)
///
/// // Functional pseudo-classes via methods:
/// Card.Not(Disabled)             // .card:not(.disabled)
/// Card.NthChild(2)               // .card:nth-child(2)
/// </code>
/// </example>
/// <seealso cref="PseudoElementSelector"/>
/// <seealso cref="Class"/>
public readonly struct Selector : IEquatable<Selector> {
    /// <summary>The raw CSS selector text this instance represents (e.g. <c>".card:hover"</c>).</summary>
    public string Css { get; }

    /// <summary>
    /// Constructs a selector from raw CSS text. Prefer the typed entry points
    /// (<see cref="Class"/>, <c>El.*</c>, <c>Self</c>) — this constructor is for
    /// the internals of those entry points and the rare escape-hatch case.
    /// </summary>
    /// <param name="css">A valid CSS selector string. No validation is performed.</param>
    public Selector(string css) {
        Css = css ?? throw new ArgumentNullException(nameof(css));
    }

    // ─────────────────────────────────── Combinators (operators) ────────────────────

    /// <summary>Compound selector (<c>.a.b</c>) — both selectors apply to the same element.
    /// Highest C# binary-operator precedence, so it binds tighter than any combinator.</summary>
    public static Selector operator *(Selector a, Selector b) => new($"{a.Css}{b.Css}");

    /// <summary>Adjacent-sibling combinator (<c>.a + .b</c>) — <c>b</c> immediately after <c>a</c>.</summary>
    public static Selector operator +(Selector a, Selector b) => new($"{a.Css} + {b.Css}");

    /// <summary>General-sibling combinator (<c>.a ~ .b</c>) — <c>b</c> anywhere after <c>a</c>
    /// with the same parent. Uses C# <c>-</c> because <c>~</c> is unary-only in C#.</summary>
    public static Selector operator -(Selector a, Selector b) => new($"{a.Css} ~ {b.Css}");

    /// <summary>Child combinator (<c>.a &gt; .b</c>) — <c>b</c> directly inside <c>a</c>.</summary>
    public static Selector operator >(Selector a, Selector b) => new($"{a.Css} > {b.Css}");

    /// <summary>The reverse-direction child operator has no CSS equivalent. Stops the build via
    /// analyzer <c>BCA002</c>; this runtime throw is a backstop for reflection paths.</summary>
    public static Selector operator <(Selector a, Selector b) =>
        throw new NotSupportedException("CSS has no '<' combinator. Use '>' (child) instead.");

    /// <summary>Descendant combinator (<c>.a .b</c>) — <c>b</c> nested anywhere inside <c>a</c>.
    /// <c>&gt;&gt;</c> binds tighter than <c>&gt;</c>, so <c>A &gt;&gt; B &gt; C</c> parses as
    /// <c>(A &gt;&gt; B) &gt; C</c>.</summary>
    public static Selector operator >>(Selector a, Selector b) => new($"{a.Css} {b.Css}");

    /// <summary>The reverse-direction descendant operator has no CSS equivalent.
    /// Stops the build via analyzer <c>BCA002</c>; this runtime throw is a backstop.</summary>
    public static Selector operator <<(Selector a, Selector b) =>
        throw new NotSupportedException("CSS has no '<<' combinator. Use '>>' (descendant) instead.");

    /// <summary>Selector list (<c>.a, .b</c>) — match any of the listed selectors.
    /// Lowest precedence so list construction is the outermost interpretation.</summary>
    public static Selector operator |(Selector a, Selector b) => new($"{a.Css}, {b.Css}");

    // ───────────────────────────────── Pseudo-classes (subset) ──────────────────────

    /// <summary>The <c>:hover</c> pseudo-class — element under the user's pointing device.</summary>
    public Selector Hover => new($"{Css}:hover");

    /// <summary>The <c>:focus</c> pseudo-class — element that currently has keyboard focus.</summary>
    public Selector Focus => new($"{Css}:focus");

    /// <summary>The <c>:focus-visible</c> pseudo-class — focus made visible by the user agent
    /// (typically keyboard navigation, not mouse click).</summary>
    public Selector FocusVisible => new($"{Css}:focus-visible");

    /// <summary>The <c>:focus-within</c> pseudo-class — element contains a focused descendant.</summary>
    public Selector FocusWithin => new($"{Css}:focus-within");

    /// <summary>The <c>:active</c> pseudo-class — element being activated (e.g. mouse-down).</summary>
    public Selector Active => new($"{Css}:active");

    /// <summary>The <c>:disabled</c> pseudo-class — disabled form-control element.</summary>
    public Selector Disabled => new($"{Css}:disabled");

    /// <summary>The <c>:checked</c> pseudo-class — checked checkbox/radio/option element.</summary>
    public Selector Checked => new($"{Css}:checked");

    /// <summary>The <c>:first-child</c> pseudo-class — element that is the first child of its parent.</summary>
    public Selector FirstChild => new($"{Css}:first-child");

    /// <summary>The <c>:last-child</c> pseudo-class — element that is the last child of its parent.</summary>
    public Selector LastChild => new($"{Css}:last-child");

    /// <summary>The <c>:nth-child(n)</c> functional pseudo-class.</summary>
    /// <param name="formula">An nth-child formula (e.g. <c>"2"</c>, <c>"odd"</c>, <c>"2n+1"</c>).</param>
    public Selector NthChild(string formula) => new($"{Css}:nth-child({formula})");

    /// <summary>The <c>:nth-of-type(n)</c> functional pseudo-class.</summary>
    public Selector NthOfType(string formula) => new($"{Css}:nth-of-type({formula})");

    /// <summary>The <c>:nth-last-child(n)</c> functional pseudo-class.</summary>
    public Selector NthLastChild(string formula) => new($"{Css}:nth-last-child({formula})");

    /// <summary>The <c>:lang(code)</c> functional pseudo-class.</summary>
    public Selector Lang(string code) => new($"{Css}:lang({code})");

    /// <summary>The <c>:empty</c> pseudo-class — element with no children/text.</summary>
    public Selector Empty => new($"{Css}:empty");

    /// <summary>The <c>:root</c> pseudo-class — the document root (typically html).</summary>
    public Selector Root => new($"{Css}:root");

    /// <summary>The <c>:read-only</c> pseudo-class.</summary>
    public Selector ReadOnly => new($"{Css}:read-only");

    /// <summary>The <c>:read-write</c> pseudo-class.</summary>
    public Selector ReadWrite => new($"{Css}:read-write");

    /// <summary>The <c>:required</c> pseudo-class.</summary>
    public Selector Required => new($"{Css}:required");

    /// <summary>The <c>:optional</c> pseudo-class.</summary>
    public Selector Optional => new($"{Css}:optional");

    /// <summary>The <c>:valid</c> pseudo-class — form field passing validation.</summary>
    public Selector Valid => new($"{Css}:valid");

    /// <summary>The <c>:invalid</c> pseudo-class — form field failing validation.</summary>
    public Selector Invalid => new($"{Css}:invalid");

    /// <summary>The <c>:placeholder-shown</c> pseudo-class — input still showing its placeholder.</summary>
    public Selector PlaceholderShown => new($"{Css}:placeholder-shown");

    /// <summary>The <c>:default</c> pseudo-class — default form element among options.</summary>
    public Selector Default => new($"{Css}:default");

    /// <summary>The <c>:target</c> pseudo-class — element targeted by URL fragment.</summary>
    public Selector Target => new($"{Css}:target");

    /// <summary>The <c>:visited</c> pseudo-class — link that's been visited.</summary>
    public Selector Visited => new($"{Css}:visited");

    /// <summary>The <c>:link</c> pseudo-class — unvisited link.</summary>
    public Selector Link => new($"{Css}:link");

    /// <summary>The <c>:any-link</c> pseudo-class — any link (visited or unvisited).</summary>
    public Selector AnyLink => new($"{Css}:any-link");

    /// <summary>The <c>:dir(direction)</c> functional pseudo-class — directionality
    /// (<c>"ltr"</c> or <c>"rtl"</c>).</summary>
    public Selector Dir(string direction) => new($"{Css}:dir({direction})");

    /// <summary>The <c>:where(...)</c> functional pseudo-class — like
    /// <c>:is</c> but with zero specificity. The <see cref="StyleSheet.Where"/>
    /// helper is the recommended entry point; this method exists for
    /// post-fix usage on an existing selector.</summary>
    public Selector Where(params Selector[] inner) {
        var parts = string.Join(", ", System.Array.ConvertAll(inner, s => s.Css));
        return new Selector($"{Css}:where({parts})");
    }

    /// <summary>The <c>:is(...)</c> functional pseudo-class — like
    /// <c>:where</c> but inherits the highest specificity among arguments.</summary>
    public Selector Is(params Selector[] inner) {
        var parts = string.Join(", ", System.Array.ConvertAll(inner, s => s.Css));
        return new Selector($"{Css}:is({parts})");
    }

    /// <summary>The <c>:not(...)</c> functional pseudo-class — match when the inner selector does NOT match.</summary>
    /// <param name="inner">The selector that must NOT match.</param>
    public Selector Not(Selector inner) => new($"{Css}:not({inner.Css})");

    /// <summary>The <c>:has(...)</c> functional pseudo-class — match when the inner selector matches a descendant.</summary>
    /// <param name="inner">The descendant selector to test for.</param>
    public Selector Has(Selector inner) => new($"{Css}:has({inner.Css})");

    // ─────────────────────────────── Pseudo-elements ────────────────────────────────

    /// <summary>The <c>::before</c> pseudo-element — generated content inserted before the
    /// element's actual content. Returns a <see cref="PseudoElementSelector"/> which forbids
    /// further pseudo-elements and combinators, matching CSS validity rules.</summary>
    public PseudoElementSelector Before => new($"{Css}::before");

    /// <summary>The <c>::after</c> pseudo-element — generated content inserted after the
    /// element's actual content. Returns a <see cref="PseudoElementSelector"/> which forbids
    /// further pseudo-elements and combinators, matching CSS validity rules.</summary>
    public PseudoElementSelector After => new($"{Css}::after");

    /// <summary>The <c>::placeholder</c> pseudo-element — placeholder text in form inputs.</summary>
    public PseudoElementSelector Placeholder => new($"{Css}::placeholder");

    // ─────────────────────────────── Class variants ─────────────────────────────────

    /// <summary>BEM-style modifier (<c>&amp;--variant</c>) — appends <c>--{slug}</c> to the
    /// current selector. The slug parameter is the one place dynamic strings legitimately
    /// flow in: data-driven variants from runtime sources.</summary>
    /// <param name="slug">The variant suffix (without leading <c>--</c>).</param>
    public Selector Variant(string slug) => new($"{Css}--{slug}");

    // ─────────────────────────────── Equality / display ─────────────────────────────

    /// <inheritdoc/>
    public bool Equals(Selector other) => Css == other.Css;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Selector s && Equals(s);

    /// <inheritdoc/>
    public override int GetHashCode() => Css.GetHashCode();

    /// <summary>Returns the CSS selector string. Useful for diagnostics and tests.</summary>
    public override string ToString() => Css;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Selector a, Selector b) => a.Equals(b);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Selector a, Selector b) => !a.Equals(b);
}
