using System.Collections.Generic;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// CSS <c>@font-face</c> declaration — registers a custom font with the browser.
/// Authored as a <c>static readonly</c> field on a stylesheet alongside
/// <see cref="Class"/>, <see cref="Rule"/>, and <see cref="CssVar{T}"/>; the
/// emitter discovers it by type.
/// </summary>
/// <example>
/// <code>
/// public class FontStyles : StyleSheet {
///     public static readonly FontFace Inter = new() {
///         Family = "Inter",
///         Src    = "url('/fonts/Inter.woff2') format('woff2')",
///         Weight = "400 700",          // variable-weight range
///         Style  = "normal",
///         Display = "swap",
///     };
/// }
/// </code>
/// </example>
/// <remarks>
/// All properties are <c>string</c>-typed in this MVP slice — typed wrappers for
/// font-family, font-weight ranges, and url()/format() pairs are queued. The
/// CSS produced is byte-equivalent to a hand-written <c>@font-face</c>.
/// </remarks>
public sealed class FontFace {
    private readonly List<KeyValuePair<string, string>> _props = new();

    /// <summary>The properties in source order.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Properties => _props;

    /// <summary>The CSS <c>font-family</c> property — name to register.</summary>
    public string Family { init => _props.Add(new("font-family", $"\"{value}\"")); }

    /// <summary>The CSS <c>src</c> property — typically <c>url('...')</c> or
    /// <c>url('...') format('woff2')</c>.</summary>
    public string Src { init => _props.Add(new("src", value)); }

    /// <summary>The CSS <c>font-weight</c> property — single weight or
    /// space-separated range (<c>"400 700"</c>) for variable fonts.</summary>
    public string Weight { init => _props.Add(new("font-weight", value)); }

    /// <summary>The CSS <c>font-style</c> property.</summary>
    public string Style { init => _props.Add(new("font-style", value)); }

    /// <summary>The CSS <c>font-display</c> property — typically
    /// <c>"swap"</c>, <c>"block"</c>, <c>"fallback"</c>, or <c>"optional"</c>.</summary>
    public string Display { init => _props.Add(new("font-display", value)); }

    /// <summary>The CSS <c>unicode-range</c> property — restrict the font to a
    /// subset of code points (e.g. <c>"U+0000-00FF"</c>).</summary>
    public string UnicodeRange { init => _props.Add(new("unicode-range", value)); }
}
