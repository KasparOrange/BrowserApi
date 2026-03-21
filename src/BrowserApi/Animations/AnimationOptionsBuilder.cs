namespace BrowserApi.Animations;

public sealed class AnimationOptionsBuilder {
    private object? _duration;
    private double? _delay;
    private double? _endDelay;
    private string? _easing;
    private FillMode? _fill;
    private PlaybackDirection? _direction;
    private double? _iterations;
    private double? _iterationStart;
    private string? _id;
    private CompositeOperation? _composite;

    public AnimationOptionsBuilder Duration(double ms) {
        _duration = ms;
        return this;
    }

    public AnimationOptionsBuilder Delay(double ms) {
        _delay = ms;
        return this;
    }

    public AnimationOptionsBuilder EndDelay(double ms) {
        _endDelay = ms;
        return this;
    }

    public AnimationOptionsBuilder Easing(string easing) {
        _easing = easing;
        return this;
    }

    public AnimationOptionsBuilder Fill(FillMode fill) {
        _fill = fill;
        return this;
    }

    public AnimationOptionsBuilder Direction(PlaybackDirection direction) {
        _direction = direction;
        return this;
    }

    public AnimationOptionsBuilder Iterations(double count) {
        _iterations = count;
        return this;
    }

    public AnimationOptionsBuilder IterationStart(double start) {
        _iterationStart = start;
        return this;
    }

    public AnimationOptionsBuilder Id(string id) {
        _id = id;
        return this;
    }

    public AnimationOptionsBuilder Composite(CompositeOperation composite) {
        _composite = composite;
        return this;
    }

    public KeyframeAnimationOptions Build() => new() {
        Duration = _duration,
        Delay = _delay,
        EndDelay = _endDelay,
        Easing = _easing,
        Fill = _fill,
        Direction = _direction,
        Iterations = _iterations,
        IterationStart = _iterationStart,
        Id = _id,
        Composite = _composite
    };

    public static implicit operator KeyframeAnimationOptions(AnimationOptionsBuilder builder) =>
        builder.Build();
}
