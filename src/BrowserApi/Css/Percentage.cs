using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Percentage : IEquatable<Percentage> {
    public static Percentage Zero { get; } = new("0%");

    public static Percentage Of(double value) => new($"{FormatNumber(value)}%");
    public static Percentage Calc(string expression) => new($"calc({expression})");

    public bool Equals(Percentage other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Percentage other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Percentage left, Percentage right) => left.Equals(right);
    public static bool operator !=(Percentage left, Percentage right) => !left.Equals(right);
}
