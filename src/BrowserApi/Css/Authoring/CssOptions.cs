namespace BrowserApi.Css.Authoring;

/// <summary>
/// Runtime configuration for the CSS-in-C# authoring pipeline. Configure via
/// <c>builder.Services.AddBrowserApiCss(opts =&gt; opts.GlobalPrefix = "mw")</c>.
/// The eventual source-gen path (spec §20) will read these from MSBuild
/// properties and a <c>Program.cs</c> single-place declaration; this class is
/// the runtime equivalent.
/// </summary>
/// <remarks>
/// Options are captured as a static singleton inside <see cref="CssRegistry"/>.
/// They must be set before the first registry scan, which is why the eager
/// <c>AddBrowserApiCss(...)</c> call site is the recommended place — it
/// applies options THEN triggers the scan.
/// </remarks>
public sealed class CssOptions {
    /// <summary>
    /// A project-wide prefix applied to every CSS class and animation-name
    /// emitted by every <see cref="StyleSheet"/>. Useful for scoping all
    /// project-emitted CSS away from third-party CSS in the same page
    /// (no specificity conflicts, no accidental class-name collisions).
    /// Empty string disables prefixing.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddBrowserApiCss(opts =&gt; opts.GlobalPrefix = "mw");
    /// // Class field "Card" → ".mw-card"
    /// </code>
    /// </example>
    public string GlobalPrefix { get; set; } = string.Empty;
}
