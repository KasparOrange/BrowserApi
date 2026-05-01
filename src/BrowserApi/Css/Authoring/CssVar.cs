using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A CSS custom property — what most developers call a "CSS variable" — typed by
/// the value type it holds. Created via <c>new CssVar&lt;T&gt;(default)</c> or
/// <c>CssVar.External(name)</c>; referenced anywhere a value of <typeparamref name="T"/>
/// is expected (implicit conversion emits <c>var(--name)</c>).
/// </summary>
/// <remarks>
/// <para>
/// "Variable" matches developer mental models — see philosophy point §9 in the spec.
/// W3C calls this a "custom property"; we keep "property" reserved for regular CSS
/// properties (<c>color</c>, <c>display</c>) on the <see cref="Declarations"/> type.
/// </para>
/// <para>
/// In the source-gen path, a <see cref="CssVar{T}"/> field's name is derived from the
/// C# field identifier (PascalCase → kebab-case) and emitted as <c>--name</c> on the
/// stylesheet's chosen declaration scope (typically <c>:root</c>). The MVP ships the
/// type and the implicit conversion; default-emission and conditional overrides are
/// added when the source generator lands.
/// </para>
/// </remarks>
/// <typeparam name="T">The value type carried — typically <c>Length</c>, <c>CssColor</c>,
/// <c>Percentage</c>, <c>Angle</c>, etc.</typeparam>
/// <example>
/// <code>
/// public static partial class Tokens : StyleSheet {
///     public static readonly CssVar&lt;Length&gt; Radius = new(8.Px());
///     public static readonly CssVar&lt;CssColor&gt; Bg = new(CssColor.White);
/// }
///
/// // Used inside another stylesheet — emits var(--radius) etc.:
/// public static readonly Class Card = new() {
///     BorderRadius = Tokens.Radius,
///     Background = Tokens.Bg,
/// };
/// </code>
/// </example>
public sealed class CssVar<T> : ICssValue where T : ICssValue {
    /// <summary>The default value, emitted on the stylesheet's <c>:root</c>
    /// declaration block (or whichever scope the stylesheet chooses).</summary>
    public T DefaultValue { get; }

    /// <summary>The CSS variable name including the leading <c>--</c>
    /// (e.g. <c>"--radius"</c>). Set by the source generator from the C# field name,
    /// or supplied explicitly via <see cref="CssVar.External(string)"/>.</summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>Whether this variable inherits from ancestors (CSS default
    /// <see langword="true"/>). Emitted to <c>@property</c> when a syntax can be
    /// inferred from <typeparamref name="T"/>.</summary>
    public bool Inherits { get; init; } = true;

    /// <summary>The CSS <c>syntax</c> string for the auto-generated
    /// <c>@property</c> rule (spec §30). Inferred from <typeparamref name="T"/>
    /// at render time when not explicitly set.</summary>
    public string? Syntax { get; init; }

    /// <summary>Constructs a variable with a default value.</summary>
    /// <param name="defaultValue">Emitted in the stylesheet's <c>:root</c> block.</param>
    public CssVar(T defaultValue) {
        DefaultValue = defaultValue;
    }

    /// <summary>Variables are runtime-resolved by the browser — they carry through
    /// the <c>IsVariable</c>-style taint that the spec defines on
    /// <see cref="ICssValue"/> (§29). Referencing this variable means any expression
    /// it participates in must emit through the CSS branch, not SCSS.</summary>
    /// <returns><c>var(--{Name})</c>.</returns>
    /// <remarks>
    /// If <see cref="Name"/> hasn't been populated yet (e.g. another stylesheet
    /// references this variable before the variable's stylesheet has been
    /// rendered), <see cref="CssRegistry.EnsureScanned"/> is triggered to walk the
    /// AppDomain and populate names from field identifiers. Lazy and idempotent.
    /// </remarks>
    public string ToCss() {
        if (string.IsNullOrEmpty(Name)) CssRegistry.EnsureScanned();
        return $"var({Name})";
    }

    /// <summary>
    /// Provides a fallback value for <c>var(--name, fallback)</c>. Returns a
    /// fresh <typeparamref name="T"/> rather than another <see cref="CssVar{T}"/>
    /// — the type system thus prevents accidental chaining like
    /// <c>a.Or(b).Or(c)</c> (which would build the wrong CSS) and forces correct
    /// inside-out nesting (<c>brand.Or(primary.Or(Color.Blue))</c> → spec §31).
    /// </summary>
    /// <param name="fallback">The value to use when the variable is unset.</param>
    /// <remarks>
    /// <para>
    /// <c>.Or()</c> is typically called inside a stylesheet's static field
    /// initializer (<c>Background = Brand.Or(Color.Blue)</c>), at which point
    /// the variable's <see cref="Name"/> may not yet be populated by the
    /// AppDomain scan. To handle that, the result captures a placeholder token
    /// that the renderer resolves to the real variable name once names are
    /// known. The token is a registered key in
    /// <see cref="CssVarFallbackRegistry"/> — entirely an implementation
    /// detail, never visible in the emitted CSS.
    /// </para>
    /// </remarks>
    public T Or(T fallback) {
        var ctor = typeof(T).GetConstructor(new[] { typeof(string) });
        if (ctor is null) {
            throw new System.NotSupportedException(
                $"{typeof(T).Name} must expose a public constructor that takes a single string for .Or() fallback support.");
        }
        // Defer name resolution: register this CssVar reference and emit a
        // placeholder. The renderer replaces placeholders with var(...) once
        // PopulateFieldNames has run.
        var token = CssVarFallbackRegistry.Register(this, fallback);
        return (T)ctor.Invoke(new object[] { token });
    }
}

