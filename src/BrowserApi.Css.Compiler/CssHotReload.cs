using BrowserApi.Css.Authoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrowserApi.Css.Compiler;

/// <summary>
/// Registers the in-app CSS live loop. Normally you never call this:
/// <c>AddBrowserApiCss(css =&gt; css.HotReload = true)</c> finds this package by
/// reflection and calls it for you. It is public for hosts that wire services
/// by hand.
/// </summary>
public static class CssHotReloadExtensions {
    /// <summary>
    /// Watches the project's <c>*.css.cs</c> island and, on every save, recompiles
    /// it in-process and swaps the rendered CSS into <see cref="CssRegistry"/>.
    /// <c>&lt;BrowserApiCss /&gt;</c> observes <see cref="CssRegistry.Changed"/> and
    /// re-renders its <c>&lt;style&gt;</c> tag — no page reload, no rebuild, circuit
    /// state preserved. Reads <see cref="CssOptions.AllowAppReferences"/> from
    /// <see cref="CssRegistry.Options"/>. Active only when the host environment is
    /// Development; elsewhere the service starts and immediately does nothing.
    /// </summary>
    /// <param name="projectDirectory">Root of the island; default: the host's content root (the project dir under <c>dotnet run</c>).</param>
    public static IServiceCollection AddBrowserApiCssHotReload(this IServiceCollection services, string? projectDirectory = null) {
        services.AddSingleton(new CssHotReloadSettings(projectDirectory));
        services.AddHostedService<CssHotReloadService>();
        return services;
    }
}

internal sealed record CssHotReloadSettings(string? ProjectDirectory);

/// <summary>Hosted service that owns the watcher for the app's lifetime.</summary>
internal sealed class CssHotReloadService : IHostedService, IDisposable {
    private readonly CssHotReloadSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly ILogger<CssHotReloadService> _log;
    private CssIslandWatcher? _watcher;

    public CssHotReloadService(CssHotReloadSettings settings, IHostEnvironment env, ILogger<CssHotReloadService> log) {
        _settings = settings;
        _env = env;
        _log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        // Safe to leave HotReload = true in committed Program.cs: only Development
        // pays for Roslyn and a file watcher.
        if (!_env.IsDevelopment()) return Task.CompletedTask;

        var dir = _settings.ProjectDirectory ?? _env.ContentRootPath;
        if (!Directory.Exists(dir)) {
            _log.LogWarning("BrowserApi CSS hot reload: project directory '{Dir}' not found; live loop disabled.", dir);
            return Task.CompletedTask;
        }

        var options = CssRegistry.Options;
        var compiler = new CssIslandCompiler(options.AllowAppReferences ? CssIslandCompiler.AppReferences() : null);
        _watcher = new CssIslandWatcher(dir, compiler);
        _watcher.Compiled += OnCompiled;
        _watcher.Faulted += ex => _log.LogError(ex, "BrowserApi CSS hot reload: island compile crashed.");
        _watcher.Start();

        // Compile once at startup so a broken island is reported immediately
        // rather than on the first save. The result is intentionally NOT pushed
        // as an override: the app's own render is identical, and leaving the
        // override empty keeps HasOverride meaningful ("something was swapped").
        var initial = compiler.Compile(CssIsland.FindFiles(dir));
        if (!initial.Success) {
            LogDiagnostics(initial);
        }
        _log.LogInformation("BrowserApi CSS hot reload watching {Dir} ({Count} stylesheet(s){AppRefs}).",
            dir, initial.CssByType.Count, options.AllowAppReferences ? ", app references allowed" : "");
        return Task.CompletedTask;
    }

    private void OnCompiled(CssIslandResult result) {
        if (!result.Success) {
            LogDiagnostics(result);
            return; // keep serving the last good CSS
        }
        CssRegistry.SetOverride(result.CssByType);
        _log.LogInformation("BrowserApi CSS hot reload: swapped {Count} stylesheet(s).", result.CssByType.Count);
    }

    private void LogDiagnostics(CssIslandResult result) {
        foreach (var d in result.Diagnostics) {
            _log.LogWarning("BrowserApi CSS island: {Diagnostic}", d.ToString());
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() {
        _watcher?.Dispose();
        _watcher = null;
    }
}
