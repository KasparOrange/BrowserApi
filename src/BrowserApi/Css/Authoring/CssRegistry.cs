using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Discovers all <see cref="StyleSheet"/>-derived types in the loaded
/// <see cref="AppDomain"/>, renders each into CSS, and exposes the combined
/// result for consumption by the Blazor <c>BrowserApiCss</c> component or any
/// other consumer.
/// </summary>
/// <remarks>
/// <para>
/// The registry is the auto-discovery mechanism that keeps the consumer
/// experience to "declare a class, see it work." Users never call
/// <see cref="StyleSheet.Render(System.Type)"/> directly — they place
/// <c>&lt;BrowserApiCss /&gt;</c> in their <c>App.razor</c> and the registry
/// handles the rest at first reference.
/// </para>
/// <para>
/// Discovery is lazy and idempotent — the first call to any public method
/// (or any <c>Class.Name</c>/<c>Class.Selector</c> access) triggers a single
/// AppDomain scan. Newly-loaded assemblies after the initial scan are picked
/// up if <see cref="Refresh"/> is called explicitly, otherwise the cached
/// result is reused.
/// </para>
/// <para>
/// <strong>Override slot.</strong> The rendered output can be replaced
/// wholesale via <see cref="SetOverride"/>. This is how the dev-time island
/// compiler (<c>BrowserApi.Css.Compiler</c>) pushes freshly compiled CSS into a
/// running app without touching the app's own (immutable, cctor-initialized)
/// stylesheet fields: class <em>names</em> never change for a given field, so
/// the app's <c>AppStyles.Btn</c> references stay valid while the CSS text behind
/// them is swapped. Consumers observe swaps through <see cref="Changed"/>.
/// </para>
/// <para>
/// Thread-safety: the registry uses concurrent collections and double-checked
/// locking so concurrent first-access from multiple Blazor circuits is safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // The user-facing path is just to render &lt;BrowserApiCss /&gt; somewhere in
/// // App.razor's &lt;HeadContent&gt;. But for tests or non-Blazor scenarios:
/// var css = CssRegistry.GetCombinedCss();
/// </code>
/// </example>
public static class CssRegistry {
    /// <summary>
    /// Assembly-name prefix of throwaway island assemblies emitted by
    /// <c>BrowserApi.Css.Compiler</c>. The AppDomain scan skips them — they hold
    /// <em>copies</em> of the app's stylesheets, and counting both would
    /// duplicate every rule in the combined output.
    /// </summary>
    public const string IslandAssemblyPrefix = "BrowserApi.CssIsland.";

    private static readonly object _scanLock = new();
    private static readonly ConcurrentDictionary<Type, string> _rendered = new();
    private static Type[] _order = Array.Empty<Type>();
    private static volatile bool _scanned;
    private static string? _combined;
    private static CssOptions _options = new();
    private static IReadOnlyDictionary<string, string>? _override;

    /// <summary>The configured runtime options (global prefix, etc.). Set
    /// before the first scan via <c>AddBrowserApiCss(css =&gt; ...)</c>.</summary>
    public static CssOptions Options => _options;

    /// <summary>
    /// Raised whenever the CSS a consumer would receive may have changed: after
    /// <see cref="Refresh"/>, <see cref="Configure"/>, and <see cref="SetOverride"/>.
    /// <c>BrowserApiCss</c> subscribes to re-render its <c>&lt;style&gt;</c> tag live.
    /// </summary>
    public static event EventHandler? Changed;

