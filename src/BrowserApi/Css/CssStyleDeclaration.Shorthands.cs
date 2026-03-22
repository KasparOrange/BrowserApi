namespace BrowserApi.Css;

/// <summary>
/// Provides shorthand methods on <see cref="CssStyleDeclaration"/> for setting CSS shorthand
/// properties that expand into multiple individual properties.
/// </summary>
/// <remarks>
/// <para>
/// CSS shorthand properties like <c>margin</c>, <c>padding</c>, and <c>gap</c> set multiple
/// related properties at once. Since the generated <see cref="CssStyleDeclaration"/> exposes
/// only the individual longhand properties (e.g., <c>MarginTop</c>, <c>MarginRight</c>, etc.),
/// these hand-written methods provide the ergonomic shorthand equivalents.
/// </para>
/// <para>
/// The overloads mirror the CSS shorthand syntax: a single value sets all sides, two values
/// set vertical/horizontal pairs, and four values set each side individually.
/// </para>
/// </remarks>
public partial class CssStyleDeclaration {
    // Margin

    /// <summary>
    /// Sets all four margin properties (<c>margin-top</c>, <c>margin-right</c>, <c>margin-bottom</c>,
    /// <c>margin-left</c>) to the same value.
    /// </summary>
    /// <param name="all">The margin value to apply to all four sides.</param>
    /// <example>
    /// <code>
    /// style.SetMargin(Length.Px(16)); // margin: 16px (all sides)
    /// </code>
    /// </example>
    /// <seealso cref="SetMargin(Length, Length)"/>
    /// <seealso cref="SetMargin(Length, Length, Length, Length)"/>
    public void SetMargin(Length all) =>
        MarginTop = MarginRight = MarginBottom = MarginLeft = all;

    /// <summary>
    /// Sets the vertical and horizontal margin pairs separately, equivalent to the CSS
    /// shorthand <c>margin: vertical horizontal</c>.
    /// </summary>
    /// <param name="vertical">The margin value for top and bottom.</param>
    /// <param name="horizontal">The margin value for left and right.</param>
    /// <example>
    /// <code>
    /// style.SetMargin(Length.Px(8), Length.Px(16)); // margin: 8px 16px
    /// </code>
    /// </example>
    /// <seealso cref="SetMargin(Length)"/>
    /// <seealso cref="SetMargin(Length, Length, Length, Length)"/>
    public void SetMargin(Length vertical, Length horizontal) {
        MarginTop = MarginBottom = vertical;
        MarginRight = MarginLeft = horizontal;
    }

    /// <summary>
    /// Sets each margin property individually, equivalent to the CSS shorthand
    /// <c>margin: top right bottom left</c>.
    /// </summary>
    /// <param name="top">The top margin value.</param>
    /// <param name="right">The right margin value.</param>
    /// <param name="bottom">The bottom margin value.</param>
    /// <param name="left">The left margin value.</param>
    /// <example>
    /// <code>
    /// style.SetMargin(Length.Px(10), Length.Px(20), Length.Px(10), Length.Px(20));
    /// // margin: 10px 20px 10px 20px
    /// </code>
    /// </example>
    /// <seealso cref="SetMargin(Length)"/>
    /// <seealso cref="SetMargin(Length, Length)"/>
    public void SetMargin(Length top, Length right, Length bottom, Length left) {
        MarginTop = top;
        MarginRight = right;
        MarginBottom = bottom;
        MarginLeft = left;
    }

    // Padding

    /// <summary>
    /// Sets all four padding properties (<c>padding-top</c>, <c>padding-right</c>, <c>padding-bottom</c>,
    /// <c>padding-left</c>) to the same value.
    /// </summary>
    /// <param name="all">The padding value to apply to all four sides.</param>
    /// <example>
    /// <code>
    /// style.SetPadding(Length.Rem(1)); // padding: 1rem (all sides)
    /// </code>
    /// </example>
    /// <seealso cref="SetPadding(Length, Length)"/>
    /// <seealso cref="SetPadding(Length, Length, Length, Length)"/>
    public void SetPadding(Length all) =>
        PaddingTop = PaddingRight = PaddingBottom = PaddingLeft = all;

    /// <summary>
    /// Sets the vertical and horizontal padding pairs separately, equivalent to the CSS
    /// shorthand <c>padding: vertical horizontal</c>.
    /// </summary>
    /// <param name="vertical">The padding value for top and bottom.</param>
    /// <param name="horizontal">The padding value for left and right.</param>
    /// <example>
    /// <code>
    /// style.SetPadding(Length.Px(8), Length.Px(16)); // padding: 8px 16px
    /// </code>
    /// </example>
    /// <seealso cref="SetPadding(Length)"/>
    /// <seealso cref="SetPadding(Length, Length, Length, Length)"/>
    public void SetPadding(Length vertical, Length horizontal) {
        PaddingTop = PaddingBottom = vertical;
        PaddingRight = PaddingLeft = horizontal;
    }

    /// <summary>
    /// Sets each padding property individually, equivalent to the CSS shorthand
    /// <c>padding: top right bottom left</c>.
    /// </summary>
    /// <param name="top">The top padding value.</param>
    /// <param name="right">The right padding value.</param>
    /// <param name="bottom">The bottom padding value.</param>
    /// <param name="left">The left padding value.</param>
    /// <example>
    /// <code>
    /// style.SetPadding(Length.Px(10), Length.Px(20), Length.Px(10), Length.Px(20));
    /// // padding: 10px 20px 10px 20px
    /// </code>
    /// </example>
    /// <seealso cref="SetPadding(Length)"/>
    /// <seealso cref="SetPadding(Length, Length)"/>
    public void SetPadding(Length top, Length right, Length bottom, Length left) {
        PaddingTop = top;
        PaddingRight = right;
        PaddingBottom = bottom;
        PaddingLeft = left;
    }

    // Gap

    /// <summary>
    /// Sets both <c>row-gap</c> and <c>column-gap</c> to the same value, equivalent to the CSS
    /// shorthand <c>gap: all</c>.
    /// </summary>
    /// <param name="all">The gap value to apply to both rows and columns.</param>
    /// <example>
    /// <code>
    /// style.SetGap(Length.Px(16)); // gap: 16px
    /// </code>
    /// </example>
    /// <seealso cref="SetGap(Length, Length)"/>
    public void SetGap(Length all) =>
        RowGap = ColumnGap = all;

    /// <summary>
    /// Sets the row and column gap independently, equivalent to the CSS shorthand
    /// <c>gap: row column</c>.
    /// </summary>
    /// <param name="row">The gap between rows.</param>
    /// <param name="column">The gap between columns.</param>
    /// <example>
    /// <code>
    /// style.SetGap(Length.Px(8), Length.Px(16)); // gap: 8px 16px
    /// </code>
    /// </example>
    /// <seealso cref="SetGap(Length)"/>
    public void SetGap(Length row, Length column) {
        RowGap = row;
        ColumnGap = column;
    }
}
