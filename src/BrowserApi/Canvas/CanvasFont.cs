using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Canvas;

public readonly struct CanvasFont : ICssValue, IEquatable<CanvasFont> {
    private readonly double _sizePx;
    private readonly string _family;
    private readonly string? _weight;
    private readonly string? _style;

    private CanvasFont(double sizePx, string family, string? weight, string? style) {
        _sizePx = sizePx;
        _family = family;
        _weight = weight;
        _style = style;
    }

    public static CanvasFont Of(double sizePx, string family) => new(sizePx, family, null, null);

    public CanvasFont Bold() => new(_sizePx, _family, "bold", _style);
    public CanvasFont Italic() => new(_sizePx, _family, _weight, "italic");
    public CanvasFont WithWeight(string weight) => new(_sizePx, _family, weight, _style);
    public CanvasFont WithStyle(string style) => new(_sizePx, _family, _weight, style);
    public CanvasFont WithSize(double sizePx) => new(sizePx, _family, _weight, _style);
    public CanvasFont WithFamily(string family) => new(_sizePx, family, _weight, _style);

    public string ToCss() {
        var parts = new List<string>(4);
        if (_style != null) parts.Add(_style);
        if (_weight != null) parts.Add(_weight);
        parts.Add($"{FormatNumber(_sizePx)}px");
        parts.Add(_family);
        return string.Join(" ", parts);
    }

    public override string ToString() => ToCss();

    public static implicit operator string(CanvasFont font) => font.ToCss();

    // Equality
    public bool Equals(CanvasFont other) =>
        _sizePx == other._sizePx && _family == other._family &&
        _weight == other._weight && _style == other._style;
    public override bool Equals(object? obj) => obj is CanvasFont other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_sizePx, _family, _weight, _style);
    public static bool operator ==(CanvasFont left, CanvasFont right) => left.Equals(right);
    public static bool operator !=(CanvasFont left, CanvasFont right) => !left.Equals(right);
}