    /// <summary>Replaces the runtime options. Triggers a refresh so any
    /// previously-cached output is regenerated under the new options.</summary>
    public static void Configure(CssOptions options) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Refresh();
    }

    /// <summary>
    /// Renders every <see cref="StyleSheet"/>-derived type in the AppDomain into
    /// a single CSS document. Idempotent — subsequent calls return the cached result.
    /// When an override is active, returns the override's combined document instead.
    /// </summary>
    public static string GetCombinedCss() {
        var ov = _override;
        if (ov is not null) return Combine(ov.Values);
        EnsureScanned();
        return _combined ?? RebuildCombined();
    }

    /// <summary>
    /// Returns the rendered CSS for a single stylesheet type, scanning the
    /// AppDomain first if needed. Use this when you only need one stylesheet's
    /// output (e.g. for testing or selective serving). Honors the override slot,
    /// matched by <see cref="Type.FullName"/>.
    /// </summary>
    public static string GetCss(Type styleSheetType) {
        if (styleSheetType is null) throw new ArgumentNullException(nameof(styleSheetType));
        var ov = _override;
        if (ov is not null) {
            return ov.TryGetValue(styleSheetType.FullName ?? styleSheetType.Name, out var o) ? o : string.Empty;
        }
        EnsureScanned();
        return _rendered.TryGetValue(styleSheetType, out var css) ? css : string.Empty;
    }

    /// <summary>Strongly-typed convenience: <c>CssRegistry.GetCss&lt;AppStyles&gt;()</c>.</summary>
    public static string GetCss<T>() where T : StyleSheet => GetCss(typeof(T));

    /// <summary>
    /// Replaces (or, with <c>null</c>, clears) the registry's output with CSS
    /// rendered elsewhere — keyed by stylesheet <see cref="Type.FullName"/>, the
    /// same shape <see cref="StyleSheet.RenderAll"/> returns. Raises <see cref="Changed"/>.
    /// </summary>
    /// <remarks>
    /// Intended for the dev-time live loop only. The override is a snapshot: a
    /// later <see cref="Refresh"/> does not clear it, because the island output
    /// is by definition newer than anything the app's own assemblies can render.
    /// </remarks>
    public static void SetOverride(IReadOnlyDictionary<string, string>? cssByType) {
        _override = cssByType;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>True while <see cref="SetOverride"/> output is being served.</summary>
    public static bool HasOverride => _override is not null;

    /// <summary>
    /// Forces re-scanning of the AppDomain. Useful for hot-reload scenarios
    /// where new <see cref="StyleSheet"/> types may have been loaded after the
    /// initial scan, or for tests that introduce types dynamically.
    /// </summary>
    public static void Refresh() {
        lock (_scanLock) {
            _rendered.Clear();
            _combined = null;
            _scanned = false;
            // Reset all field names too so the next scan re-prefixes them
            // under the (possibly new) options. Without this, names set by a
            // previous scan would survive a reconfigure and Refresh would
            // be a no-op for prefix changes.
            ClearFieldNames();
            ScanAndRenderAll();
        }
        Changed?.Invoke(null, EventArgs.Empty);
    }

    private static void ClearFieldNames() {
        foreach (var type in DiscoverStyleSheetTypes()) {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)) {
                var value = field.GetValue(null);
                switch (value) {
                    case Class cls: cls.Name = string.Empty; break;
                    case Keyframes kf: kf.Name = string.Empty; break;
                    default: {
                        var t = value?.GetType();
                        if (t is not null && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CssVar<>)) {
                            var nameProp = t.GetProperty(nameof(CssVar<Common.ICssValue>.Name));
                            // External-named variables (CssVar.External) carry their literal
                            // name in the same field, so reset only when the name looks like
                            // one we previously assigned (kebab-cased, leading "--").
                            var existing = (string?)nameProp?.GetValue(value);
                            if (existing is not null && existing.StartsWith("--", StringComparison.Ordinal)) {
                                nameProp?.SetValue(value, string.Empty);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Triggers the AppDomain scan if it hasn't run yet. Lightweight no-op afterward.
    /// Called implicitly by every other public method on this class and by
    /// <see cref="Class"/>'s implicit conversions when a name hasn't been resolved yet.
    /// </summary>
    public static void EnsureScanned() {
        if (_scanned) return;
        lock (_scanLock) {
            if (_scanned) return;
            ScanAndRenderAll();
        }
    }

    /// <summary>Returns every discovered stylesheet type. Useful for diagnostics.</summary>
    public static IReadOnlyCollection<Type> DiscoveredStyleSheets {
        get {
            EnsureScanned();
            return _rendered.Keys.ToArray();
        }
    }

    /// <summary>
    /// Concatenates per-stylesheet CSS documents into one, guaranteeing a
    /// trailing newline between them. Shared by the registry and by mode-3
    /// file writers so a file on disk and the runtime <c>&lt;style&gt;</c> body are
    /// byte-identical for the same input.
    /// </summary>
    public static string Combine(IEnumerable<string> cssDocuments) {
        var sb = new StringBuilder();
        foreach (var css in cssDocuments) {
            sb.Append(css);
            if (!css.EndsWith("\n", StringComparison.Ordinal)) sb.Append('\n');
        }
        return sb.ToString();
    }

    // ─────────────────────────────────── Internals ──────────────────────────────────

    private static void ScanAndRenderAll() {
        // Mark scan complete BEFORE any rendering so re-entrant calls from
        // within Render (e.g. CssVar.ToCss → lazy ToScan) see the flag and
        // short-circuit instead of recursing infinitely. RenderAll's two-pass
        // design populates all names first, so by the time we render anything
        // every cross-stylesheet reference is resolvable.
        _scanned = true;

        var styleSheetTypes = DiscoverStyleSheetTypes();
        var rendered = StyleSheet.RenderAll(styleSheetTypes);
        foreach (var type in styleSheetTypes) {
            _rendered.TryAdd(type, rendered[type.FullName ?? type.Name]);
        }
        _order = styleSheetTypes.ToArray();

        _combined = null; // mark for rebuild on next read
    }

    private static List<Type> DiscoverStyleSheetTypes() {
        var styleSheetTypes = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            if (asm.IsDynamic) continue;
            var name = asm.GetName().Name;
            if (name is null) continue;
            if (name.StartsWith("System.", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.", StringComparison.Ordinal) ||
                name.StartsWith(IslandAssemblyPrefix, StringComparison.Ordinal) ||
                name == "mscorlib" || name == "netstandard") {
                continue;
            }
            foreach (var type in SafeGetTypes(asm)) {
                if (type is null) continue;
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                if (!typeof(StyleSheet).IsAssignableFrom(type)) continue;
                styleSheetTypes.Add(type);
            }
        }
        // Assembly load order differs between processes (app vs. CLI vs. test
        // host), so sort by full name: the combined document must be the same
        // bytes wherever it is rendered or mode-3 drift detection would fire
        // on every start.
        styleSheetTypes.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
        return styleSheetTypes;
    }

    private static string RebuildCombined() {
        var result = Combine(_order.Select(t => _rendered[t]));
        _combined = result;
        return result;
    }

    private static IEnumerable<Type?> SafeGetTypes(Assembly asm) {
        try {
            return asm.GetTypes();
        } catch (ReflectionTypeLoadException ex) {
            // Some assemblies (e.g. with optional dependencies missing) throw on
            // GetTypes(). The Types property still has the loadable subset.
            return ex.Types;
        } catch {
            return Array.Empty<Type?>();
        }
    }
}
