using BrowserApi.Css;
using BrowserApi.Dom;

namespace BrowserApi.Canvas;

public sealed class GradientBuilder {
    private readonly CanvasGradient _gradient;

    internal GradientBuilder(CanvasGradient gradient) {
        _gradient = gradient;
    }

    public GradientBuilder AddStop(double offset, CssColor color) {
        _gradient.AddColorStop(offset, color.ToCss());
        return this;
    }

    public GradientBuilder AddStop(double offset, string color) {
        _gradient.AddColorStop(offset, color);
        return this;
    }

    public CanvasGradient Build() => _gradient;

    public static implicit operator CanvasGradient(GradientBuilder builder) => builder._gradient;
}
