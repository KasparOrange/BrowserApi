using BrowserApi.Canvas;

namespace BrowserApi.Tests.Canvas;

public class CanvasFontTests {
    [Fact]
    public void Of_creates_font_with_size_and_family() {
        var font = CanvasFont.Of(16, "Arial");
        Assert.Equal("16px Arial", font.ToCss());
    }

    [Fact]
    public void Bold_adds_bold_weight() {
        var font = CanvasFont.Of(24, "Inter").Bold();
        Assert.Equal("bold 24px Inter", font.ToCss());
    }

    [Fact]
    public void Italic_adds_italic_style() {
        var font = CanvasFont.Of(14, "Helvetica").Italic();
        Assert.Equal("italic 14px Helvetica", font.ToCss());
    }

    [Fact]
    public void Bold_and_italic_combined() {
        var font = CanvasFont.Of(18, "Georgia").Bold().Italic();
        Assert.Equal("italic bold 18px Georgia", font.ToCss());
    }

    [Fact]
    public void WithWeight_sets_custom_weight() {
        var font = CanvasFont.Of(16, "Arial").WithWeight("600");
        Assert.Equal("600 16px Arial", font.ToCss());
    }

    [Fact]
    public void WithStyle_sets_custom_style() {
        var font = CanvasFont.Of(16, "Arial").WithStyle("oblique");
        Assert.Equal("oblique 16px Arial", font.ToCss());
    }

    [Fact]
    public void WithSize_changes_size() {
        var font = CanvasFont.Of(16, "Arial").WithSize(32);
        Assert.Equal("32px Arial", font.ToCss());
    }

    [Fact]
    public void WithFamily_changes_family() {
        var font = CanvasFont.Of(16, "Arial").WithFamily("Inter");
        Assert.Equal("16px Inter", font.ToCss());
    }

    [Fact]
    public void Decimal_size_formats_correctly() {
        var font = CanvasFont.Of(14.5, "Arial");
        Assert.Equal("14.5px Arial", font.ToCss());
    }

    [Fact]
    public void Implicit_conversion_to_string() {
        string font = CanvasFont.Of(16, "Arial").Bold();
        Assert.Equal("bold 16px Arial", font);
    }

    [Fact]
    public void ToString_returns_ToCss() {
        var font = CanvasFont.Of(16, "Arial");
        Assert.Equal(font.ToCss(), font.ToString());
    }

    [Fact]
    public void Equality_same_fonts_are_equal() {
        var a = CanvasFont.Of(16, "Arial").Bold();
        var b = CanvasFont.Of(16, "Arial").Bold();
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_different_fonts_are_not_equal() {
        var a = CanvasFont.Of(16, "Arial");
        var b = CanvasFont.Of(18, "Arial");
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
