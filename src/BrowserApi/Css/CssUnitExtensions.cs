namespace BrowserApi.Css;

public static class CssUnitExtensions {
    // Length
    public static Length Px(this int value) => Length.Px(value);
    public static Length Px(this double value) => Length.Px(value);
    public static Length Em(this double value) => Length.Em(value);
    public static Length Rem(this double value) => Length.Rem(value);
    public static Length Vh(this double value) => Length.Vh(value);
    public static Length Vw(this double value) => Length.Vw(value);

    // Duration
    public static Duration Ms(this int value) => Duration.Ms(value);
    public static Duration Ms(this double value) => Duration.Ms(value);
    public static Duration S(this double value) => Duration.S(value);

    // Angle
    public static Angle Deg(this int value) => Angle.Deg(value);
    public static Angle Deg(this double value) => Angle.Deg(value);

    // Percentage
    public static Percentage Percent(this int value) => Percentage.Of(value);
    public static Percentage Percent(this double value) => Percentage.Of(value);

    // Flex
    public static Flex Fr(this int value) => Flex.Fr(value);
    public static Flex Fr(this double value) => Flex.Fr(value);
}
