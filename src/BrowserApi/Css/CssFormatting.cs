using System.Globalization;

namespace BrowserApi.Css;

internal static class CssFormatting {
    internal static string FormatNumber(double value) =>
        value == (int)value
            ? ((int)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
}
