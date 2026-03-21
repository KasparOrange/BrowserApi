using static BrowserApi.Css.CssFormatting;

namespace BrowserApi.Animations;

public static class Easing {
    // Named easings
    public static string Linear => "linear";
    public static string Ease => "ease";
    public static string EaseIn => "ease-in";
    public static string EaseOut => "ease-out";
    public static string EaseInOut => "ease-in-out";

    // Factories
    public static string CubicBezier(double x1, double y1, double x2, double y2) =>
        $"cubic-bezier({FormatNumber(x1)}, {FormatNumber(y1)}, {FormatNumber(x2)}, {FormatNumber(y2)})";

    public static string Steps(int count, string? jumpTerm = null) =>
        jumpTerm is null ? $"steps({count})" : $"steps({count}, {jumpTerm})";

    // Common curves (cubic-bezier values)
    public static string EaseInSine => "cubic-bezier(0.12, 0, 0.39, 0)";
    public static string EaseOutSine => "cubic-bezier(0.61, 1, 0.88, 1)";
    public static string EaseInOutSine => "cubic-bezier(0.37, 0, 0.63, 1)";

    public static string EaseInQuad => "cubic-bezier(0.11, 0, 0.5, 0)";
    public static string EaseOutQuad => "cubic-bezier(0.5, 1, 0.89, 1)";
    public static string EaseInOutQuad => "cubic-bezier(0.45, 0, 0.55, 1)";

    public static string EaseInCubic => "cubic-bezier(0.32, 0, 0.67, 0)";
    public static string EaseOutCubic => "cubic-bezier(0.33, 1, 0.68, 1)";
    public static string EaseInOutCubic => "cubic-bezier(0.65, 0, 0.35, 1)";
}
