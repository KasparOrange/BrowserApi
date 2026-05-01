using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// Typed values for the CSS <c>grid-template-columns</c> /
/// <c>grid-template-rows</c> properties. Supports the common track-list
/// constructs: lengths, flex (<c>fr</c>), <c>auto</c>, <c>min-content</c>,
/// <c>max-content</c>, <c>repeat()</c>, and <c>minmax()</c>.
/// </summary>
/// <remarks>
/// Spec §17. Implicit conversions from <see cref="Length"/> and
/// <see cref="Flex"/> let single-track values flow into the property
/// without ceremony; multi-track expressions use the static factories.
/// </remarks>
/// <example>
/// <code>
/// GridTemplateColumns = GridTemplate.Repeat(3, Length.Fr(1));
/// GridTemplateColumns = GridTemplate.Repeat(GridTemplate.AutoFill,
///                                            GridTemplate.MinMax(Length.Px(200), Length.Fr(1)));
/// GridTemplateRows = Length.Auto;   // single-track via implicit conversion
/// </code>
/// </example>
public readonly struct GridTemplate : ICssValue {
    private readonly string _css;

    /// <summary>Wraps a pre-rendered grid-template string.</summary>
    public GridTemplate(string css) { _css = css; }

    /// <inheritdoc/>
    public string ToCss() => _css;

    /// <summary>The CSS <c>none</c> keyword.</summary>
    public static GridTemplate None { get; } = new("none");

    /// <summary>The CSS <c>auto-fill</c> keyword for repeat() count.</summary>
    public static GridTemplate AutoFill { get; } = new("auto-fill");

    /// <summary>The CSS <c>auto-fit</c> keyword for repeat() count.</summary>
    public static GridTemplate AutoFit { get; } = new("auto-fit");

    /// <summary>Builds <c>repeat(count, tracks)</c>.</summary>
    public static GridTemplate Repeat(int count, GridTemplate tracks)
        => new($"repeat({count}, {tracks._css})");

    /// <summary>Builds <c>repeat(auto-fill | auto-fit, tracks)</c>.</summary>
    public static GridTemplate Repeat(GridTemplate count, GridTemplate tracks)
        => new($"repeat({count._css}, {tracks._css})");

    /// <summary>Builds <c>minmax(min, max)</c>.</summary>
    public static GridTemplate MinMax(GridTemplate min, GridTemplate max)
        => new($"minmax({min._css}, {max._css})");

    /// <summary>Builds <c>minmax(min, max)</c> from two lengths.</summary>
    public static GridTemplate MinMax(Length min, Length max)
        => new($"minmax({min.ToCss()}, {max.ToCss()})");

    /// <summary>Builds <c>minmax(min, max)</c> from a length min and a flex max.</summary>
    public static GridTemplate MinMax(Length min, Flex max)
        => new($"minmax({min.ToCss()}, {max.ToCss()})");

    /// <summary>Joins multiple track expressions with whitespace.</summary>
    public static GridTemplate Of(params GridTemplate[] tracks)
        => new(string.Join(" ", System.Array.ConvertAll(tracks, t => t._css)));

    /// <summary>Implicit conversion from <see cref="Length"/> for the single-track case.</summary>
    public static implicit operator GridTemplate(Length length) => new(length.ToCss());

    /// <summary>Implicit conversion from <see cref="Flex"/> (<c>1.Fr()</c>, etc.).</summary>
    public static implicit operator GridTemplate(Flex flex) => new(flex.ToCss());

    /// <summary>Implicit conversion to string for use in property setters that
    /// accept raw strings (interim until typed setters land everywhere).</summary>
    public static implicit operator string(GridTemplate t) => t._css;

    /// <summary>Implicit conversion FROM string — lets pre-existing string-typed
    /// callers continue to work (e.g. <c>GridTemplateColumns = "1fr 2fr 1fr"</c>).</summary>
    public static implicit operator GridTemplate(string css) => new(css);
}
