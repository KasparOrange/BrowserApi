using BrowserApi.Common;

namespace BrowserApi.Css.Authoring;

/// <summary>
/// A CSS keyword value, optionally with the <c>!important</c> priority flag.
/// Used as the property-setter type for keyword-typed properties (Display,
/// Position, Cursor, …) so both <c>Display.Flex</c> and
/// <c>Display.Flex.Important</c> can flow into the setter without breaking
/// the enum-based ergonomics.
/// </summary>
/// <typeparam name="TEnum">The keyword enum type.</typeparam>
/// <remarks>
/// <para>
/// Two implicit conversions feed it: from the bare enum value (no priority)
/// and from <see cref="EnumImportantExtensions"/>'s extension property that
/// returns the same wrapper with the priority flag set.
/// </para>
/// <para>
/// Per spec §14, this is the C# 14 extension-property approach to
/// <c>.Important</c> for enum keywords — preserves switch-exhaustiveness
/// and zero-allocation while still supporting the priority flag.
/// </para>
/// </remarks>
public readonly struct Keyword<TEnum> : ICssValue where TEnum : System.Enum {
    private readonly string _css;

    /// <summary>Wraps a pre-rendered CSS keyword string.</summary>
    public Keyword(string css) { _css = css; }

    /// <inheritdoc/>
    public string ToCss() => _css;

    /// <summary>Implicit conversion from the bare enum value — emits the
    /// kebab-cased keyword with no priority flag.</summary>
    public static implicit operator Keyword<TEnum>(TEnum value) =>
        new(KeywordExtensions.AsCss(value));
}

/// <summary>
/// C# 14 extension property that gives every CSS keyword enum a
/// <c>.Important</c> property returning a <see cref="Keyword{TEnum}"/> with
/// the <c>!important</c> priority flag set.
/// </summary>
/// <example>
/// <code>
/// Display = Display.None.Important;     // → "display: none !important;"
/// Position = Position.Absolute.Important;
/// </code>
/// </example>
public static class EnumImportantExtensions {
    extension<TEnum>(TEnum value) where TEnum : System.Enum {
        /// <summary>Returns this keyword value with the <c>!important</c>
        /// priority flag appended to its CSS output.</summary>
        public Keyword<TEnum> Important =>
            new(KeywordExtensions.AsCss(value) + " !important");
    }
}
