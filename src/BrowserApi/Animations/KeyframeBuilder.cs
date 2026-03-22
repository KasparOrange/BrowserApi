using System.Reflection;

namespace BrowserApi.Animations;

/// <summary>
/// A fluent builder for constructing an array of keyframe dictionaries for the Web Animations API.
/// </summary>
/// <remarks>
/// <para>
/// Keyframes define the start, end, and intermediate states of an animation. Each frame is
/// specified as an anonymous object whose properties correspond to CSS properties (e.g.,
/// <c>opacity</c>, <c>transform</c>). The builder converts these objects to dictionaries
/// using reflection.
/// </para>
/// <para>
/// Optionally, each frame can include an <c>offset</c> (0.0 to 1.0) and/or an <c>easing</c>
/// string to control timing at that specific keyframe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var keyframes = new KeyframeBuilder()
///     .AddFrame(new { opacity = 0, transform = "scale(0.5)" })
///     .AddFrame(new { opacity = 1, transform = "scale(1)" }, offset: 0.6)
///     .AddFrame(new { opacity = 1, transform = "scale(1.1)" }, easing: Easing.EaseOut)
///     .Build();
///
/// element.Animate(keyframes, 500);
/// </code>
/// </example>
/// <seealso cref="AnimationOptionsBuilder"/>
/// <seealso cref="AnimateExtensions"/>
/// <seealso cref="Easing"/>
public sealed class KeyframeBuilder {
    private readonly List<Dictionary<string, object>> _frames = [];

    /// <summary>
    /// Adds a keyframe with the specified CSS properties. The offset is distributed
    /// evenly across all frames by the browser.
    /// </summary>
    /// <param name="properties">
    /// An object whose public properties map to CSS property values
    /// (e.g., <c>new { opacity = 0, transform = "translateX(0)" }</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public KeyframeBuilder AddFrame(object properties) {
        _frames.Add(ToDictionary(properties));
        return this;
    }

    /// <summary>
    /// Adds a keyframe with the specified CSS properties at a specific offset in the animation timeline.
    /// </summary>
    /// <param name="properties">
    /// An object whose public properties map to CSS property values.
    /// </param>
    /// <param name="offset">
    /// The position in the animation timeline, from <c>0.0</c> (start) to <c>1.0</c> (end).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public KeyframeBuilder AddFrame(object properties, double offset) {
        var dict = ToDictionary(properties);
        dict["offset"] = offset;
        _frames.Add(dict);
        return this;
    }

    /// <summary>
    /// Adds a keyframe with the specified CSS properties and a per-keyframe easing function.
    /// </summary>
    /// <param name="properties">
    /// An object whose public properties map to CSS property values.
    /// </param>
    /// <param name="easing">
    /// A CSS easing string (e.g., <c>"ease-in"</c>, a <c>cubic-bezier(...)</c> value, or a
    /// constant from <see cref="Easing"/>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public KeyframeBuilder AddFrame(object properties, string easing) {
        var dict = ToDictionary(properties);
        dict["easing"] = easing;
        _frames.Add(dict);
        return this;
    }

    /// <summary>
    /// Adds a keyframe with the specified CSS properties, offset, and per-keyframe easing function.
    /// </summary>
    /// <param name="properties">
    /// An object whose public properties map to CSS property values.
    /// </param>
    /// <param name="offset">
    /// The position in the animation timeline, from <c>0.0</c> (start) to <c>1.0</c> (end).
    /// </param>
    /// <param name="easing">A CSS easing string for this keyframe transition.</param>
    /// <returns>This builder for method chaining.</returns>
    public KeyframeBuilder AddFrame(object properties, double offset, string easing) {
        var dict = ToDictionary(properties);
        dict["offset"] = offset;
        dict["easing"] = easing;
        _frames.Add(dict);
        return this;
    }

    /// <summary>
    /// Builds the keyframe array suitable for passing to the Web Animations API.
    /// </summary>
    /// <returns>An array of keyframe dictionaries.</returns>
    public object Build() => _frames.ToArray();

    private static Dictionary<string, object> ToDictionary(object obj) {
        var dict = new Dictionary<string, object>();
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var value = prop.GetValue(obj);
            if (value is not null)
                dict[prop.Name] = value;
        }
        return dict;
    }
}
