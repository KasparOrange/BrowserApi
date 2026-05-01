namespace BrowserApi.Css.Authoring;

/// <summary>
/// CSS <c>@layer</c> support — a way to declare a cascade layer that wins
/// over un-layered styles AND lower-named layers, with one <c>@layer name &amp; { ... }</c>
/// block per stylesheet's content.
/// </summary>
/// <remarks>
/// <para>
/// Spec §33 lists this as post-MVP, but layers are well-supported in
/// modern browsers and useful for grouping resets / utilities / components
/// without specificity wars. Two ways to use:
/// </para>
/// <list type="bullet">
///   <item><c>[Layer("name")]</c> attribute on a StyleSheet — wraps the
///   entire emitted CSS in <c>@layer name { ... }</c>.</item>
///   <item>Indexer-form on Declarations: <c>[CssLayer.Of("utilities")] = new() { ... }</c>
///   nests a single block inside a layer.</item>
/// </list>
/// </remarks>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class LayerAttribute : System.Attribute {
    /// <summary>The layer name. Layer order is the order layers are
    /// FIRST declared in the document.</summary>
    public string Value { get; }

    /// <summary>Constructs the attribute with the supplied layer name.</summary>
    /// <param name="value">The layer name (e.g. <c>"utilities"</c>, <c>"components"</c>).</param>
    public LayerAttribute(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new System.ArgumentException("Layer name must be a non-empty identifier.", nameof(value));
        }
        Value = value;
    }
}

/// <summary>
/// Indexer-form layer wrapper. Construct with <see cref="Of"/> and use as
/// a <see cref="Selector"/>-shaped key on the Declarations indexer:
/// <code>
/// [CssLayer.Of("utilities")] = new() { Display = Display.None },
/// </code>
/// </summary>
public readonly struct CssLayer {
    private readonly string _name;
    private CssLayer(string name) { _name = name; }

    /// <summary>Builds a layer reference with the given name.</summary>
    public static CssLayer Of(string name) => new(name);

    /// <summary>Implicit conversion to <see cref="Selector"/> — emits
    /// <c>@layer name</c> as the selector token, which the renderer
    /// recognizes as an at-rule and wraps the body in
    /// <c>@layer name { ... }</c>.</summary>
    public static implicit operator Selector(CssLayer l) => new($"@layer {l._name}");
}
