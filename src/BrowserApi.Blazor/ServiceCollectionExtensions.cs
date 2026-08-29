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
    /// Wires CSS-in-C#. This is <strong>the one place it is configured</strong>:
    /// prefix, static-asset delivery, live reload, island rules — see
    /// <see cref="BrowserApi.Css.Authoring.CssOptions"/> for every option and
    /// the "Configuration" section of the CSS-in-C# guide for the reasoning.
    /// Pair with <c>&lt;BrowserApiCss /&gt;</c> in <c>App.razor</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applies the options, eagerly scans the AppDomain for stylesheets so the
    /// render cost leaves the request path, and — when <c>css.HotReload</c> is set
    /// and the <c>BrowserApi.Css.Compiler</c> package is referenced — registers the
    /// dev live loop. The Compiler package is located by reflection so this
    /// package carries no Roslyn dependency; if it isn't referenced, a warning is
    /// logged at startup and everything else works.
    /// </para>
    /// <para>
    /// Values that build-time tools need (<c>GlobalPrefix</c>, <c>StaticAssetPath</c>,
    /// <c>AllowAppReferences</c>) must be literals or consts in the lambda; the
    /// source generator copies them into the assembly for the <c>browserapi-css</c>
    /// tool. Analyzer BCA005 warns otherwise.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddBrowserApiCss(css => {
    ///     css.GlobalPrefix    = "mw";
    ///     css.StaticAssetPath = "css/app.css";
    ///     css.HotReload       = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBrowserApiCss(
        this IServiceCollection services,
        System.Action<BrowserApi.Css.Authoring.CssOptions>? configure = null) {
        var options = new BrowserApi.Css.Authoring.CssOptions();
        configure?.Invoke(options);
        BrowserApi.Css.Authoring.CssRegistry.Configure(options);

        if (options.HotReload) {
            RegisterHotReload(services);
        }
        return services;
    }

    /// <summary>
    /// Finds <c>BrowserApi.Css.Compiler.CssHotReloadExtensions.AddBrowserApiCssHotReload</c>
    /// without a compile-time reference. A missing package is a configuration
    /// slip, not a crash: register a startup warning instead.
    /// </summary>
    private static void RegisterHotReload(IServiceCollection services) {
        System.Reflection.MethodInfo? method = null;
        try {
            var asm = System.Reflection.Assembly.Load("BrowserApi.Css.Compiler");
            method = asm.GetType("BrowserApi.Css.Compiler.CssHotReloadExtensions")
                ?.GetMethod("AddBrowserApiCssHotReload", new[] { typeof(IServiceCollection), typeof(string) });
        } catch (System.IO.FileNotFoundException) {
            // not referenced
        }

        if (method is null) {
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => {
                var log = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("BrowserApi.Css");
                Microsoft.Extensions.Logging.LoggerExtensions.LogWarning(log!,
                    "AddBrowserApiCss: HotReload = true but the BrowserApi.Css.Compiler package is not referenced; live reload is off. Add <PackageReference Include=\"BrowserApi.Css.Compiler\" /> (dev-time dependency).");
                return new NoopHostedService();
            });
            return;
        }
        method.Invoke(null, new object?[] { services, null });
    }

    private sealed class NoopHostedService : Microsoft.Extensions.Hosting.IHostedService {
        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.CompletedTask;
    }
}
