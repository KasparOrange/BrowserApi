namespace BrowserApi.Css.Authoring;

/// <summary>
/// CSS <c>@media</c> query — wraps a feature expression like
/// <c>(min-width: 768px)</c> and converts to a <see cref="Selector"/> with
/// the <c>@media</c> prefix when used as an indexer key.
/// </summary>
/// <remarks>
/// <para>
/// Construct queries via the static factory members (<see cref="MinWidth"/>,
/// <see cref="MaxWidth"/>, <see cref="PrefersDark"/>, …) and feed them to the
/// nesting indexer:
/// </para>
/// <code>
/// [MediaQuery.MaxWidth(768.Px())] = new() { Padding = 4.Px() }
/// </code>
/// <para>
/// Multiple features combine with <c>&amp;</c> (logical AND); alternatives use
/// <c>|</c> (CSS media query list). Naming note: the type is <c>MediaQuery</c>
/// rather than just <c>Media</c> because <c>BrowserApi.Media</c> is an existing
/// namespace from the WebIDL-generated MediaCapabilities API. Using the more
/// specific name avoids collision and reads naturally at call sites.
/// </para>
/// </remarks>
public readonly struct MediaQuery {
    private readonly string _features;

    /// <summary>Wraps a media-feature expression like <c>"(min-width: 768px)"</c>.</summary>
    public MediaQuery(string features) { _features = features; }

    // ─────────────────────────────────── Factories ──────────────────────────────────

    /// <summary><c>@media (min-width: …)</c>.</summary>
    public static MediaQuery MinWidth(Length width) => new($"(min-width: {width.ToCss()})");

    /// <summary><c>@media (max-width: …)</c>.</summary>
    public static MediaQuery MaxWidth(Length width) => new($"(max-width: {width.ToCss()})");

    /// <summary><c>@media (min-height: …)</c>.</summary>
    public static MediaQuery MinHeight(Length height) => new($"(min-height: {height.ToCss()})");

    /// <summary><c>@media (max-height: …)</c>.</summary>
    public static MediaQuery MaxHeight(Length height) => new($"(max-height: {height.ToCss()})");

    /// <summary><c>@media (prefers-color-scheme: dark)</c>.</summary>
    public static MediaQuery PrefersDark { get; } = new("(prefers-color-scheme: dark)");

    /// <summary><c>@media (prefers-color-scheme: light)</c>.</summary>
    public static MediaQuery PrefersLight { get; } = new("(prefers-color-scheme: light)");

    /// <summary><c>@media (prefers-reduced-motion: reduce)</c>.</summary>
    public static MediaQuery PrefersReducedMotion { get; } = new("(prefers-reduced-motion: reduce)");

    /// <summary><c>@media (prefers-reduced-data: reduce)</c>.</summary>
    public static MediaQuery PrefersReducedData { get; } = new("(prefers-reduced-data: reduce)");

    /// <summary><c>@media print</c>.</summary>
    public static MediaQuery Print { get; } = new("print");

    /// <summary><c>@media screen</c>.</summary>
    public static MediaQuery Screen { get; } = new("screen");

    /// <summary><c>@media (hover: hover)</c> — capable of hover (mouse, not touch).</summary>
    public static MediaQuery Hover { get; } = new("(hover: hover)");

    /// <summary><c>@media (orientation: portrait)</c>.</summary>
    public static MediaQuery Portrait { get; } = new("(orientation: portrait)");

    /// <summary><c>@media (orientation: landscape)</c>.</summary>
    public static MediaQuery Landscape { get; } = new("(orientation: landscape)");

    // ────────────────────────────────── Composition ─────────────────────────────────

    /// <summary>Combines two media queries with <c>and</c> — both must be true.</summary>
    public static MediaQuery operator &(MediaQuery a, MediaQuery b) =>
        new($"{a._features} and {b._features}");

    /// <summary>Combines two media queries as a comma-separated list — match any.</summary>
    public static MediaQuery operator |(MediaQuery a, MediaQuery b) =>
        new($"{a._features}, {b._features}");

    /// <summary>The full media-query selector, including the <c>@media</c> prefix.</summary>
    public Selector AsSelector() => new($"@media {_features}");

    /// <summary>Implicit conversion so a <see cref="MediaQuery"/> can be used directly
    /// as an indexer key on <see cref="Declarations"/>.</summary>
    public static implicit operator Selector(MediaQuery q) => q.AsSelector();
}
