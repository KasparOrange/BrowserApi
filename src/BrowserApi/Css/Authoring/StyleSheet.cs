using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// The base class for stylesheets authored in C#. Subclasses declare CSS as
/// <c>public static readonly Class</c>/<c>Rule</c>/<c>Rules</c> fields; calling
/// <see cref="Render(System.Type)"/> walks those fields and produces CSS.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StyleSheet"/> is also a <em>marker</em> — the source generator
/// discovers stylesheet types by checking which classes derive from it. This is
/// the one place the spec admits a base-class convention; everything else
/// (Class/Rule/CssVar discovery) is by C# type, not by inheritance or naming.
/// </para>
/// <para>
/// The class exposes <see cref="Self"/>, <see cref="From"/>, <see cref="To"/>,
/// <see cref="Is(Selector[])"/>, and <see cref="Where(Selector[])"/> as
/// <c>protected static</c> members so derived stylesheets can reference them
/// unqualified — <c>[Self.Hover]</c>, <c>[Where(El.H1, El.H2)]</c>, etc. The same
/// names are also available via <c>Css.X</c> for use outside stylesheets.
/// </para>
/// <para>
/// <strong>Render strategy.</strong> The MVP path is reflection-based: at runtime
/// we walk <c>static readonly</c> fields of the stylesheet type, emit CSS for
/// each <see cref="Class"/>/<see cref="Rule"/> in declaration order, and return a
/// single CSS string. The source generator will replace this with a generated
/// <c>ToCss()</c> method that runs at compile time, but the reflection path stays
/// available for tests and for runtime-driven scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class AppStyles : StyleSheet {
///     public static readonly Class Card = new() {
///         Padding = 16.Px(),
///         Background = CssColor.White,
///         BorderRadius = 8.Px(),
///         [Self.Hover] = new() {
///             Background = CssColor.Hex("#f5f5f5"),
///         },
///     };
/// }
///
/// var css = StyleSheet.Render(typeof(AppStyles));
/// // .card { padding: 16px; background: #fff; border-radius: 8px; }
/// // .card:hover { background: #f5f5f5; }
/// </code>
/// </example>
public abstract class StyleSheet {
    // ─────────────────────────────── Injected helpers ───────────────────────────────

    /// <summary>The SCSS parent reference (<c>&amp;</c>). Inside a nesting indexer,
    /// <see cref="Self"/> refers to the rule whose declaration block we're in.</summary>
    protected static Selector Self { get; } = new("&");

    /// <summary>Keyframe stop at <c>0%</c>.</summary>
    protected static string From { get; } = "0%";

    /// <summary>Keyframe stop at <c>100%</c>.</summary>
    protected static string To { get; } = "100%";

    /// <summary>The <c>:is(...)</c> grouping pseudo-class — match any of the
    /// supplied selectors with the highest specificity among them.</summary>
    /// <param name="selectors">The selectors to group.</param>
    protected static Selector Is(params Selector[] selectors) {
        if (selectors is null || selectors.Length == 0) {
            throw new ArgumentException("At least one selector is required.", nameof(selectors));
        }
        var inner = string.Join(", ", Array.ConvertAll(selectors, s => s.Css));
        return new Selector($":is({inner})");
    }

    /// <summary>The <c>:where(...)</c> grouping pseudo-class — match any of the
    /// supplied selectors with specificity zero. The specificity-deflation tool.</summary>
    /// <param name="selectors">The selectors to group.</param>
    protected static Selector Where(params Selector[] selectors) {
        if (selectors is null || selectors.Length == 0) {
            throw new ArgumentException("At least one selector is required.", nameof(selectors));
        }
        var inner = string.Join(", ", Array.ConvertAll(selectors, s => s.Css));
        return new Selector($":where({inner})");
    }

    // ───────────────────────────── Runtime render path ──────────────────────────────

