using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Easing : ICssValue, IEquatable<Easing> {
    private readonly string _value;

    public Easing(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    // Named keywords
    public static Easing Ease { get; } = new("ease");
    public static Easing Linear { get; } = new("linear");
    public static Easing EaseIn { get; } = new("ease-in");
    public static Easing EaseOut { get; } = new("ease-out");
    public static Easing EaseInOut { get; } = new("ease-in-out");
    public static Easing StepStart { get; } = new("step-start");
    public static Easing StepEnd { get; } = new("step-end");

    // Parametric factories
    public static Easing CubicBezier(double x1, double y1, double x2, double y2) =>
        new($"cubic-bezier({FormatNumber(x1)}, {FormatNumber(y1)}, {FormatNumber(x2)}, {FormatNumber(y2)})");

    public static Easing Steps(int count, string? jumpTerm = null) =>
        jumpTerm is null ? new($"steps({count})") : new($"steps({count}, {jumpTerm})");

    // Equality
    public bool Equals(Easing other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Easing other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Easing left, Easing right) => left.Equals(right);
    public static bool operator !=(Easing left, Easing right) => !left.Equals(right);
}
