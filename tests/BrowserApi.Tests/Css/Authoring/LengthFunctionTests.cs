using BrowserApi.Css;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for the CSS function-style Length constructors (clamp, min, max).</summary>
public class LengthFunctionTests {
    [Fact]
    public void Clamp_emits_three_arg_expression() {
        var clamp = Length.Clamp(Length.Rem(1), Length.Vw(5), Length.Rem(3));
        Assert.Equal("clamp(1rem, 5vw, 3rem)", clamp.ToCss());
    }

    [Fact]
    public void Min_emits_two_arg_expression() {
        var min = Length.Min(Length.Px(200), Length.Percent(50));
        Assert.Equal("min(200px, 50%)", min.ToCss());
    }

    [Fact]
    public void Min_supports_n_args() {
        var min = Length.Min(Length.Px(100), Length.Px(200), Length.Px(50));
        Assert.Equal("min(100px, 200px, 50px)", min.ToCss());
    }

    [Fact]
    public void Max_two_arg_works_like_min() {
        var max = Length.Max(Length.Px(200), Length.Em(10));
        Assert.Equal("max(200px, 10em)", max.ToCss());
    }

    [Fact]
    public void Fit_content_keyword_emits_bare() {
        Assert.Equal("fit-content", Length.FitContent.ToCss());
    }

    [Fact]
    public void Fit_content_with_limit_emits_function_form() {
        Assert.Equal("fit-content(400px)", Length.FitContentLimit(Length.Px(400)).ToCss());
    }

    [Fact]
    public void Min_max_content_keywords_resolve() {
        Assert.Equal("min-content", Length.MinContent.ToCss());
        Assert.Equal("max-content", Length.MaxContent.ToCss());
    }
}
