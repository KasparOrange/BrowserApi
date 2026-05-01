using System.Collections.Generic;
using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// The "what styles?" half of a CSS rule: an ordered set of property/value declarations
/// plus any nested rules (selector indexer, media queries, container queries, etc.).
/// </summary>
/// <remarks>
/// <para>
/// In a finished implementation, every CSS property would be exposed here as an
/// <c>init</c>-only typed setter (e.g. <c>Padding</c> accepting
/// <see cref="LengthOrPercentage"/>, <c>Color</c> accepting <c>Color</c>, …) so that
/// invalid values become C# compile errors. This MVP slice exposes a small
/// representative subset by name; the rest are emitted from the CSS spec data files
/// in a follow-up commit. Until then, <see cref="Set"/> is the explicit setter for
/// any property by string name.
/// </para>
/// <para>
/// The class also holds the nested-rule mechanism via the
/// <see cref="this[Selector]"/> indexer — the universal "attach something to this rule"
/// hook used for pseudo-class blocks, descendant rules, media queries, and so on.
/// </para>
/// <para>
/// Source order matters for CSS cascade. <see cref="Declarations"/> stores its
/// entries in <see cref="List{T}"/>s, preserving the order in which the C# object
/// initializer assigned them. Same-key duplicates are kept rather than overwritten —
/// CSS naturally handles this via cascade, and a future analyzer flags overlaps.
/// </para>
/// </remarks>
/// <seealso cref="Class"/>
/// <seealso cref="Rule"/>
public abstract class Declarations {
    private readonly List<KeyValuePair<string, string>> _props = new();
    private readonly List<KeyValuePair<Selector, Declarations>> _nested = new();

    /// <summary>The property/value declarations in source order.</summary>
    /// <remarks>Exposed for emitter access; not intended for user code.</remarks>
    public IReadOnlyList<KeyValuePair<string, string>> Properties => _props;

    /// <summary>The nested rules (pseudo-classes, descendant selectors, etc.) in source order.</summary>
    /// <remarks>Exposed for emitter access; not intended for user code.</remarks>
    public IReadOnlyList<KeyValuePair<Selector, Declarations>> Nested => _nested;

    /// <summary>
    /// Sets a CSS property by name with an arbitrary <see cref="ICssValue"/>.
    /// Used by the typed property setters below and as an escape hatch for
    /// properties not yet exposed as typed members.
    /// </summary>
    /// <param name="cssPropertyName">The CSS property name as it appears in CSS
    /// (e.g. <c>"padding"</c>, <c>"background-color"</c>). Kebab-case.</param>
    /// <param name="value">The value implementing <see cref="ICssValue"/>; serialized
    /// via <see cref="ICssValue.ToCss"/>.</param>
    protected void Set(string cssPropertyName, ICssValue value) {
        _props.Add(new(cssPropertyName, value.ToCss()));
    }

    /// <summary>
    /// Sets a CSS property with a pre-rendered string value. Internal — used by
    /// typed-keyword setters where the value is a known token.
    /// </summary>
    protected void SetRaw(string cssPropertyName, string cssValue) {
        _props.Add(new(cssPropertyName, cssValue));
    }

    // ─────────────────────────────────── Nesting indexer ────────────────────────────

    /// <summary>
    /// Universal "attach a nested block" indexer. The same indexer accepts pseudo-class
    /// selectors, descendant selectors, media queries, container queries, and feature
    /// queries — they all express "in this context, apply these declarations."
    /// </summary>
    /// <param name="selector">The selector for the nested rule. <c>Self</c> refers to
    /// the parent rule (SCSS <c>&amp;</c>); other selectors compose normally.</param>
    /// <remarks>
    /// The getter is a stub — it's required by the C# language for indexer-initializer
    /// syntax to compile, but never actually invoked. All initializer assignments call
    /// the setter, which appends to the nested-rules list.
    /// </remarks>
    public Declarations this[Selector selector] {
        get => throw new System.NotSupportedException(
            "The Declarations indexer is set-only; reading it is never meaningful.");
        set => _nested.Add(new(selector, value));
    }

    // ─────────────────────── Representative typed property setters ──────────────────
    // The full set is generated from CSS spec data in a follow-up commit. These are
    // present so the MVP can build a real card style end-to-end.

    /// <summary>The CSS <c>padding</c> shorthand. Accepts a length on all four sides for
    /// the MVP; the full shorthand (Sides type, tuple conversions) lands later.</summary>
    public Length Padding { init => Set("padding", value); }

    /// <summary>The CSS <c>margin</c> shorthand. MVP scope: single length all-sides.</summary>
    public Length Margin { init => Set("margin", value); }

    /// <summary>The CSS <c>background</c> shorthand — a color for the MVP.</summary>
    public CssColor Background { init => Set("background", value); }

    /// <summary>The CSS <c>color</c> property (foreground/text color).</summary>
    public CssColor Color { init => Set("color", value); }

    /// <summary>The CSS <c>width</c> property.</summary>
    public Length Width { init => Set("width", value); }

    /// <summary>The CSS <c>height</c> property.</summary>
    public Length Height { init => Set("height", value); }

    /// <summary>The CSS <c>font-size</c> property.</summary>
    public Length FontSize { init => Set("font-size", value); }

    /// <summary>The CSS <c>border-radius</c> property.</summary>
    public Length BorderRadius { init => Set("border-radius", value); }

    /// <summary>The CSS <c>opacity</c> property — accepts a 0..1 number.</summary>
    public double Opacity { init => SetRaw("opacity", value.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
}
