using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Resolution : ICssValue, IEquatable<Resolution> {
    private readonly string _value;

    public Resolution(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    public static Resolution Dpi(double value) => new($"{FormatNumber(value)}dpi");
    public static Resolution Dpcm(double value) => new($"{FormatNumber(value)}dpcm");
    public static Resolution Dppx(double value) => new($"{FormatNumber(value)}dppx");
    public static Resolution Calc(string expression) => new($"calc({expression})");

    public bool Equals(Resolution other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Resolution other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Resolution left, Resolution right) => left.Equals(right);
    public static bool operator !=(Resolution left, Resolution right) => !left.Equals(right);
}
