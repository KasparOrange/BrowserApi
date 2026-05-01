using BrowserApi.JSInterop;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApi.Blazor;

/// <summary>
/// Extension methods for registering BrowserApi services with a dependency injection container.
/// </summary>
/// <remarks>
/// These extensions are the primary entry point for integrating BrowserApi into a
/// Blazor application's service configuration. Call <see cref="AddBrowserApi"/> in your
/// <c>Program.cs</c> to register the required services.
/// </remarks>
/// <seealso cref="JSInteropBackend"/>
/// <seealso cref="BrowserApiComponentBase"/>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Registers the BrowserApi services, including the <see cref="JSInteropBackend"/>,
    /// with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The <see cref="JSInteropBackend"/> is registered with a <b>scoped</b> lifetime,
    /// which is the standard lifetime for Blazor services that depend on
    /// <c>IJSRuntime</c>. In Blazor WebAssembly, scoped is effectively singleton;
    /// in Blazor Server, each circuit gets its own instance.
    /// </para>
    /// <para>
    /// After calling this method, inherit from <see cref="BrowserApiComponentBase"/> in
    /// your components to get automatic backend initialization and access to the
    /// <c>Window</c> and <c>Document</c> objects.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs:
    /// var builder = WebAssemblyHostBuilder.CreateDefault(args);
    /// builder.Services.AddBrowserApi();
    /// await builder.Build().RunAsync();
    /// </code>
    /// </example>
    public static IServiceCollection AddBrowserApi(this IServiceCollection services) {
        services.AddScoped<JSInteropBackend>();
        return services;
    }

    /// <summary>
    /// Eagerly scans the AppDomain for C#-authored stylesheets (subclasses of
    /// <see cref="BrowserApi.Css.Authoring.StyleSheet"/>) and renders their CSS into
    /// the static <see cref="BrowserApi.Css.Authoring.CssRegistry"/>. Optional —
    /// the registry self-initializes lazily on first access — but calling this
    /// during DI configuration moves the cost out of the request path.
    /// </summary>
    /// <param name="services">The service collection (returned unchanged for chaining).</param>
    /// <returns>The same <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// <para>
    /// Pair this with <c>&lt;BrowserApiCss /&gt;</c> in your <c>App.razor</c> to get
    /// a fully wired CSS-in-C# pipeline:
    /// </para>
    /// <code language="csharp">
    /// // Program.cs
    /// builder.Services.AddBrowserApi();
    /// builder.Services.AddBrowserApiCss();
    /// </code>
    /// <code language="razor">
    /// &lt;!-- App.razor --&gt;
    /// &lt;HeadContent&gt;&lt;BrowserApiCss /&gt;&lt;/HeadContent&gt;
    /// </code>
    /// </remarks>
    public static IServiceCollection AddBrowserApiCss(this IServiceCollection services) {
        BrowserApi.Css.Authoring.CssRegistry.EnsureScanned();
        return services;
    }
}
