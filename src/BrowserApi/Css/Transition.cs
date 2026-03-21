using BrowserApi.Common;

namespace BrowserApi.Css;

public readonly partial struct Transition : ICssValue, IEquatable<Transition> {
    private readonly string _value;

    public Transition(string value) => _value = value;

    public string ToCss() => _value;
    public override string ToString() => _value;

    // Sentinel
    public static Transition None { get; } = new("none");

    // Factories
    public static Transition For(string property, Duration duration,
        Easing? timingFunction = null, Duration? delay = null) {
        var parts = new List<string> { property, duration.ToCss() };
        if (timingFunction is not null) parts.Add(timingFunction.Value.ToCss());
        if (delay is not null) parts.Add(delay.Value.ToCss());
        return new(string.Join(' ', parts));
    }

    public static Transition All(Duration duration,
        Easing? timingFunction = null, Duration? delay = null) =>
        For("all", duration, timingFunction, delay);

    // Combine multiple transitions
    public static Transition Combine(params ReadOnlySpan<Transition> transitions) {
        var parts = new string[transitions.Length];
        for (var i = 0; i < transitions.Length; i++)
            parts[i] = transitions[i].ToCss();
        return new(string.Join(", ", parts));
    }

    // Equality
    public bool Equals(Transition other) => _value == other._value;
    public override bool Equals(object? obj) => obj is Transition other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(Transition left, Transition right) => left.Equals(right);
    public static bool operator !=(Transition left, Transition right) => !left.Equals(right);
}
