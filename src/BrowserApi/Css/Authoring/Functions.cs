using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Top-level CSS function helpers — <c>url()</c>, <c>env()</c>, content
/// strings, and other functions that don't naturally belong to a specific
/// value type's API surface.
/// </summary>
/// <remarks>
/// Spec §17. The factories live on <c>CssFn</c> rather than <c>Css</c>
/// because <c>Css</c> conflicts with namespace lookup — see spec §9 on
/// drifting from CSS terminology when a clearer C# name exists.
/// </remarks>
public static class CssFn {
    /// <summary>Builds <c>url("path")</c> from a string path.</summary>
    public static UrlValue Url(string path) => new($"url(\"{path}\")");

    /// <summary>Builds <c>url("data:mime;base64,...")</c> for inline data URIs.</summary>
    public static UrlValue DataUrl(string mime, string base64) =>
        new($"url(\"data:{mime};base64,{base64}\")");

    /// <summary>Builds a <c>content</c>-property string literal value
    /// (<c>content: "→"</c>) — wraps the given string in CSS-escaped quotes.</summary>
    public static StringValue String(string content) =>
        new($"\"{content.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");

    /// <summary>Builds an <c>env(name)</c> reference for safe-area insets,
    /// keyboard inset, etc.</summary>
    public static EnvValue Env(string name) => new($"env({name})");

    /// <summary>Pre-built env() references for the common safe-area insets.</summary>
    public static class SafeArea {
        /// <summary><c>env(safe-area-inset-top)</c>.</summary>
        public static EnvValue Top { get; } = Env("safe-area-inset-top");
        /// <summary><c>env(safe-area-inset-right)</c>.</summary>
        public static EnvValue Right { get; } = Env("safe-area-inset-right");
        /// <summary><c>env(safe-area-inset-bottom)</c>.</summary>
        public static EnvValue Bottom { get; } = Env("safe-area-inset-bottom");
        /// <summary><c>env(safe-area-inset-left)</c>.</summary>
        public static EnvValue Left { get; } = Env("safe-area-inset-left");
    }
}

/// <summary>A wrapper around a <c>url(...)</c> value. Implicitly converts
/// to <see cref="ICssValue"/> for property assignment.</summary>
public readonly struct UrlValue : ICssValue {
    private readonly string _css;
    /// <summary>Wraps a pre-rendered url() expression.</summary>
    public UrlValue(string css) { _css = css; }
    /// <inheritdoc/>
    public string ToCss() => _css;
}

/// <summary>A wrapper around a CSS string literal value (e.g. for <c>content</c>).</summary>
public readonly struct StringValue : ICssValue {
    private readonly string _css;
    /// <summary>Wraps a pre-rendered quoted string.</summary>
    public StringValue(string css) { _css = css; }
    /// <inheritdoc/>
    public string ToCss() => _css;
}

/// <summary>A wrapper around an <c>env(name)</c> value. Implicitly converts to
/// <see cref="Length"/> since env() is most commonly used in length contexts
/// (safe-area insets).</summary>
public readonly struct EnvValue : ICssValue {
    private readonly string _css;
    /// <summary>Wraps a pre-rendered env() expression.</summary>
    public EnvValue(string css) { _css = css; }
    /// <inheritdoc/>
    public string ToCss() => _css;

    /// <summary>Implicit conversion to <see cref="Length"/> for use in length-typed setters.</summary>
    public static implicit operator Length(EnvValue env) => new(env._css);

    /// <summary>Implicit conversion to <see cref="LengthOrPercentage"/>.</summary>
    public static implicit operator LengthOrPercentage(EnvValue env) => new(env._css);
}
