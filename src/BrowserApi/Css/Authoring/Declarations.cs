using System.Collections.Generic;
using System.Globalization;
using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// The "what styles?" half of a CSS rule: an ordered set of property/value declarations
/// plus any nested rules (selector indexer, media queries, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Declarations are populated via C# object-initializer syntax. Each typed
/// <c>init</c>-only setter accepts the right CSS-value type for its property —
/// <c>Padding</c> takes a <see cref="Length"/>, <c>Color</c> takes a
/// <see cref="CssColor"/>, <c>Display</c> takes a <see cref="Authoring.Display"/>
/// keyword. Wrong types are compile errors, not runtime surprises.
/// </para>
/// <para>
/// The class is intentionally non-abstract so that target-typed <c>new()</c>
/// works inside the nesting indexer:
/// <code>
/// [Self.Hover] = new() { Padding = 8.Px() }
/// </code>
/// </para>
/// <para>
/// Source order is preserved by storing entries in <see cref="List{T}"/>s. Same-key
/// duplicates are kept rather than overwritten — CSS naturally handles this via
/// cascade. A future analyzer flags overlaps.
/// </para>
/// </remarks>
public class Declarations {
    private readonly List<KeyValuePair<string, string>> _props = new();
    private readonly List<KeyValuePair<Selector, Declarations>> _nested = new();

    /// <summary>The property/value declarations in source order.</summary>
    /// <remarks>Exposed for the emitter; not intended for user code.</remarks>
    public IReadOnlyList<KeyValuePair<string, string>> Properties => _props;

    /// <summary>The nested rules in source order.</summary>
    /// <remarks>Exposed for the emitter; not intended for user code.</remarks>
    public IReadOnlyList<KeyValuePair<Selector, Declarations>> Nested => _nested;

    /// <summary>Sets a CSS property by name with an arbitrary <see cref="ICssValue"/>.</summary>
    /// <param name="cssPropertyName">CSS property name in kebab-case (e.g. <c>"padding"</c>).</param>
    /// <param name="value">The value; serialized via <see cref="ICssValue.ToCss"/>.</param>
    protected void Set(string cssPropertyName, ICssValue value) {
        _props.Add(new(cssPropertyName, value.ToCss()));
    }

    /// <summary>Sets a CSS property with a pre-rendered string value.</summary>
    protected void SetRaw(string cssPropertyName, string cssValue) {
        _props.Add(new(cssPropertyName, cssValue));
    }

    /// <summary>Sets a CSS property using an enum value, converting to kebab-case.</summary>
    protected void SetKeyword<T>(string cssPropertyName, T value) where T : System.Enum {
        _props.Add(new(cssPropertyName, value.AsCss()));
    }

    /// <summary>Sets a CSS property using a <see cref="Keyword{TEnum}"/>,
    /// which accepts both the bare enum value and a <c>.Important</c>-wrapped
    /// version via implicit conversion.</summary>
    protected void SetKeyword<T>(string cssPropertyName, Keyword<T> value) where T : System.Enum {
        _props.Add(new(cssPropertyName, value.ToCss()));
    }

    private static string Num(double d) => d.ToString(CultureInfo.InvariantCulture);

    // ─────────────────────────────────── Nesting indexer ────────────────────────────

