using BrowserApi.Css;
using BrowserApi.Dom;

namespace BrowserApi.Canvas;

/// <summary>
/// A fluent builder for configuring color stops on a <see cref="CanvasGradient"/>.
/// </summary>
/// <remarks>
/// <para>
/// Create instances via <see cref="CanvasExtensions.LinearGradient"/>,
/// <see cref="CanvasExtensions.RadialGradient"/>, or <see cref="CanvasExtensions.ConicGradient"/>.
/// Add color stops with <see cref="AddStop(double, CssColor)"/> or
/// <see cref="AddStop(double, string)"/>, then either call <see cref="Build"/> or rely on the
/// implicit conversion to <see cref="CanvasGradient"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Linear gradient from red to blue
/// var gradient = ctx.LinearGradient(0, 0, 200, 0)
///     .AddStop(0, CssColor.Named("red"))
///     .AddStop(0.5, CssColor.Named("white"))
///     .AddStop(1, CssColor.Named("blue"))
///     .Build();
///
/// ctx.SetFill(gradient).FillRect(0, 0, 200, 100);
///
/// // Implicit conversion -- no need to call Build()
/// CanvasGradient g = ctx.RadialGradient(100, 100, 0, 100, 100, 80)
///     .AddStop(0, "yellow")
///     .AddStop(1, "green");
/// </code>
/// </example>
/// <seealso cref="CanvasExtensions.LinearGradient"/>
/// <seealso cref="CanvasExtensions.RadialGradient"/>
/// <seealso cref="CanvasExtensions.ConicGradient"/>
/// <seealso cref="CanvasGradient"/>
public sealed class GradientBuilder {
    private readonly CanvasGradient _gradient;

    internal GradientBuilder(CanvasGradient gradient) {
        _gradient = gradient;
    }

    /// <summary>
    /// Adds a color stop to the gradient using a <see cref="CssColor"/> value.
    /// </summary>
    /// <param name="offset">
    /// The position of the color stop, between <c>0.0</c> (start) and <c>1.0</c> (end).
    /// </param>
    /// <param name="color">The color at this stop, which is serialized via <see cref="CssColor.ToCss"/>.</param>
    /// <returns>This builder for method chaining.</returns>
    public GradientBuilder AddStop(double offset, CssColor color) {
        _gradient.AddColorStop(offset, color.ToCss());
        return this;
    }

    /// <summary>
    /// Adds a color stop to the gradient using a CSS color string.
    /// </summary>
    /// <param name="offset">
    /// The position of the color stop, between <c>0.0</c> (start) and <c>1.0</c> (end).
    /// </param>
    /// <param name="color">A CSS color string (e.g., <c>"red"</c>, <c>"#ff0000"</c>, <c>"rgb(255,0,0)"</c>).</param>
    /// <returns>This builder for method chaining.</returns>
    public GradientBuilder AddStop(double offset, string color) {
        _gradient.AddColorStop(offset, color);
        return this;
    }

    /// <summary>
    /// Returns the underlying <see cref="CanvasGradient"/> with all configured color stops.
    /// </summary>
    /// <returns>The configured <see cref="CanvasGradient"/>.</returns>
    public CanvasGradient Build() => _gradient;

    /// <summary>
    /// Implicitly converts a <see cref="GradientBuilder"/> to a <see cref="CanvasGradient"/>,
    /// allowing the builder to be used directly wherever a gradient is expected.
    /// </summary>
    /// <param name="builder">The gradient builder to convert.</param>
    /// <returns>The underlying <see cref="CanvasGradient"/>.</returns>
    public static implicit operator CanvasGradient(GradientBuilder builder) => builder._gradient;
}
