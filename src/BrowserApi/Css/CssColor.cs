using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct CssColor : IEquatable<CssColor> {
    // Named colors
    public static CssColor Transparent { get; } = new("transparent");
    public static CssColor Black { get; } = new("black");
    public static CssColor White { get; } = new("white");
    public static CssColor Red { get; } = new("red");
    public static CssColor Green { get; } = new("green");
    public static CssColor Blue { get; } = new("blue");
    public static CssColor Yellow { get; } = new("yellow");
    public static CssColor Cyan { get; } = new("cyan");
    public static CssColor Magenta { get; } = new("magenta");
    public static CssColor Orange { get; } = new("orange");
    public static CssColor Purple { get; } = new("purple");
    public static CssColor Gray { get; } = new("gray");

    // CSS keywords
    public static CssColor Inherit { get; } = new("inherit");
    public static CssColor CurrentColor { get; } = new("currentcolor");

    // Factories
    public static CssColor Rgb(int r, int g, int b) =>
        new($"rgb({r}, {g}, {b})");

    public static CssColor Rgba(int r, int g, int b, double a) =>
        new($"rgba({r}, {g}, {b}, {FormatNumber(a)})");

    public static CssColor Hsl(int h, int s, int l) =>
        new($"hsl({h}, {s}%, {l}%)");

    public static CssColor Hsla(int h, int s, int l, double a) =>
        new($"hsla({h}, {s}%, {l}%, {FormatNumber(a)})");

    public static CssColor Hex(string hex) {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        if (hex[0] != '#' || (hex.Length != 4 && hex.Length != 7))
            throw new ArgumentException($"Invalid hex color format: '{hex}'. Expected '#rgb' or '#rrggbb'.", nameof(hex));
        return new(hex);
    }

    // Equality
    public bool Equals(CssColor other) => _value == other._value;
    public override bool Equals(object? obj) => obj is CssColor other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(CssColor left, CssColor right) => left.Equals(right);
    public static bool operator !=(CssColor left, CssColor right) => !left.Equals(right);

}
