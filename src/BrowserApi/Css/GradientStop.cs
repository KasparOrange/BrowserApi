namespace BrowserApi.Css;

public readonly record struct GradientStop(CssColor Color, string? Position = null) {
    public string ToCss() => Position is null
        ? Color.ToCss()
        : $"{Color.ToCss()} {Position}";

    public static implicit operator GradientStop(CssColor color) => new(color);

    public static GradientStop At(CssColor color, Length position) =>
        new(color, position.ToCss());

    public static GradientStop At(CssColor color, Percentage position) =>
        new(color, position.ToCss());
}
