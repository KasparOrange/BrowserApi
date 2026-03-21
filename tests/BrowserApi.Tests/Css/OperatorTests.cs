using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class OperatorTests {
    [Fact]
    public void Addition_creates_calc_expression() {
        var result = Length.Px(16) + Length.Rem(1);
        Assert.Equal("calc(16px + 1rem)", result.ToCss());
    }

    [Fact]
    public void Subtraction_creates_calc_expression() {
        var result = Length.Percent(100) - Length.Px(20);
        Assert.Equal("calc(100% - 20px)", result.ToCss());
    }

    [Fact]
    public void Negation_creates_calc_expression() {
        var result = -Length.Px(10);
        Assert.Equal("calc(-1 * 10px)", result.ToCss());
    }

    [Fact]
    public void Implicit_int_to_length_px() {
        Length len = 16;
        Assert.Equal("16px", len.ToCss());
    }

    [Fact]
    public void Implicit_double_to_length_px() {
        Length len = 1.5;
        Assert.Equal("1.5px", len.ToCss());
    }

    [Fact]
    public void Mixed_arithmetic() {
        var result = Length.Vh(100) - Length.Rem(4);
        Assert.Equal("calc(100vh - 4rem)", result.ToCss());
    }
}
