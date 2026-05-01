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
    private static readonly object _scanLock = new();
    private static readonly ConcurrentDictionary<Type, string> _rendered = new();
    private static volatile bool _scanned;
    private static string? _combined;
    private static CssOptions _options = new();

    /// <summary>The configured runtime options (global prefix, etc.). Set
    /// before the first scan via <c>AddBrowserApiCss(opts =&gt; ...)</c>.</summary>
    public static CssOptions Options => _options;

    /// <summary>Replaces the runtime options. Triggers a refresh so any
    /// previously-cached output is regenerated under the new options.</summary>
    public static void Configure(CssOptions options) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Refresh();
    }

    /// <summary>
    /// Renders every <see cref="StyleSheet"/>-derived type in the AppDomain into
    /// a single CSS document. Idempotent — subsequent calls return the cached result.
    /// </summary>
    public static string GetCombinedCss() {
        EnsureScanned();
        return _combined ?? RebuildCombined();
    }

    /// <summary>
    /// Returns the rendered CSS for a single stylesheet type, scanning the
    /// AppDomain first if needed. Use this when you only need one stylesheet's
    /// output (e.g. for testing or selective serving).
    /// </summary>
    public static string GetCss(Type styleSheetType) {
        if (styleSheetType is null) throw new ArgumentNullException(nameof(styleSheetType));
        EnsureScanned();
        return _rendered.TryGetValue(styleSheetType, out var css) ? css : string.Empty;
    }

    /// <summary>Strongly-typed convenience: <c>CssRegistry.GetCss&lt;AppStyles&gt;()</c>.</summary>
    public static string GetCss<T>() where T : StyleSheet => GetCss(typeof(T));

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
    }

    private static void ClearFieldNames() {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            if (asm.IsDynamic) continue;
            var asmName = asm.GetName().Name;
            if (asmName is null) continue;
            if (asmName.StartsWith("System.", StringComparison.Ordinal) ||
                asmName.StartsWith("Microsoft.", StringComparison.Ordinal) ||
                asmName == "mscorlib" || asmName == "netstandard") {
                continue;
            }
            foreach (var type in SafeGetTypes(asm)) {
                if (type is null) continue;
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                if (!typeof(StyleSheet).IsAssignableFrom(type)) continue;

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

    // ─────────────────────────────────── Internals ──────────────────────────────────

    private static void ScanAndRenderAll() {
        // Mark scan complete BEFORE any rendering so re-entrant calls from
        // within Render (e.g. CssVar.ToCss → lazy ToScan) see the flag and
        // short-circuit instead of recursing infinitely. The two-pass design
        // below populates all names first, so by the time we render anything
        // every cross-stylesheet reference is resolvable.
        _scanned = true;

        var styleSheetTypes = new System.Collections.Generic.List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
            if (asm.IsDynamic) continue;
            var name = asm.GetName().Name;
            if (name is null) continue;
            if (name.StartsWith("System.", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.", StringComparison.Ordinal) ||
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

        // Pass 1 — populate names on every Class / CssVar / Keyframes field
        // so cross-stylesheet references (`Padding = OtherStyles.Spacing`) can
        // serialize to `var(--spacing)` regardless of render order.
        foreach (var type in styleSheetTypes) {
            StyleSheet.PopulateFieldNames(type);
        }

        // Pass 2 — render each stylesheet to its CSS string.
        foreach (var type in styleSheetTypes) {
            _rendered.TryAdd(type, StyleSheet.Render(type));
        }

        _combined = null; // mark for rebuild on next read
    }

    private static string RebuildCombined() {
        var sb = new StringBuilder();
        foreach (var kvp in _rendered) {
            sb.Append(kvp.Value);
            if (!kvp.Value.EndsWith("\n", StringComparison.Ordinal)) sb.Append('\n');
        }
        var result = sb.ToString();
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
