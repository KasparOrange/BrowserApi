using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Length : IEquatable<Length> {
    public static Length Zero { get; } = new("0");
    public static Length Auto { get; } = new("auto");

    public static Length Px(double value) => new($"{FormatNumber(value)}px");
    public static Length Em(double value) => new($"{FormatNumber(value)}em");
    public static Length Rem(double value) => new($"{FormatNumber(value)}rem");
    public static Length Vh(double value) => new($"{FormatNumber(value)}vh");
    public static Length Vw(double value) => new($"{FormatNumber(value)}vw");
    public static Length Percent(double value) => new($"{FormatNumber(value)}%");
    public static Length Calc(string expression) => new($"calc({expression})");

    // Implicit conversions — Px is the default unit
    public static implicit operator Length(int value) => Px(value);
    public static implicit operator Length(double value) => Px(value);

    // Arithmetic → calc expressions
    public static Length operator +(Length a, Length b) => new($"calc({a.ToCss()} + {b.ToCss()})");
    public static Length operator -(Length a, Length b) => new($"calc({a.ToCss()} - {b.ToCss()})");
    public static Length operator -(Length a) => new($"calc(-1 * {a.ToCss()})");

    // Equality
    public bool Equals(Length other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Length other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Length left, Length right) => left.Equals(right);
    public static bool operator !=(Length left, Length right) => !left.Equals(right);
}
