using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Duration : IEquatable<Duration> {
    public static Duration Zero { get; } = new("0s");

    public static Duration S(double value) => new($"{FormatNumber(value)}s");
    public static Duration Ms(double value) => new($"{FormatNumber(value)}ms");
    public static Duration Calc(string expression) => new($"calc({expression})");

    public bool Equals(Duration other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Duration other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Duration left, Duration right) => left.Equals(right);
    public static bool operator !=(Duration left, Duration right) => !left.Equals(right);
}
