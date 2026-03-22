using System.Globalization;

namespace BrowserApi.Css;

/// <summary>
/// Provides internal formatting utilities for CSS numeric values.
/// </summary>
/// <remarks>
/// This class ensures that all CSS numeric output uses invariant culture formatting
/// and avoids unnecessary decimal places. Whole numbers are rendered without a decimal
/// point (e.g., <c>"10"</c> instead of <c>"10.0000"</c>), while fractional values
/// retain up to four decimal digits (e.g., <c>"1.5"</c>, <c>"0.3333"</c>).
/// All CSS value types in the <see cref="BrowserApi.Css"/> namespace use this class
/// to guarantee consistent, locale-independent output.
/// </remarks>
internal static class CssFormatting {
    /// <summary>
    /// Formats a numeric value for use in CSS output.
    /// </summary>
    /// <param name="value">The numeric value to format.</param>
    /// <returns>
    /// A string representation of the value suitable for CSS. Whole numbers are formatted
    /// without a decimal point; fractional values use up to four decimal digits with
    /// invariant culture formatting.
    /// </returns>
    /// <example>
    /// <code>
    /// CssFormatting.FormatNumber(10.0);   // "10"
    /// CssFormatting.FormatNumber(1.5);    // "1.5"
    /// CssFormatting.FormatNumber(0.3333); // "0.3333"
    /// </code>
    /// </example>
    internal static string FormatNumber(double value) =>
        value == (int)value
            ? ((int)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
}
