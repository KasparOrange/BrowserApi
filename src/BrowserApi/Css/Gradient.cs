using BrowserApi.Common;

namespace BrowserApi.Css;

public readonly partial struct Gradient : ICssValue, IEquatable<Gradient> {
    private readonly string _value;

    public Gradient(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    // Linear
    public static Gradient Linear(params ReadOnlySpan<GradientStop> stops) =>
        new($"linear-gradient({FormatStops(stops)})");

    public static Gradient Linear(Angle angle, params ReadOnlySpan<GradientStop> stops) =>
        new($"linear-gradient({angle.ToCss()}, {FormatStops(stops)})");

    // Radial
    public static Gradient Radial(params ReadOnlySpan<GradientStop> stops) =>
        new($"radial-gradient({FormatStops(stops)})");

    public static Gradient Radial(string shape, params ReadOnlySpan<GradientStop> stops) =>
        new($"radial-gradient({shape}, {FormatStops(stops)})");

    // Conic
    public static Gradient Conic(params ReadOnlySpan<GradientStop> stops) =>
        new($"conic-gradient({FormatStops(stops)})");

    public static Gradient Conic(Angle fromAngle, params ReadOnlySpan<GradientStop> stops) =>
        new($"conic-gradient(from {fromAngle.ToCss()}, {FormatStops(stops)})");

    // Repeating variants
    public static Gradient RepeatingLinear(Angle angle, params ReadOnlySpan<GradientStop> stops) =>
        new($"repeating-linear-gradient({angle.ToCss()}, {FormatStops(stops)})");

    public static Gradient RepeatingRadial(string shape, params ReadOnlySpan<GradientStop> stops) =>
        new($"repeating-radial-gradient({shape}, {FormatStops(stops)})");

    public static Gradient RepeatingConic(Angle fromAngle, params ReadOnlySpan<GradientStop> stops) =>
        new($"repeating-conic-gradient(from {fromAngle.ToCss()}, {FormatStops(stops)})");

    // Helper
    private static string FormatStops(ReadOnlySpan<GradientStop> stops) {
        var parts = new string[stops.Length];
        for (var i = 0; i < stops.Length; i++)
            parts[i] = stops[i].ToCss();
        return string.Join(", ", parts);
    }

    // Equality
    public bool Equals(Gradient other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Gradient other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Gradient left, Gradient right) => left.Equals(right);
    public static bool operator !=(Gradient left, Gradient right) => !left.Equals(right);
}
