namespace BrowserApi.Css;

/// <summary>
/// <c>.Important</c> properties on CSS value types — appends <c>!important</c>
/// to the serialized output. The same property type is returned, so
/// <c>16.Px().Important</c> is still a <see cref="Length"/> assignable to any
/// length-typed setter.
/// </summary>
/// <remarks>
/// <para>
/// For <see cref="Length"/> and <see cref="CssColor"/> the property lives directly on
/// the partial struct. For C# enum keyword types (<c>Display</c>, <c>Position</c>, …)
/// the spec calls for C# 14 extension properties; that's tracked as a planned
/// follow-up. Until then, use <c>"none !important"</c> via the raw string setter
/// for enum keywords if you genuinely need the override.
/// </para>
/// <para>
/// <c>!important</c> is the wrong tool the vast majority of the time. Reach for it
/// only when you cannot reorder selectors or adjust specificity — and consider
/// whether a future BCA analyzer would flag your usage as smelly. Spec §14.
/// </para>
/// </remarks>
public readonly partial struct Length {
    /// <summary>Returns this length with the <c>!important</c> priority flag
    /// appended to its CSS output.</summary>
    public Length Important => new(this.ToCss() + " !important");
}

/// <inheritdoc cref="Length"/>
public readonly partial struct CssColor {
    /// <summary>Returns this color with the <c>!important</c> priority flag.</summary>
    public CssColor Important => new(this.ToCss() + " !important");
}

/// <inheritdoc cref="Length"/>
public readonly partial struct Percentage {
    /// <summary>Returns this percentage with the <c>!important</c> priority flag.</summary>
    public Percentage Important => new(this.ToCss() + " !important");
}
