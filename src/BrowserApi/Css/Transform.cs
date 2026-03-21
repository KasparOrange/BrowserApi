using BrowserApi.Common;
using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Css;

public readonly partial struct Transform : ICssValue, IEquatable<Transform> {
    private readonly string _value;

    public Transform(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    // Sentinel
    public static Transform None { get; } = new("none");

    // Static factories
    public static Transform Translate(Length x, Length y) =>
        new($"translate({x.ToCss()}, {y.ToCss()})");

    public static Transform TranslateX(Length x) =>
        new($"translateX({x.ToCss()})");

    public static Transform TranslateY(Length y) =>
        new($"translateY({y.ToCss()})");

    public static Transform Rotate(Angle angle) =>
        new($"rotate({angle.ToCss()})");

    public static Transform Scale(double factor) =>
        new($"scale({FormatNumber(factor)})");

    public static Transform Scale(double x, double y) =>
        new($"scale({FormatNumber(x)}, {FormatNumber(y)})");

    public static Transform ScaleX(double x) =>
        new($"scaleX({FormatNumber(x)})");

    public static Transform ScaleY(double y) =>
        new($"scaleY({FormatNumber(y)})");

    public static Transform SkewX(Angle angle) =>
        new($"skewX({angle.ToCss()})");

    public static Transform SkewY(Angle angle) =>
        new($"skewY({angle.ToCss()})");

    public static Transform Skew(Angle x, Angle y) =>
        new($"skew({x.ToCss()}, {y.ToCss()})");

    public static Transform Matrix(double a, double b, double c, double d, double e, double f) =>
        new($"matrix({FormatNumber(a)}, {FormatNumber(b)}, {FormatNumber(c)}, {FormatNumber(d)}, {FormatNumber(e)}, {FormatNumber(f)})");

    // Chaining
    public Transform Then(Transform other) => new($"{_value} {other._value}");
    public Transform ThenTranslate(Length x, Length y) => Then(Translate(x, y));
    public Transform ThenRotate(Angle angle) => Then(Rotate(angle));
    public Transform ThenScale(double factor) => Then(Scale(factor));
    public Transform ThenScale(double x, double y) => Then(Scale(x, y));
    public Transform ThenSkewX(Angle angle) => Then(SkewX(angle));
    public Transform ThenSkewY(Angle angle) => Then(SkewY(angle));

    // Equality
    public bool Equals(Transform other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Transform other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Transform left, Transform right) => left.Equals(right);
    public static bool operator !=(Transform left, Transform right) => !left.Equals(right);
}