/// <summary>
/// Internal registry that defers name-dependent string composition until
/// render time. Solves the type-initializer ordering problem: when a
/// stylesheet's <c>static readonly</c> fields are first accessed, their
/// initializers run in declaration order, and inside those initializers
/// users compose values that reference other fields' names — names which
/// haven't been populated yet by the AppDomain scan that owns naming.
/// </summary>
/// <remarks>
/// <para>
/// Each registration produces a placeholder token like
/// <c>__late_bind_N__</c>. Generated CSS contains those tokens until
/// <see cref="StyleSheet.Render(System.Type)"/> calls
/// <see cref="Resolve(string)"/>, which scans for tokens and substitutes the
/// resolved string. Tokens that were never persisted into a render output
/// are simply ignored.
/// </para>
/// <para>
/// Two kinds of late binding are supported:
/// <list type="bullet">
///   <item>CssVar fallback — captures the variable reference and the fallback
///   value, resolves to <c>var(--name, fallback)</c>.</item>
///   <item>Keyframe name — captures the Keyframes reference, resolves to its
///   kebab-cased name (used by <c>Animation = SomeKeyframes + " 200ms"</c>).</item>
/// </list>
/// </para>
/// </remarks>
internal static class CssVarFallbackRegistry {
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Func<string>> _entries = new();
    private static int _nextId;

    /// <summary>Registers a CssVar fallback chain; returns a placeholder token.</summary>
    public static string Register(object variable, ICssValue fallback) {
        var id = System.Threading.Interlocked.Increment(ref _nextId);
        var token = $"__late_bind_{id}__";
        _entries[token] = () => {
            var nameProp = variable.GetType().GetProperty(nameof(CssVar<ICssValue>.Name));
            var name = (string?)nameProp?.GetValue(variable) ?? "";
            return $"var({name}, {fallback.ToCss()})";
        };
        return token;
    }

    /// <summary>Registers a name lookup against an arbitrary object that
    /// exposes a <c>Name</c> property — used by <c>Keyframes</c> implicit-string.
    /// Returns a placeholder token that resolves to the object's name at render time.</summary>
    public static string RegisterNameRef(object source) {
        var id = System.Threading.Interlocked.Increment(ref _nextId);
        var token = $"__late_bind_{id}__";
        _entries[token] = () => {
            var nameProp = source.GetType().GetProperty("Name");
            return (string?)nameProp?.GetValue(source) ?? "";
        };
        return token;
    }

    /// <summary>Replaces any placeholder tokens in <paramref name="css"/> with
    /// their resolved expressions. Nested chains (a fallback that itself
    /// references another late-bound value) require multiple passes — keep
    /// iterating until either no tokens remain or no replacements happen
    /// in a pass (the loop is bounded by the number of registered tokens).</summary>
    public static string Resolve(string css) {
        if (_entries.IsEmpty) return css;
        if (css.IndexOf("__late_bind_", System.StringComparison.Ordinal) < 0) return css;

        for (int pass = 0; pass < _entries.Count + 1; pass++) {
            bool replaced = false;
            foreach (var kvp in _entries) {
                if (!css.Contains(kvp.Key)) continue;
                css = css.Replace(kvp.Key, kvp.Value());
                replaced = true;
            }
            if (!replaced) break;
            if (css.IndexOf("__late_bind_", System.StringComparison.Ordinal) < 0) break;
        }
        return css;
    }
}

/// <summary>
/// Static entry points for <see cref="CssVar{T}"/> that don't fit on the generic type itself.
/// </summary>
public static class CssVar {
    /// <summary>
    /// Binds to a custom property defined outside this stylesheet — for example
    /// <c>--mud-palette-primary</c> from MudBlazor — by literal name. The auto-generated
    /// external-class hierarchy (e.g. <c>Mud.Palette.Primary</c>) is the preferred path;
    /// this is the escape hatch when the parser can't resolve the variable.
    /// </summary>
    /// <param name="name">The literal CSS custom-property name including <c>--</c>.</param>
    public static CssVar<T> External<T>(string name) where T : ICssValue {
        // We use Activator with no args to construct a default T. Every
        // primitive in BrowserApi.Css ships a parameterless or single-string
        // constructor; default-CSS-value cases pass through a "" string when
        // possible. The DefaultValue of an external variable is never emitted
        // (no :root block), so the actual value here doesn't matter — Name is
        // what's used.
        T defaultValue;
        try {
            // Most primitives are structs, so default(T) gives a valid
            // (perhaps empty) instance.
            defaultValue = default(T)!;
        } catch {
            defaultValue = (T)System.Activator.CreateInstance(typeof(T))!;
        }
        return new CssVar<T>(defaultValue) { Name = name };
    }
}
