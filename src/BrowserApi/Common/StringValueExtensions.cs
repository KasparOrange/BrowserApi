using System.Reflection;

namespace BrowserApi.Common;

/// <summary>
/// Provides extension methods for converting enum values to their WebIDL string representations.
/// </summary>
/// <remarks>
/// This class complements <see cref="StringValueAttribute"/> by offering a convenient way to
/// retrieve the original string value from any enum field decorated with that attribute.
/// If an enum field does not have a <see cref="StringValueAttribute"/>, the standard
/// <see cref="Enum.ToString()"/> result is returned as a fallback.
/// </remarks>
/// <seealso cref="StringValueAttribute"/>
public static class StringValueExtensions {
    /// <summary>
    /// Returns the WebIDL string value associated with the specified enum value,
    /// or the default <see cref="Enum.ToString()"/> result if no
    /// <see cref="StringValueAttribute"/> is present.
    /// </summary>
    /// <typeparam name="TEnum">
    /// The enum type. Must be a value type that inherits from <see cref="Enum"/>.
    /// </typeparam>
    /// <param name="value">The enum value to convert to its string representation.</param>
    /// <returns>
    /// The <see cref="StringValueAttribute.Value"/> if the enum field is decorated with
    /// <see cref="StringValueAttribute"/>; otherwise, the result of <see cref="Enum.ToString()"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// public enum ScrollBehavior {
    ///     [StringValue("auto")]
    ///     Auto,
    ///     [StringValue("smooth")]
    ///     Smooth,
    /// }
    ///
    /// var result = ScrollBehavior.Smooth.ToStringValue(); // "smooth"
    /// </code>
    /// </example>
    public static string ToStringValue<TEnum>(this TEnum value) where TEnum : struct, Enum {
        var member = typeof(TEnum).GetField(value.ToString()!);
        var attr = member?.GetCustomAttribute<StringValueAttribute>();
        return attr?.Value ?? value.ToString()!;
    }
}
