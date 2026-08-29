using System.Security.Cryptography;
using BrowserApi.Css.Authoring;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrowserApi.Blazor;

/// <summary>
/// Drop-in Blazor component that delivers all C#-authored stylesheets to the
/// page. Place it in your <c>App.razor</c> (typically inside
/// <c>&lt;HeadContent&gt;</c>). Delivery: one inline <c>&lt;style&gt;</c> tag by
/// default, or a <c>&lt;link&gt;</c> to a build-time static asset when
/// <see cref="CssOptions.StaticAssetPath"/> is set.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Inline mode.</strong> On first render, <see cref="CssRegistry.EnsureScanned"/>
/// walks the AppDomain, discovers every <see cref="StyleSheet"/> subclass, and
/// renders their CSS. Subsequent renders reuse the cached output. The component
/// also subscribes to <see cref="CssRegistry.Changed"/> — when the dev-time
/// island compiler (<c>AddBrowserApiCssHotReload</c>) swaps in fresh CSS, the
/// <c>&lt;style&gt;</c> body is re-rendered through the normal Blazor diff: no
/// page reload, circuit state intact.
/// </para>
/// <para>
/// <strong>Static-asset mode.</strong> Emits <c>&lt;link rel="stylesheet"&gt;</c>
/// to <see cref="CssOptions.StaticAssetPath"/> with a content-hash query string
/// for cache busting. Before trusting the file, the component verifies once
/// per process that its bytes equal what the loaded assemblies render. A
/// mismatch (stale build output, prefix configured differently than the CLI
/// was invoked with) falls back to inline delivery and logs a warning — wrong
/// styles are never served silently.
/// </para>
/// </remarks>
/// <example>
/// <code language="razor">
/// &lt;!-- App.razor --&gt;
/// &lt;HeadContent&gt;
///     &lt;BrowserApiCss /&gt;
/// &lt;/HeadContent&gt;
/// </code>
/// </example>
public sealed class BrowserApiCss : ComponentBase, IDisposable {
    // One verdict per process: the file and the assemblies don't change while
    // the app runs, so checking per circuit would only add disk reads.
    private static readonly object _driftLock = new();
    private static bool? _staticAssetValid;
    private static string _staticAssetVersion = string.Empty;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// When <see langword="true"/> (the default), the component scans the AppDomain
    /// for stylesheets eagerly during the first render. Set to <see langword="false"/>
    /// in tests or specialized scenarios where discovery should be deferred.
    /// </summary>
    [Parameter] public bool EagerScan { get; set; } = true;

    /// <summary>
    /// Restrict output to these stylesheet types instead of every discovered one.
    /// Inline mode only — a static asset always carries the full document.
    /// </summary>
    [Parameter] public System.Type[]? Only { get; set; }

    protected override void OnInitialized() {
        CssRegistry.Changed += OnCssChanged;
    }

    private void OnCssChanged(object? sender, EventArgs e) {
        // Raised from the watcher's thread-pool thread; marshal onto the renderer.
        _ = InvokeAsync(StateHasChanged);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        if (EagerScan) CssRegistry.EnsureScanned();

        var options = CssRegistry.Options;
        if (!string.IsNullOrEmpty(options.StaticAssetPath) && Only is not { Length: > 0 } && StaticAssetIsValid(options)) {
            builder.OpenElement(0, "link");
            builder.AddAttribute(1, "rel", "stylesheet");
            builder.AddAttribute(2, "href", "/" + options.StaticAssetPath!.TrimStart('/') + "?v=" + _staticAssetVersion);
            builder.AddAttribute(3, "data-source", "BrowserApiCss");
            builder.CloseElement();
            return;
        }

        var css = Only is { Length: > 0 }
            ? RenderOnly(Only)
            : CssRegistry.GetCombinedCss();

        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "data-source", "BrowserApiCss");
        builder.AddMarkupContent(2, css);
        builder.CloseElement();
    }

    private bool StaticAssetIsValid(CssOptions options) {
        // A hot-reload override means the file on disk is, by definition, older
        // than what the developer just saved — serve inline while it's active.
        if (CssRegistry.HasOverride) return false;

        lock (_driftLock) {
            if (_staticAssetValid is bool cached) return cached;

            var log = Services.GetService<ILoggerFactory>()?.CreateLogger<BrowserApiCss>();
            var env = Services.GetService<IHostEnvironment>();
            if (env is null) {
                log?.LogWarning("BrowserApiCss: StaticAsset mode needs IHostEnvironment to locate wwwroot; serving inline instead.");
                return (_staticAssetValid = false).Value;
            }

            var path = Path.Combine(env.ContentRootPath, "wwwroot", options.StaticAssetPath!.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) {
                log?.LogWarning("BrowserApiCss: static asset '{Path}' not found (run `browserapi-css build`); serving inline instead.", path);
                return (_staticAssetValid = false).Value;
            }

            var expected = CssRegistry.GetCombinedCss();
            var actual = File.ReadAllText(path);
            if (!string.Equals(Normalize(actual), Normalize(expected), StringComparison.Ordinal)) {
                log?.LogWarning("BrowserApiCss: static asset '{Path}' does not match the CSS rendered from the loaded assemblies (stale build or different GlobalPrefix); serving inline instead.", path);
                return (_staticAssetValid = false).Value;
            }

            _staticAssetVersion = ShortHash(expected);
            log?.LogInformation("BrowserApiCss: serving static asset '{Path}' (v={Version}).", options.StaticAssetPath, _staticAssetVersion);
            return (_staticAssetValid = true).Value;
        }
    }

    private static string Normalize(string css) => css.Replace("\r\n", "\n").TrimEnd();

    private static string ShortHash(string text) {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Normalize(text)));
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
    }

    private static string RenderOnly(System.Type[] types) {
        var sb = new System.Text.StringBuilder();
        foreach (var t in types) {
            sb.Append(CssRegistry.GetCss(t));
            if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append('\n');
        }
        return sb.ToString();
    }

    public void Dispose() {
        CssRegistry.Changed -= OnCssChanged;
    }
}