    /// <summary>
    /// Universal "attach a nested block" indexer — the same mechanism for pseudo-class
    /// blocks (<c>[Self.Hover]</c>), descendant rules (<c>[Self &gt; El.A]</c>), and
    /// (in a follow-up) media/container/feature queries.
    /// </summary>
    /// <param name="selector">The selector for the nested rule. <c>Self</c> refers to
    /// the parent rule (SCSS <c>&amp;</c>).</param>
    public Declarations this[Selector selector] {
        get => throw new System.NotSupportedException(
            "The Declarations indexer is set-only; reading it is never meaningful.");
        set => _nested.Add(new(selector, value));
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Layout
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>display</c> property — sets the box type.</summary>
    public Keyword<Display> Display { init => SetKeyword("display", value); }

    /// <summary>The CSS <c>position</c> property.</summary>
    public Keyword<Position> Position { init => SetKeyword("position", value); }

    /// <summary>The CSS <c>visibility</c> property. Uses the CSSOM-generated enum
    /// in <see cref="BrowserApi.Css"/> which carries the correct <c>[StringValue]</c>
    /// attributes for serialization.</summary>
    public Keyword<BrowserApi.Css.Visibility> Visibility { init => SetKeyword("visibility", value); }

    /// <summary>The CSS <c>overflow</c> shorthand.</summary>
    public Keyword<Overflow> Overflow { init => SetKeyword("overflow", value); }

    /// <summary>The CSS <c>overflow-x</c> property.</summary>
    public Keyword<Overflow> OverflowX { init => SetKeyword("overflow-x", value); }

    /// <summary>The CSS <c>overflow-y</c> property.</summary>
    public Keyword<Overflow> OverflowY { init => SetKeyword("overflow-y", value); }

    /// <summary>The CSS <c>z-index</c> property.</summary>
    public int ZIndex { init => SetRaw("z-index", value.ToString(CultureInfo.InvariantCulture)); }

    /// <summary>The CSS <c>top</c> offset.</summary>
    public LengthOrPercentage Top { init => Set("top", value); }

    /// <summary>The CSS <c>right</c> offset.</summary>
    public LengthOrPercentage Right { init => Set("right", value); }

    /// <summary>The CSS <c>bottom</c> offset.</summary>
    public LengthOrPercentage Bottom { init => Set("bottom", value); }

    /// <summary>The CSS <c>left</c> offset.</summary>
    public LengthOrPercentage Left { init => Set("left", value); }

    /// <summary>The CSS <c>inset</c> shorthand (all four sides).</summary>
    public LengthOrPercentage Inset { init => Set("inset", value); }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Box model
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>box-sizing</c> property. Uses the CSSOM-generated enum
    /// in <see cref="BrowserApi.Css"/>.</summary>
    public Keyword<BrowserApi.Css.BoxSizing> BoxSizing { init => SetKeyword("box-sizing", value); }

    /// <summary>The CSS <c>width</c> property.</summary>
    public LengthOrPercentage Width { init => Set("width", value); }

    /// <summary>The CSS <c>height</c> property.</summary>
    public LengthOrPercentage Height { init => Set("height", value); }

    /// <summary>The CSS <c>min-width</c> property.</summary>
    public LengthOrPercentage MinWidth { init => Set("min-width", value); }

    /// <summary>The CSS <c>min-height</c> property.</summary>
    public LengthOrPercentage MinHeight { init => Set("min-height", value); }

    /// <summary>The CSS <c>max-width</c> property.</summary>
    public LengthOrPercentage MaxWidth { init => Set("max-width", value); }

    /// <summary>The CSS <c>max-height</c> property.</summary>
    public LengthOrPercentage MaxHeight { init => Set("max-height", value); }

    /// <summary>The CSS <c>padding</c> shorthand. Accepts a single <see cref="Length"/>
    /// (all sides), a <c>(vertical, horizontal)</c> tuple, or a
    /// <c>(top, right, bottom, left)</c> tuple via <see cref="Sides"/>.</summary>
    public Sides Padding { init => Set("padding", value); }

    /// <summary>The CSS <c>padding-top</c> property.</summary>
    public Length PaddingTop { init => Set("padding-top", value); }

    /// <summary>The CSS <c>padding-right</c> property.</summary>
    public Length PaddingRight { init => Set("padding-right", value); }

    /// <summary>The CSS <c>padding-bottom</c> property.</summary>
    public Length PaddingBottom { init => Set("padding-bottom", value); }

    /// <summary>The CSS <c>padding-left</c> property.</summary>
    public Length PaddingLeft { init => Set("padding-left", value); }

    /// <summary>The CSS <c>margin</c> shorthand. Same multi-form input as <see cref="Padding"/>.</summary>
    public Sides Margin { init => Set("margin", value); }

    /// <summary>The CSS <c>margin-top</c> property.</summary>
    public Length MarginTop { init => Set("margin-top", value); }

    /// <summary>The CSS <c>margin-right</c> property.</summary>
    public Length MarginRight { init => Set("margin-right", value); }

    /// <summary>The CSS <c>margin-bottom</c> property.</summary>
    public Length MarginBottom { init => Set("margin-bottom", value); }

    /// <summary>The CSS <c>margin-left</c> property.</summary>
    public Length MarginLeft { init => Set("margin-left", value); }

    /// <summary>The CSS <c>margin-block</c> shorthand (logical block-axis margins).</summary>
    public Length MarginBlock { init => Set("margin-block", value); }

    /// <summary>The CSS <c>margin-inline</c> shorthand (logical inline-axis margins).</summary>
    public Length MarginInline { init => Set("margin-inline", value); }

    /// <summary>The CSS <c>border</c> shorthand. Use <see cref="Authoring.Border.Solid"/> etc.</summary>
    public Border Border { init => SetRaw("border", value.ToString()); }

    /// <summary>The CSS <c>border-top</c> shorthand.</summary>
    public Border BorderTop { init => SetRaw("border-top", value.ToString()); }

    /// <summary>The CSS <c>border-right</c> shorthand.</summary>
    public Border BorderRight { init => SetRaw("border-right", value.ToString()); }

    /// <summary>The CSS <c>border-bottom</c> shorthand.</summary>
    public Border BorderBottom { init => SetRaw("border-bottom", value.ToString()); }

    /// <summary>The CSS <c>border-left</c> shorthand.</summary>
    public Border BorderLeft { init => SetRaw("border-left", value.ToString()); }

    /// <summary>The CSS <c>border-color</c> shorthand.</summary>
    public CssColor BorderColor { init => Set("border-color", value); }

    /// <summary>The CSS <c>border-width</c> shorthand.</summary>
    public Length BorderWidth { init => Set("border-width", value); }

    /// <summary>The CSS <c>border-style</c> shorthand.</summary>
    public Keyword<BorderStyle> BorderStyle { init => SetKeyword("border-style", value); }

    /// <summary>The CSS <c>border-radius</c> shorthand.</summary>
    public Length BorderRadius { init => Set("border-radius", value); }

    /// <summary>The CSS <c>border-top-left-radius</c> property.</summary>
    public Length BorderTopLeftRadius { init => Set("border-top-left-radius", value); }

    /// <summary>The CSS <c>border-top-right-radius</c> property.</summary>
    public Length BorderTopRightRadius { init => Set("border-top-right-radius", value); }

    /// <summary>The CSS <c>border-bottom-left-radius</c> property.</summary>
    public Length BorderBottomLeftRadius { init => Set("border-bottom-left-radius", value); }

    /// <summary>The CSS <c>border-bottom-right-radius</c> property.</summary>
    public Length BorderBottomRightRadius { init => Set("border-bottom-right-radius", value); }

    /// <summary>The CSS <c>outline</c> shorthand.</summary>
    public Border Outline { init => SetRaw("outline", value.ToString()); }

    /// <summary>The CSS <c>outline-offset</c> property.</summary>
    public Length OutlineOffset { init => Set("outline-offset", value); }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Flexbox & Grid
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>flex-direction</c> property. Uses the CSSOM-generated
    /// enum in <see cref="BrowserApi.Css"/>.</summary>
    public Keyword<BrowserApi.Css.FlexDirection> FlexDirection { init => SetKeyword("flex-direction", value); }

    /// <summary>The CSS <c>flex-wrap</c> property. Uses the CSSOM-generated enum
    /// in <see cref="BrowserApi.Css"/>.</summary>
    public Keyword<BrowserApi.Css.FlexWrap> FlexWrap { init => SetKeyword("flex-wrap", value); }

    /// <summary>The CSS <c>flex-grow</c> property.</summary>
    public double FlexGrow { init => SetRaw("flex-grow", Num(value)); }

    /// <summary>The CSS <c>flex-shrink</c> property.</summary>
    public double FlexShrink { init => SetRaw("flex-shrink", Num(value)); }

    /// <summary>The CSS <c>flex-basis</c> property.</summary>
    public Length FlexBasis { init => Set("flex-basis", value); }

    /// <summary>The CSS <c>justify-content</c> property.</summary>
    public Keyword<JustifyContent> JustifyContent { init => SetKeyword("justify-content", value); }

    /// <summary>The CSS <c>align-items</c> property.</summary>
    public Keyword<AlignItems> AlignItems { init => SetKeyword("align-items", value); }

    /// <summary>The CSS <c>align-self</c> property.</summary>
    public Keyword<AlignItems> AlignSelf { init => SetKeyword("align-self", value); }

    /// <summary>The CSS <c>align-content</c> property.</summary>
    public Keyword<AlignItems> AlignContent { init => SetKeyword("align-content", value); }

    /// <summary>The CSS <c>order</c> property — flex/grid item order.</summary>
    public int Order { init => SetRaw("order", value.ToString(CultureInfo.InvariantCulture)); }

    /// <summary>The CSS <c>gap</c> shorthand (row + column gap).</summary>
    public Length Gap { init => Set("gap", value); }

    /// <summary>The CSS <c>row-gap</c> property.</summary>
    public Length RowGap { init => Set("row-gap", value); }

    /// <summary>The CSS <c>column-gap</c> property.</summary>
    public Length ColumnGap { init => Set("column-gap", value); }

    /// <summary>The CSS <c>grid-template-columns</c> property — accepts a
    /// <see cref="GridTemplate"/> directly (with implicit conversions from
    /// <see cref="Length"/>, <see cref="Flex"/>, and <see cref="string"/>).</summary>
    public GridTemplate GridTemplateColumns { init => SetRaw("grid-template-columns", value.ToCss()); }

    /// <summary>The CSS <c>grid-template-rows</c> property.</summary>
    public GridTemplate GridTemplateRows { init => SetRaw("grid-template-rows", value.ToCss()); }

    /// <summary>The CSS <c>grid-area</c> property.</summary>
    public string GridArea { init => SetRaw("grid-area", value); }

    /// <summary>The CSS <c>grid-column</c> property.</summary>
    public string GridColumn { init => SetRaw("grid-column", value); }

    /// <summary>The CSS <c>grid-row</c> property.</summary>
    public string GridRow { init => SetRaw("grid-row", value); }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Color & Background
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>color</c> property — text color.</summary>
    public CssColor Color { init => Set("color", value); }

    /// <summary>The CSS <c>background</c> shorthand. MVP: a color.</summary>
    public CssColor Background { init => Set("background", value); }

    /// <summary>The CSS <c>background-color</c> property.</summary>
    public CssColor BackgroundColor { init => Set("background-color", value); }

    /// <summary>The CSS <c>background-image</c> property — string for now;
    /// typed <c>Gradient.Linear(...)</c> already exists in <see cref="BrowserApi.Css.Gradient"/>.</summary>
    public ICssValue BackgroundImage { init => Set("background-image", value); }

    /// <summary>The CSS <c>background-size</c> property.</summary>
    public string BackgroundSize { init => SetRaw("background-size", value); }

    /// <summary>The CSS <c>background-position</c> property.</summary>
    public string BackgroundPosition { init => SetRaw("background-position", value); }

    /// <summary>The CSS <c>background-repeat</c> property.</summary>
    public string BackgroundRepeat { init => SetRaw("background-repeat", value); }

    /// <summary>The CSS <c>opacity</c> property — 0..1.</summary>
    public double Opacity { init => SetRaw("opacity", Num(value)); }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Typography
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>font-family</c> property — pass a comma-list string
    /// (e.g. <c>"Inter, system-ui, sans-serif"</c>).</summary>
    public string FontFamily { init => SetRaw("font-family", value); }

    /// <summary>The CSS <c>font-size</c> property.</summary>
    public Length FontSize { init => Set("font-size", value); }

    /// <summary>The CSS <c>font-weight</c> property — accepts numbers like 400, 600, 700.</summary>
    public int FontWeight { init => SetRaw("font-weight", value.ToString(CultureInfo.InvariantCulture)); }

    /// <summary>The CSS <c>font-style</c> property.</summary>
    public Keyword<FontStyle> FontStyle { init => SetKeyword("font-style", value); }

    /// <summary>The CSS <c>line-height</c> as a unitless multiplier.</summary>
    public double LineHeight { init => SetRaw("line-height", Num(value)); }

    /// <summary>The CSS <c>line-height</c> as a length.</summary>
    public Length LineHeightLength { init => Set("line-height", value); }

    /// <summary>The CSS <c>letter-spacing</c> property.</summary>
    public Length LetterSpacing { init => Set("letter-spacing", value); }

    /// <summary>The CSS <c>word-spacing</c> property.</summary>
    public Length WordSpacing { init => Set("word-spacing", value); }

    /// <summary>The CSS <c>text-align</c> property.</summary>
    public Keyword<TextAlign> TextAlign { init => SetKeyword("text-align", value); }

    /// <summary>The CSS <c>text-decoration</c> property.</summary>
    public Keyword<TextDecoration> TextDecoration { init => SetKeyword("text-decoration", value); }

    /// <summary>The CSS <c>text-transform</c> property.</summary>
    public Keyword<TextTransform> TextTransform { init => SetKeyword("text-transform", value); }

    /// <summary>The CSS <c>white-space</c> property.</summary>
    public Keyword<WhiteSpace> WhiteSpace { init => SetKeyword("white-space", value); }

    /// <summary>The CSS <c>text-overflow</c> property — typically <c>"ellipsis"</c> or <c>"clip"</c>.</summary>
    public string TextOverflow { init => SetRaw("text-overflow", value); }

    /// <summary>The CSS <c>text-indent</c> property.</summary>
    public Length TextIndent { init => Set("text-indent", value); }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Effects & Misc
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>cursor</c> property.</summary>
    public Keyword<Cursor> Cursor { init => SetKeyword("cursor", value); }

    /// <summary>The CSS <c>box-shadow</c> property — string for now.</summary>
    public string BoxShadow { init => SetRaw("box-shadow", value); }

    /// <summary>The CSS <c>text-shadow</c> property.</summary>
    public string TextShadow { init => SetRaw("text-shadow", value); }

    /// <summary>The CSS <c>transform</c> property.</summary>
    public string Transform { init => SetRaw("transform", value); }

    /// <summary>The CSS <c>transform-origin</c> property.</summary>
    public string TransformOrigin { init => SetRaw("transform-origin", value); }

    /// <summary>The CSS <c>transition</c> shorthand.</summary>
    public string Transition { init => SetRaw("transition", value); }

    /// <summary>The CSS <c>animation</c> shorthand.</summary>
    public string Animation { init => SetRaw("animation", value); }

    /// <summary>The CSS <c>filter</c> property.</summary>
    public string Filter { init => SetRaw("filter", value); }

    /// <summary>The CSS <c>backdrop-filter</c> property.</summary>
    public string BackdropFilter { init => SetRaw("backdrop-filter", value); }

    /// <summary>The CSS <c>pointer-events</c> property — typically <c>"none"</c> or <c>"auto"</c>.</summary>
    public string PointerEvents { init => SetRaw("pointer-events", value); }

    /// <summary>The CSS <c>user-select</c> property.</summary>
    public string UserSelect { init => SetRaw("user-select", value); }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  Container queries (CSS containment)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>The CSS <c>container-type</c> property — establishes a containment
    /// context for <c>@container</c> queries. Common values: <c>"inline-size"</c>,
    /// <c>"size"</c>, <c>"normal"</c>.</summary>
    public string ContainerType { init => SetRaw("container-type", value); }

    /// <summary>The CSS <c>container-name</c> property — names the container for
    /// targeted <c>@container</c> queries.</summary>
    public string ContainerName { init => SetRaw("container-name", value); }

    /// <summary>The CSS <c>container</c> shorthand (<c>container-name</c> /
    /// <c>container-type</c>).</summary>
    public string Container { init => SetRaw("container", value); }
}
