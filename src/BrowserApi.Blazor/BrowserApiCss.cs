using BrowserApi.Css.Authoring;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BrowserApi.Blazor;

/// <summary>
/// Drop-in Blazor component that emits all C#-authored stylesheets in the
/// AppDomain as a single inline <c>&lt;style&gt;</c> tag. Place it in your
/// <c>App.razor</c> (typically inside <c>&lt;HeadContent&gt;</c>) and the
/// CSS for every <see cref="StyleSheet"/>-derived class in the app appears
/// in the rendered HTML.
/// </summary>
/// <remarks>
/// <para>
/// On first render, <see cref="CssRegistry.EnsureScanned"/> walks the
/// AppDomain, discovers every <see cref="StyleSheet"/> subclass, and renders
/// their CSS. Subsequent renders reuse the cached output. The cost is paid
/// once per app lifetime (or per circuit warm-up), not per render.
/// </para>
/// <para>
/// For production deployments with many stylesheets, prefer the source
/// generator path (compile-time emission to a static asset) once it ships;
/// this component is the immediate "just works" path. Both paths can coexist —
/// the source-gen path fills <see cref="CssRegistry"/> at compile time and
/// the component renders whatever's there.
/// </para>
/// </remarks>
/// <example>
/// <code language="razor">
/// &lt;!-- App.razor --&gt;
/// &lt;Router AppAssembly="@typeof(Program).Assembly"&gt;
///     &lt;Found Context="routeData"&gt;
///         &lt;HeadContent&gt;
///             &lt;BrowserApiCss /&gt;
///         &lt;/HeadContent&gt;
///         &lt;RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" /&gt;
///     &lt;/Found&gt;
/// &lt;/Router&gt;
/// </code>
/// </example>
public sealed class BrowserApiCss : ComponentBase {
    /// <summary>
    /// When <see langword="true"/> (the default), the component scans the AppDomain
    /// for stylesheets eagerly during the first render. Set to <see langword="false"/>
    /// in tests or specialized scenarios where discovery should be deferred.
    /// </summary>
    [Parameter] public bool EagerScan { get; set; } = true;

    /// <summary>
    /// Optional list of stylesheet types to render. If empty (default), every
    /// <see cref="StyleSheet"/> subclass discovered in the AppDomain is rendered.
    /// Useful when an app needs to scope CSS to a particular feature area.
    /// </summary>
    [Parameter] public System.Type[]? Only { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        if (EagerScan) CssRegistry.EnsureScanned();

        var css = Only is { Length: > 0 }
            ? RenderOnly(Only)
            : CssRegistry.GetCombinedCss();

        builder.OpenElement(0, "style");
        builder.AddAttribute(1, "data-source", "BrowserApiCss");
        builder.AddMarkupContent(2, css);
        builder.CloseElement();
    }

    private static string RenderOnly(System.Type[] types) {
        var sb = new System.Text.StringBuilder();
        foreach (var t in types) {
            sb.Append(CssRegistry.GetCss(t));
            if (sb.Length > 0 && sb[sb.Length - 1] != '\n') sb.Append('\n');
        }
        return sb.ToString();
    }
}
