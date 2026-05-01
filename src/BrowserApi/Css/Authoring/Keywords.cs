using System.Reflection;
using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// CSS keyword enums for property values. These are real C# <c>enum</c>s — the spec
/// (§14) keeps enums for switch-exhaustiveness and zero-allocation, with C# 14
/// extension properties layering on <c>.Important</c> when needed.
/// </summary>
/// <remarks>
/// <para>
/// Some keyword enums are defined here (the ones the generated CSSOM doesn't ship)
/// and use PascalCase names that <see cref="KeywordExtensions.AsCss"/> converts to
/// kebab-case at serialization time. Others — <c>BrowserApi.Css.BoxSizing</c>,
/// <c>BrowserApi.Css.Visibility</c>, <c>BrowserApi.Css.FlexDirection</c>,
/// <c>BrowserApi.Css.FlexWrap</c> — are reused from the existing CSSOM-generated
/// enums (decorated with <see cref="StringValueAttribute"/>). Both kinds are
/// supported by <see cref="KeywordExtensions.AsCss"/> via attribute inspection.
/// </para>
/// </remarks>
internal static class KeywordExtensions {
    /// <summary>Returns the CSS string for an enum value: the
    /// <see cref="StringValueAttribute"/> value if present, otherwise the C# name
    /// converted PascalCase → kebab-case (<c>InlineBlock</c> → <c>inline-block</c>).</summary>
    public static string AsCss<T>(this T value) where T : System.Enum {
        var name = value.ToString();
        var member = typeof(T).GetField(name!);
        var attr = member?.GetCustomAttribute<StringValueAttribute>();
        if (attr is not null) return attr.Value;

        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++) {
            var ch = name[i];
            if (i > 0 && char.IsUpper(ch)) sb.Append('-');
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}

/// <summary>Values for the CSS <c>display</c> property.</summary>
public enum Display {
    /// <summary><c>block</c> — generates a block-level box.</summary>
    Block,
    /// <summary><c>inline</c> — generates an inline-level box.</summary>
    Inline,
    /// <summary><c>inline-block</c> — block-like in flow but line-wrappable like inline.</summary>
    InlineBlock,
    /// <summary><c>flex</c> — establishes a flex formatting context.</summary>
    Flex,
    /// <summary><c>inline-flex</c> — inline version of flex.</summary>
    InlineFlex,
    /// <summary><c>grid</c> — establishes a grid formatting context.</summary>
    Grid,
    /// <summary><c>inline-grid</c> — inline version of grid.</summary>
    InlineGrid,
    /// <summary><c>none</c> — element produces no box at all.</summary>
    None,
    /// <summary><c>contents</c> — element's box is replaced by its contents.</summary>
    Contents,
    /// <summary><c>flow-root</c> — establishes a new block formatting context.</summary>
    FlowRoot,
}

/// <summary>Values for the CSS <c>position</c> property.</summary>
public enum Position {
    /// <summary><c>static</c> — default; not affected by top/right/bottom/left.</summary>
    Static,
    /// <summary><c>relative</c> — offset from its normal position.</summary>
    Relative,
    /// <summary><c>absolute</c> — positioned relative to nearest positioned ancestor.</summary>
    Absolute,
    /// <summary><c>fixed</c> — positioned relative to the viewport.</summary>
    Fixed,
    /// <summary><c>sticky</c> — relative until a scroll threshold, then fixed.</summary>
    Sticky,
}

// FlexDirection and FlexWrap are reused from the generated CSSOM enums in
// BrowserApi.Css — they already carry [StringValue] attributes that AsCss() honors.

/// <summary>Values for the CSS <c>justify-content</c> property.</summary>
public enum JustifyContent {
    /// <summary><c>flex-start</c> — items at start of main axis.</summary>
    FlexStart,
    /// <summary><c>flex-end</c> — items at end of main axis.</summary>
    FlexEnd,
    /// <summary><c>center</c> — items centered on main axis.</summary>
    Center,
    /// <summary><c>space-between</c> — first/last at edges, equal gaps between.</summary>
    SpaceBetween,
    /// <summary><c>space-around</c> — equal space around each item.</summary>
    SpaceAround,
    /// <summary><c>space-evenly</c> — truly equal spacing including edges.</summary>
    SpaceEvenly,
    /// <summary><c>start</c> — logical start (newer alias for flex-start).</summary>
    Start,
    /// <summary><c>end</c> — logical end (newer alias for flex-end).</summary>
    End,
    /// <summary><c>stretch</c> — items stretch to fill available space.</summary>
    Stretch,
}

/// <summary>Values for CSS <c>align-items</c> / <c>align-self</c> / <c>align-content</c>.</summary>
public enum AlignItems {
    /// <summary><c>stretch</c> — items stretch across the cross axis.</summary>
    Stretch,
    /// <summary><c>flex-start</c> — items at start of cross axis.</summary>
    FlexStart,
    /// <summary><c>flex-end</c> — items at end of cross axis.</summary>
    FlexEnd,
    /// <summary><c>center</c> — items centered on cross axis.</summary>
    Center,
    /// <summary><c>baseline</c> — items aligned along their text baselines.</summary>
    Baseline,
    /// <summary><c>start</c> — logical start.</summary>
    Start,
    /// <summary><c>end</c> — logical end.</summary>
    End,
}

/// <summary>Values for the CSS <c>text-align</c> property.</summary>
public enum TextAlign {
    /// <summary><c>left</c></summary>
    Left,
    /// <summary><c>right</c></summary>
    Right,
    /// <summary><c>center</c></summary>
    Center,
    /// <summary><c>justify</c></summary>
    Justify,
    /// <summary><c>start</c> (logical)</summary>
    Start,
    /// <summary><c>end</c> (logical)</summary>
    End,
}

// BoxSizing is reused from BrowserApi.Css (CSSOM-generated enum).

/// <summary>Values for the CSS <c>overflow</c> properties.</summary>
public enum Overflow {
    /// <summary><c>visible</c> — content not clipped.</summary>
    Visible,
    /// <summary><c>hidden</c> — content clipped without scrollbars.</summary>
    Hidden,
    /// <summary><c>scroll</c> — clipped, scrollbars always present.</summary>
    Scroll,
    /// <summary><c>auto</c> — clipped with scrollbars only when needed.</summary>
    Auto,
    /// <summary><c>clip</c> — like hidden but no scroll context.</summary>
    Clip,
}

/// <summary>Values for the CSS <c>cursor</c> property (subset).</summary>
public enum Cursor {
    /// <summary><c>auto</c></summary>
    Auto,
    /// <summary><c>default</c></summary>
    Default,
    /// <summary><c>pointer</c> — typically a hand for clickable elements.</summary>
    Pointer,
    /// <summary><c>text</c></summary>
    Text,
    /// <summary><c>move</c></summary>
    Move,
    /// <summary><c>not-allowed</c></summary>
    NotAllowed,
    /// <summary><c>wait</c></summary>
    Wait,
    /// <summary><c>help</c></summary>
    Help,
    /// <summary><c>grab</c></summary>
    Grab,
    /// <summary><c>grabbing</c></summary>
    Grabbing,
    /// <summary><c>crosshair</c></summary>
    Crosshair,
    /// <summary><c>none</c></summary>
    None,
}

// Visibility is reused from BrowserApi.Css (CSSOM-generated enum).

/// <summary>Values for the CSS <c>font-style</c> property.</summary>
public enum FontStyle {
    /// <summary><c>normal</c></summary>
    Normal,
    /// <summary><c>italic</c></summary>
    Italic,
    /// <summary><c>oblique</c></summary>
    Oblique,
}

/// <summary>Values for the CSS <c>text-decoration-line</c> / shorthand <c>text-decoration</c>.</summary>
public enum TextDecoration {
    /// <summary><c>none</c></summary>
    None,
    /// <summary><c>underline</c></summary>
    Underline,
    /// <summary><c>overline</c></summary>
    Overline,
    /// <summary><c>line-through</c></summary>
    LineThrough,
}

/// <summary>Values for the CSS <c>text-transform</c> property.</summary>
public enum TextTransform {
    /// <summary><c>none</c></summary>
    None,
    /// <summary><c>capitalize</c></summary>
    Capitalize,
    /// <summary><c>uppercase</c></summary>
    Uppercase,
    /// <summary><c>lowercase</c></summary>
    Lowercase,
}

/// <summary>Values for the CSS <c>white-space</c> property.</summary>
public enum WhiteSpace {
    /// <summary><c>normal</c></summary>
    Normal,
    /// <summary><c>nowrap</c></summary>
    Nowrap,
    /// <summary><c>pre</c></summary>
    Pre,
    /// <summary><c>pre-wrap</c></summary>
    PreWrap,
    /// <summary><c>pre-line</c></summary>
    PreLine,
    /// <summary><c>break-spaces</c></summary>
    BreakSpaces,
}

/// <summary>Common border-style values.</summary>
public enum BorderStyle {
    /// <summary><c>none</c></summary>
    None,
    /// <summary><c>solid</c></summary>
    Solid,
    /// <summary><c>dashed</c></summary>
    Dashed,
    /// <summary><c>dotted</c></summary>
    Dotted,
    /// <summary><c>double</c></summary>
    Double,
    /// <summary><c>groove</c></summary>
    Groove,
    /// <summary><c>ridge</c></summary>
    Ridge,
    /// <summary><c>inset</c></summary>
    Inset,
    /// <summary><c>outset</c></summary>
    Outset,
    /// <summary><c>hidden</c></summary>
    Hidden,
}
