using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Flex : ICssValue, IEquatable<Flex> {
    private readonly string _value;

    public Flex(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    public static Flex Fr(double value) => new($"{FormatNumber(value)}fr");

    public bool Equals(Flex other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Flex other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Flex left, Flex right) => left.Equals(right);
    public static bool operator !=(Flex left, Flex right) => !left.Equals(right);
}
