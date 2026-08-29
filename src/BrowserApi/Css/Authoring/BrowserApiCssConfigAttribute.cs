using System;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Build-time mirror of the constant values passed to <c>AddBrowserApiCss(css =&gt; …)</c>
/// in <c>Program.cs</c>. <strong>You don't write this attribute</strong> —
/// <c>BrowserApi.Css.SourceGen</c> emits it from the lambda so tools that run
/// before the app (the <c>browserapi-css</c> static emitter, analyzers) can read
/// the same configuration from the compiled assembly. Kept public so those
/// tools can bind to it and so it shows up in decompilers without mystery.
/// </summary>
/// <remarks>
/// Only assignments whose right-hand side is a compile-time constant make it
/// here (see <see cref="CssOptions"/> remarks). If the generator is not
/// referenced, the attribute is absent and build-time tools fall back to
/// defaults / explicit command-line flags.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class BrowserApiCssConfigAttribute : Attribute {
    /// <inheritdoc cref="CssOptions.GlobalPrefix"/>
    public string GlobalPrefix { get; set; } = string.Empty;

    /// <inheritdoc cref="CssOptions.StaticAssetPath"/>
    public string? StaticAssetPath { get; set; }

    /// <inheritdoc cref="CssOptions.AllowAppReferences"/>
    public bool AllowAppReferences { get; set; }

    /// <summary>Copies the captured values onto a fresh <see cref="CssOptions"/>.</summary>
    public CssOptions ToOptions() => new() {
        GlobalPrefix = GlobalPrefix,
        StaticAssetPath = StaticAssetPath,
        AllowAppReferences = AllowAppReferences,
    };
}
