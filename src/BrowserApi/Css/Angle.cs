using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Angle : IEquatable<Angle> {
    public static Angle Zero { get; } = new("0deg");

    public static Angle Deg(double value) => new($"{FormatNumber(value)}deg");
    public static Angle Rad(double value) => new($"{FormatNumber(value)}rad");
    public static Angle Grad(double value) => new($"{FormatNumber(value)}grad");
    public static Angle Turn(double value) => new($"{FormatNumber(value)}turn");
    public static Angle Calc(string expression) => new($"calc({expression})");

    public bool Equals(Angle other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Angle other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Angle left, Angle right) => left.Equals(right);
    public static bool operator !=(Angle left, Angle right) => !left.Equals(right);
}