    /// <summary>
    /// Walks the <c>static readonly</c> fields of <paramref name="styleSheetType"/>
    /// and returns the CSS as a single string. Field names map to class names via
    /// PascalCase → kebab-case; <see cref="Class"/> instances get a <c>.</c>-prefixed
    /// selector, <see cref="Rule"/> instances use their constructor-supplied selector.
    /// </summary>
    /// <param name="styleSheetType">A type derived from <see cref="StyleSheet"/>.</param>
    /// <returns>A CSS string ready to drop into a <c>&lt;style&gt;</c> tag or write to a file.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="styleSheetType"/> does not derive from <see cref="StyleSheet"/>.
    /// </exception>
    public static string Render(Type styleSheetType) {
        if (styleSheetType is null) throw new ArgumentNullException(nameof(styleSheetType));
        if (!typeof(StyleSheet).IsAssignableFrom(styleSheetType)) {
            throw new ArgumentException(
                $"Type '{styleSheetType.FullName}' must derive from {nameof(StyleSheet)}.",
                nameof(styleSheetType));
        }

        var sb = new StringBuilder();
        var fields = styleSheetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

        // Pass 1 — collect CssVar fields so their defaults can be emitted into a
        // single :root block at the top of the file. Source order is preserved
        // among the variables themselves.
        var cssVars = new System.Collections.Generic.List<(string Name, string DefaultCss, string? Syntax, bool Inherits)>();
        foreach (var field in fields) {
            var value = field.GetValue(null);
            if (value is null) continue;

            var t = value.GetType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CssVar<>)) {
                // Set the variable's name from the field if not already set
                // (CssVar.External already sets Name explicitly).
                var nameProp = t.GetProperty(nameof(CssVar<Common.ICssValue>.Name));
                var existing = (string?)nameProp?.GetValue(value);
                if (string.IsNullOrEmpty(existing)) {
                    nameProp?.SetValue(value, "--" + ToKebabCase(field.Name));
                }
                var name = (string)nameProp!.GetValue(value)!;

                // Default value: read DefaultValue and call ToCss() on it.
                var defaultProp = t.GetProperty(nameof(CssVar<Common.ICssValue>.DefaultValue));
                var defaultValue = defaultProp?.GetValue(value) as Common.ICssValue;

                // @property syntax & inherits flags (spec §30).
                var syntaxProp = t.GetProperty(nameof(CssVar<Common.ICssValue>.Syntax));
                var inheritsProp = t.GetProperty(nameof(CssVar<Common.ICssValue>.Inherits));
                var explicitSyntax = (string?)syntaxProp?.GetValue(value);
                var inheritsValue = (bool?)inheritsProp?.GetValue(value) ?? true;

                // Infer @property syntax from the value-type T when not explicit.
                var innerType = t.GetGenericArguments()[0];
                var syntax = explicitSyntax ?? InferAtPropertySyntax(innerType);

                cssVars.Add((name, defaultValue?.ToCss() ?? "", syntax, inheritsValue));
            }
        }

        if (cssVars.Count > 0) {
            sb.Append(":root {");
            foreach (var v in cssVars) {
                sb.Append(' ').Append(v.Name).Append(": ").Append(v.DefaultCss).Append(';');
            }
            sb.Append(" }\n");

            // @property auto-emission (spec §30) — gives the browser a typed
            // schema for each variable so animations, type-checking, and the
            // dev-tools "Computed" tab work correctly.
            foreach (var v in cssVars) {
                if (v.Syntax is null) continue; // No inferable syntax → skip.
                sb.Append("@property ").Append(v.Name).Append(" {");
                sb.Append(" syntax: \"").Append(v.Syntax).Append("\";");
                sb.Append(" inherits: ").Append(v.Inherits ? "true" : "false").Append(';');
                sb.Append(" initial-value: ").Append(v.DefaultCss).Append(';');
                sb.Append(" }\n");
            }
        }

        // Pass 2 — emit class, rule, keyframes, font-face, and Rules-collection
        // fields in declaration order.
        foreach (var field in fields) {
            var value = field.GetValue(null);
            switch (value) {
                case Class cls: {
                    if (string.IsNullOrEmpty(cls.Name)) {
                        cls.Name = ToKebabCase(field.Name);
                    }
                    EmitRule(sb, cls.Selector, cls);
                    break;
                }
                case Rule rule:
                    EmitRule(sb, rule.Selector, rule);
                    break;
                case Rules rules:
                    foreach (var r in rules) EmitRule(sb, r.Selector, r);
                    break;
                case Keyframes kf: {
                    if (string.IsNullOrEmpty(kf.Name)) {
                        kf.Name = ToKebabCase(field.Name);
                    }
                    EmitKeyframes(sb, kf);
                    break;
                }
                case FontFace ff:
                    EmitFontFace(sb, ff);
                    break;
            }
        }

        // Resolve any deferred .Or() fallback placeholders captured during
        // type initialization. By this point PopulateFieldNames has run for
        // every stylesheet, so all CssVar names are known.
        return CssVarFallbackRegistry.Resolve(sb.ToString());
    }

    private static void EmitFontFace(StringBuilder sb, FontFace ff) {
        sb.Append("@font-face {");
        foreach (var p in ff.Properties) {
            sb.Append(' ').Append(p.Key).Append(": ").Append(p.Value).Append(';');
        }
        sb.Append(" }\n");
    }

    private static void EmitKeyframes(StringBuilder sb, Keyframes kf) {
        sb.Append("@keyframes ").Append(kf.Name).Append(" {\n");
        foreach (var stop in kf.Stops) {
            sb.Append("  ").Append(stop.Key).Append(" {");
            foreach (var p in stop.Value.Properties) {
                sb.Append(' ').Append(p.Key).Append(": ").Append(p.Value).Append(';');
            }
            sb.Append(" }\n");
        }
        sb.Append("}\n");
    }

    /// <summary>Convenience overload — <c>StyleSheet.Render&lt;AppStyles&gt;()</c>.</summary>
    public static string Render<T>() where T : StyleSheet => Render(typeof(T));

    /// <summary>
    /// Populates the kebab-cased <c>Name</c> on every <see cref="Class"/>,
    /// <see cref="CssVar{T}"/>, and <see cref="Keyframes"/> field of
    /// <paramref name="styleSheetType"/>. Called by
    /// <see cref="CssRegistry"/> in a first pass so cross-stylesheet
    /// references resolve correctly during rendering. Idempotent — fields
    /// that already have a non-empty name (e.g. via
    /// <see cref="CssVar.External{T}(string)"/>) are left untouched.
    /// </summary>
    public static void PopulateFieldNames(Type styleSheetType) {
        if (styleSheetType is null) throw new ArgumentNullException(nameof(styleSheetType));
        var fields = styleSheetType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        foreach (var field in fields) {
            var value = field.GetValue(null);
            if (value is null) continue;

            switch (value) {
                case Class cls when string.IsNullOrEmpty(cls.Name):
                    cls.Name = ToKebabCase(field.Name);
                    break;
                case Keyframes kf when string.IsNullOrEmpty(kf.Name):
                    kf.Name = ToKebabCase(field.Name);
                    break;
                default: {
                    var t = value.GetType();
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CssVar<>)) {
                        var nameProp = t.GetProperty(nameof(CssVar<Common.ICssValue>.Name));
                        var existing = (string?)nameProp?.GetValue(value);
                        if (string.IsNullOrEmpty(existing)) {
                            nameProp?.SetValue(value, "--" + ToKebabCase(field.Name));
                        }
                    }
                    break;
                }
            }
        }
    }

    // ─────────────────────────────── Emitter internals ──────────────────────────────

    private static void EmitRule(StringBuilder sb, Selector selector, Declarations decl) {
        // Emit own properties under the supplied selector if any are present.
        if (decl.Properties.Count > 0) {
            sb.Append(selector.Css).Append(" {");
            foreach (var p in decl.Properties) {
                sb.Append(' ').Append(p.Key).Append(": ").Append(p.Value).Append(';');
            }
            sb.Append(" }\n");
        }

        // Emit nested blocks. MVP resolves the SCSS '&' substitution itself
        // rather than deferring to sass — testable without a sass dependency.
        foreach (var nested in decl.Nested) {
            var nestedKey = nested.Key.Css;
            if (nestedKey.StartsWith("@", System.StringComparison.Ordinal)) {
                // At-rule block (@media, @supports, @container) — wrap, don't concatenate.
                EmitAtRule(sb, selector, nested.Key, nested.Value);
            } else {
                var resolved = ResolveAmpersand(selector, nested.Key);
                EmitRule(sb, resolved, nested.Value);
            }
        }
    }

    private static void EmitAtRule(StringBuilder sb, Selector parent, Selector atRule, Declarations decl) {
        // @media (min-width: ...) { .card { ... } .card:hover { ... } }
        sb.Append(atRule.Css).Append(" {\n");
        EmitRule(sb, parent, decl);
        sb.Append("}\n");
    }

    private static Selector ResolveAmpersand(Selector parent, Selector child) {
        var c = child.Css;
        // Replace the SCSS parent reference with the parent selector string.
        // The simple case: child starts with '&'.
        if (c.StartsWith("&", StringComparison.Ordinal)) {
            return new Selector(parent.Css + c.Substring(1));
        }
        // Embedded '&' (e.g. ":is(&:hover, &:focus)") — substitute every occurrence.
        if (c.Contains('&')) {
            return new Selector(c.Replace("&", parent.Css));
        }
        // No '&' — child is a descendant of parent (CSS nesting default).
        return new Selector(parent.Css + " " + c);
    }

    /// <summary>
    /// Maps a <see cref="CssVar{T}"/>'s value type to a CSS <c>@property</c>
    /// <c>syntax</c> string (spec §30). Returns <see langword="null"/> for types
    /// without a known mapping — those variables are emitted without an
    /// <c>@property</c> rule.
    /// </summary>
    private static string? InferAtPropertySyntax(System.Type valueType) {
        var name = valueType.FullName;
        return name switch {
            "BrowserApi.Css.Length"     => "<length>",
            "BrowserApi.Css.Percentage" => "<percentage>",
            "BrowserApi.Css.CssColor"   => "<color>",
            "BrowserApi.Css.Angle"      => "<angle>",
            "BrowserApi.Css.Duration"   => "<time>",
            "BrowserApi.Css.Resolution" => "<resolution>",
            _ => null,
        };
    }

    private static string ToKebabCase(string pascal) {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var sb = new StringBuilder(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++) {
            var ch = pascal[i];
            if (i > 0 && char.IsUpper(ch)) sb.Append('-');
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}
