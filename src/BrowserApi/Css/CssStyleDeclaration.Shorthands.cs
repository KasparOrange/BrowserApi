namespace BrowserApi.Css;

public partial class CssStyleDeclaration {
    // Margin
    public void SetMargin(Length all) =>
        MarginTop = MarginRight = MarginBottom = MarginLeft = all;

    public void SetMargin(Length vertical, Length horizontal) {
        MarginTop = MarginBottom = vertical;
        MarginRight = MarginLeft = horizontal;
    }

    public void SetMargin(Length top, Length right, Length bottom, Length left) {
        MarginTop = top;
        MarginRight = right;
        MarginBottom = bottom;
        MarginLeft = left;
    }

    // Padding
    public void SetPadding(Length all) =>
        PaddingTop = PaddingRight = PaddingBottom = PaddingLeft = all;

    public void SetPadding(Length vertical, Length horizontal) {
        PaddingTop = PaddingBottom = vertical;
        PaddingRight = PaddingLeft = horizontal;
    }

    public void SetPadding(Length top, Length right, Length bottom, Length left) {
        PaddingTop = top;
        PaddingRight = right;
        PaddingBottom = bottom;
        PaddingLeft = left;
    }

    // Gap
    public void SetGap(Length all) =>
        RowGap = ColumnGap = all;

    public void SetGap(Length row, Length column) {
        RowGap = row;
        ColumnGap = column;
    }
}
