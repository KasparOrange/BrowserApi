namespace BrowserApi.Css.Authoring;

/// <summary>
/// A typed value for the CSS <c>border</c> / <c>outline</c> shorthand —
/// width, style, and color combined into one declaration. Constructed via
/// the named factories (<see cref="Solid"/>, <see cref="Dashed"/>, …) so the
/// resulting CSS reads naturally and the style is part of the type signature.
/// </summary>
/// <example>
/// <code>
/// Border = Border.Solid(1.Px(), CssColor.Black),       // "1px solid black"
/// Border = Border.None,                                  // "none"
/// Outline = Border.Dashed(2.Px(), CssColor.Hex("#abc")), // "2px dashed #abc"
/// </code>
/// </example>
public readonly struct Border {
    private readonly string _css;
    private Border(string css) { _css = css; }

    /// <summary>The CSS string this border serializes to.</summary>
    public string Css => _css;

    /// <summary>The CSS keyword <c>none</c> — equivalent to no border.</summary>
    public static Border None { get; } = new("none");

    /// <summary>A solid border of the given width and color.</summary>
    public static Border Solid(Length width, CssColor color) => new($"{width.ToCss()} solid {color.ToCss()}");

    /// <summary>A dashed border of the given width and color.</summary>
    public static Border Dashed(Length width, CssColor color) => new($"{width.ToCss()} dashed {color.ToCss()}");

    /// <summary>A dotted border of the given width and color.</summary>
    public static Border Dotted(Length width, CssColor color) => new($"{width.ToCss()} dotted {color.ToCss()}");

    /// <summary>A double border of the given width and color.</summary>
    public static Border Double(Length width, CssColor color) => new($"{width.ToCss()} double {color.ToCss()}");

    /// <summary>Constructs a custom border from explicit width, style, and color.</summary>
    public static Border Custom(Length width, BorderStyle style, CssColor color) =>
        new($"{width.ToCss()} {style.AsCss()} {color.ToCss()}");

    /// <summary>Returns the CSS string. Used by <see cref="Declarations"/> setters.</summary>
    public override string ToString() => _css;
}
