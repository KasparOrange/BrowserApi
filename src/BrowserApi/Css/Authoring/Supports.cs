namespace BrowserApi.Css.Authoring;

/// <summary>
/// CSS <c>@supports</c> feature query — wraps a property/value test like
/// <c>(display: grid)</c> and converts to a <see cref="Selector"/> with the
/// <c>@supports</c> prefix when used as a nesting indexer key.
/// </summary>
/// <example>
/// <code>
/// public static readonly Class Layout = new() {
///     Display = Display.Block,
///     [Supports.Property("display", "grid")] = new() { Display = Display.Grid },
/// };
/// </code>
/// </example>
public readonly struct Supports {
    private readonly string _condition;
    private Supports(string condition) { _condition = condition; }

    /// <summary>Tests whether the browser supports a given CSS property/value pair.</summary>
    public static Supports Property(string propertyName, string value) =>
        new($"({propertyName}: {value})");

    /// <summary>Pre-built check for <c>display: grid</c>.</summary>
    public static Supports Grid { get; } = Property("display", "grid");

    /// <summary>Pre-built check for <c>display: flex</c>.</summary>
    public static Supports Flex { get; } = Property("display", "flex");

    /// <summary>Pre-built check for native CSS nesting (<c>selector(&amp;)</c>).</summary>
    public static Supports Nesting { get; } = new("selector(&)");

    /// <summary>Logical AND between two feature queries.</summary>
    public static Supports operator &(Supports a, Supports b) => new($"{a._condition} and {b._condition}");

    /// <summary>Logical OR between two feature queries.</summary>
    public static Supports operator |(Supports a, Supports b) => new($"{a._condition} or {b._condition}");

    /// <summary>Logical NOT.</summary>
    public static Supports operator !(Supports a) => new($"not {a._condition}");

    /// <summary>Implicit conversion to a <see cref="Selector"/> for use as an indexer key.</summary>
    public static implicit operator Selector(Supports s) => new($"@supports {s._condition}");
}

/// <summary>
/// CSS <c>@container</c> query — like <see cref="MediaQuery"/> but evaluated
/// against the nearest containing element with a <c>container-type</c>
/// (or <c>container-name</c>) declaration rather than the viewport.
/// </summary>
/// <remarks>
/// The containing element must declare <c>ContainerType</c> for the query to
/// resolve. A future analyzer (see spec §32) will warn when a container query
/// is used without a <c>ContainerType</c> ancestor.
/// </remarks>
/// <example>
/// <code>
/// public static readonly Class CardWrapper = new() {
///     ContainerType = "inline-size",
///     [ContainerQuery.MinWidth(400.Px())] = new() {
///         Display = Display.Grid,
///     },
/// };
/// </code>
/// </example>
public readonly struct ContainerQuery {
    private readonly string _features;
    private ContainerQuery(string features) { _features = features; }

    /// <summary><c>@container (min-width: …)</c>.</summary>
    public static ContainerQuery MinWidth(Length width) => new($"(min-width: {width.ToCss()})");

    /// <summary><c>@container (max-width: …)</c>.</summary>
    public static ContainerQuery MaxWidth(Length width) => new($"(max-width: {width.ToCss()})");

    /// <summary>Logical AND.</summary>
    public static ContainerQuery operator &(ContainerQuery a, ContainerQuery b) =>
        new($"{a._features} and {b._features}");

    /// <summary>Implicit conversion to a <see cref="Selector"/> for use as an indexer key.</summary>
    public static implicit operator Selector(ContainerQuery q) => new($"@container {q._features}");
}
