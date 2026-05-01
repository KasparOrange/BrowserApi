using BrowserApi.Css;
using BrowserApi.Css.Authoring;
using StyleSheet = BrowserApi.Css.Authoring.StyleSheet;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for GridTemplate values and the top-level CSS function helpers.</summary>
public class GridAndFunctionsTests {
    [Fact]
    public void Grid_repeat_emits_repeat_function() {
        var t = GridTemplate.Repeat(3, Flex.Fr(1));
        Assert.Equal("repeat(3, 1fr)", t.ToCss());
    }

    [Fact]
    public void Grid_repeat_with_auto_fill_minmax() {
        var t = GridTemplate.Repeat(GridTemplate.AutoFill, GridTemplate.MinMax(Length.Px(200), Flex.Fr(1)));
        Assert.Equal("repeat(auto-fill, minmax(200px, 1fr))", t.ToCss());
    }

    [Fact]
    public void Grid_template_string_implicitly_converts() {
        GridTemplate t = "1fr 2fr 1fr";
        Assert.Equal("1fr 2fr 1fr", t.ToCss());
    }

    [Fact]
    public void Url_function_quotes_path() {
        var u = CssFn.Url("/images/logo.svg");
        Assert.Equal("url(\"/images/logo.svg\")", u.ToCss());
    }

    [Fact]
    public void Data_url_includes_mime_and_base64() {
        var u = CssFn.DataUrl("image/svg+xml", "PHN2Zw==");
        Assert.Equal("url(\"data:image/svg+xml;base64,PHN2Zw==\")", u.ToCss());
    }

    [Fact]
    public void Css_string_escapes_quotes() {
        var s = CssFn.String("hello \"world\"");
        Assert.Equal("\"hello \\\"world\\\"\"", s.ToCss());
    }

    [Fact]
    public void Env_returns_env_expression() {
        var e = CssFn.Env("safe-area-inset-top");
        Assert.Equal("env(safe-area-inset-top)", e.ToCss());
    }

    [Fact]
    public void Safe_area_helpers_resolve_to_env_calls() {
        Assert.Equal("env(safe-area-inset-top)",    CssFn.SafeArea.Top.ToCss());
        Assert.Equal("env(safe-area-inset-right)",  CssFn.SafeArea.Right.ToCss());
        Assert.Equal("env(safe-area-inset-bottom)", CssFn.SafeArea.Bottom.ToCss());
        Assert.Equal("env(safe-area-inset-left)",   CssFn.SafeArea.Left.ToCss());
    }

    [Fact]
    public void Env_implicitly_converts_to_length_for_padding_etc() {
        Length l = CssFn.SafeArea.Top;
        Assert.Equal("env(safe-area-inset-top)", l.ToCss());
    }

    private class GridStyles : StyleSheet {
        public static readonly Class Layout = new() {
            Display = Display.Grid,
            GridTemplateColumns = GridTemplate.Repeat(3, Flex.Fr(1)),
            Gap = Length.Px(16),
        };
    }

    [Fact]
    public void Grid_template_columns_setter_accepts_grid_template() {
        var css = StyleSheet.Render<GridStyles>();
        Assert.Contains(".layout {", css);
        Assert.Contains("grid-template-columns: repeat(3, 1fr);", css);
    }
}
