namespace BrowserApi.Css.Authoring;

/// <summary>
/// The single configuration surface for CSS-in-C#. Set it once in
/// <c>Program.cs</c> — <c>builder.Services.AddBrowserApiCss(css =&gt; { … })</c> —
/// and every consumer (runtime rendering, dev live reload, the build-time
/// static emitter, the analyzers) reads the same values.
/// </summary>
/// <remarks>
/// <para>
/// <strong>How build-time tools see runtime config.</strong> Source generators
/// and the <c>browserapi-css</c> tool run before <c>Program.cs</c> ever
/// executes. To keep one place of truth anyway, <c>BrowserApi.Css.SourceGen</c>
/// reads the lambda during the app's normal build, takes every assignment whose
/// right-hand side is a constant, and stamps the values into the assembly as
/// <see cref="BrowserApiCssConfigAttribute"/>. The static emitter reads that
/// attribute from the built DLL. Consequence: values that must reach build-time
/// tools (<see cref="GlobalPrefix"/>, <see cref="StaticAssetPath"/>,
/// <see cref="AllowAppReferences"/>) have to be literals or <c>const</c>s in the
/// lambda. A non-constant still works at runtime; analyzer BCA005 warns that the
/// build won't see it.
/// </para>
/// <para>
/// Options are captured as a static singleton inside <see cref="CssRegistry"/>.
/// They must be set before the first registry scan, which is why the eager
/// <c>AddBrowserApiCss(...)</c> call site is the recommended place — it
/// applies options THEN triggers the scan.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddBrowserApiCss(css => {
///     css.GlobalPrefix    = "mw";
///     css.StaticAssetPath = "css/app.css";   // set ⇒ emitted on build, served via &lt;link&gt;
///     css.HotReload       = true;            // Development only; ignored elsewhere
/// });
/// </code>
/// </example>
public sealed class CssOptions {
    /// <summary>
    /// A project-wide prefix applied to every CSS class, variable, and
    /// animation name emitted by every <see cref="StyleSheet"/>. Scopes all
    /// project CSS away from third-party CSS on the same page. Empty string
    /// disables prefixing. Per-stylesheet <see cref="PrefixAttribute"/> chains
    /// after it: <c>"mw"</c> + <c>[Prefix("sp")]</c> + field <c>PeopleList</c> →
    /// <c>.mw-sp-people-list</c>.
    /// </summary>
    public string GlobalPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Web-root-relative path of a static <c>.css</c> file, e.g. <c>"css/app.css"</c>.
    /// Setting it switches delivery from an inline <c>&lt;style&gt;</c> to a
    /// <c>&lt;link&gt;</c>: the build writes the file (via the <c>browserapi-css</c>
    /// tool, picked up automatically by the MSBuild target in
    /// <c>BrowserApi.Css.Compiler</c>) and <c>&lt;BrowserApiCss /&gt;</c> serves it
    /// with a content-hash query string. If the file is missing or does not
    /// match what the running assemblies render, the component falls back to
    /// inline and logs a warning — stale styles are never served silently.
    /// <c>null</c> (default) = inline delivery, no build step.
    /// </summary>
    public string? StaticAssetPath { get; set; }

    /// <summary>
    /// Enables the dev live loop: save a <c>.css.cs</c>, see it in the running
    /// app in ~100 ms without rebuild or page reload. Requires the
    /// <c>BrowserApi.Css.Compiler</c> package; honored only when the host
    /// environment is Development, so it is safe to leave <c>true</c> in
    /// committed code.
    /// </summary>
    public bool HotReload { get; set; }

    /// <summary>
    /// By default stylesheets are compiled as a closed island — they may reference
    /// <c>BrowserApi</c> and each other only — which is what makes live reload and
    /// the static build fast and MSBuild-free. Set <c>true</c> to let them also
    /// reference the app's own assembly (constants, enums, static helpers). The
    /// island then sees the <em>last built</em> app; a newly added constant is
    /// visible after the next normal build. Runtime-only values (DI services,
    /// per-user state) can never be read from a stylesheet — use
    /// <see cref="CssVar{T}"/> and set it on an element instead.
    /// </summary>
    public bool AllowAppReferences { get; set; }
}
