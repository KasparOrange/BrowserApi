using BrowserApi.Common;

namespace BrowserApi.Css;

public readonly partial struct Shadow : ICssValue, IEquatable<Shadow> {
    private readonly string _value;

    public Shadow(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    // Sentinel
    public static Shadow None { get; } = new("none");

    // Box shadow
    public static Shadow Box(Length offsetX, Length offsetY,
        Length? blur = null, Length? spread = null,
        CssColor? color = null, bool inset = false) {
        var parts = new List<string>();
        if (inset) parts.Add("inset");
        parts.Add(offsetX.ToCss());
        parts.Add(offsetY.ToCss());
        if (blur is not null) parts.Add(blur.Value.ToCss());
        if (spread is not null) parts.Add(spread.Value.ToCss());
        if (color is not null) parts.Add(color.Value.ToCss());
        return new(string.Join(' ', parts));
    }

    // Text shadow
    public static Shadow Text(Length offsetX, Length offsetY,
        Length? blur = null, CssColor? color = null) {
        var parts = new List<string>();
        parts.Add(offsetX.ToCss());
        parts.Add(offsetY.ToCss());
        if (blur is not null) parts.Add(blur.Value.ToCss());
        if (color is not null) parts.Add(color.Value.ToCss());
        return new(string.Join(' ', parts));
    }

    // Combine multiple shadows
    public static Shadow Combine(params ReadOnlySpan<Shadow> shadows) {
        var parts = new string[shadows.Length];
        for (var i = 0; i < shadows.Length; i++)
            parts[i] = shadows[i].ToCss();
        return new(string.Join(", ", parts));
    }

    // Equality
    public bool Equals(Shadow other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Shadow other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Shadow left, Shadow right) => left.Equals(right);
    public static bool operator !=(Shadow left, Shadow right) => !left.Equals(right);
}
