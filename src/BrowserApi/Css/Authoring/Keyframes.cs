using System.Collections.Generic;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A CSS <c>@keyframes</c> animation. Authored with <see cref="Percentage"/> keys
/// (or the injected <c>From</c> / <c>To</c> constants from <see cref="StyleSheet"/>)
/// each pointing at a <see cref="Declarations"/> block.
/// </summary>
/// <remarks>
/// <para>
/// The animation name is derived from the C# field identifier (PascalCase →
/// kebab-case) by the source generator — <c>FadeIn</c> becomes <c>fade-in</c>.
/// Reference an animation by name when setting <c>Animation</c> properties; a
/// future iteration will provide a typed reference that resolves to the
/// generated name without a string.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public static readonly Keyframes FadeIn = new() {
///     [From] = new() { Opacity = 0 },
///     [50.Percent()] = new() { Opacity = 0.5 },
///     [To] = new() { Opacity = 1 },
/// };
/// </code>
/// </example>
public sealed class Keyframes {
    private readonly List<KeyValuePair<string, Declarations>> _stops = new();

    /// <summary>The animation name; populated by the source generator
    /// (or by <see cref="StyleSheet.Render(System.Type)"/> at runtime) from
    /// the C# field name in PascalCase → kebab-case form.</summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>The keyframe stops in source order.</summary>
    public IReadOnlyList<KeyValuePair<string, Declarations>> Stops => _stops;

    /// <summary>Indexer for percentage stops — <c>[50.Percent()]</c>.</summary>
    public Declarations this[Percentage stop] {
        get => throw new System.NotSupportedException("Keyframes indexer is set-only.");
        set => _stops.Add(new(stop.ToCss(), value));
    }

    /// <summary>Indexer for the injected <c>From</c>/<c>To</c> string constants
    /// (which are <c>"0%"</c> / <c>"100%"</c>).</summary>
    public Declarations this[string stop] {
        get => throw new System.NotSupportedException("Keyframes indexer is set-only.");
        set => _stops.Add(new(stop, value));
    }
}
