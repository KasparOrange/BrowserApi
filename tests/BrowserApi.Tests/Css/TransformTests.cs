using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class TransformTests {
    [Fact]
    public void None_outputs_none() {
        Assert.Equal("none", Transform.None.ToCss());
    }

    [Fact]
    public void Rotate_formats_correctly() {
        Assert.Equal("rotate(45deg)", Transform.Rotate(Angle.Deg(45)).ToCss());
    }

    [Fact]
    public void Scale_single_formats_correctly() {
        Assert.Equal("scale(1.5)", Transform.Scale(1.5).ToCss());
    }

    [Fact]
    public void Scale_xy_formats_correctly() {
        Assert.Equal("scale(1, 2)", Transform.Scale(1, 2).ToCss());
    }

    [Fact]
    public void ScaleX_formats_correctly() {
        Assert.Equal("scaleX(2)", Transform.ScaleX(2).ToCss());
    }

    [Fact]
    public void ScaleY_formats_correctly() {
        Assert.Equal("scaleY(0.5)", Transform.ScaleY(0.5).ToCss());
    }

    [Fact]
    public void Translate_formats_correctly() {
        Assert.Equal("translate(10px, 20px)", Transform.Translate(Length.Px(10), Length.Px(20)).ToCss());
    }

    [Fact]
    public void TranslateX_formats_correctly() {
        Assert.Equal("translateX(10px)", Transform.TranslateX(Length.Px(10)).ToCss());
    }

    [Fact]
    public void TranslateY_formats_correctly() {
        Assert.Equal("translateY(20px)", Transform.TranslateY(Length.Px(20)).ToCss());
    }

    [Fact]
    public void SkewX_formats_correctly() {
        Assert.Equal("skewX(10deg)", Transform.SkewX(Angle.Deg(10)).ToCss());
    }

    [Fact]
    public void SkewY_formats_correctly() {
        Assert.Equal("skewY(20deg)", Transform.SkewY(Angle.Deg(20)).ToCss());
    }

    [Fact]
    public void Skew_formats_correctly() {
        Assert.Equal("skew(10deg, 20deg)", Transform.Skew(Angle.Deg(10), Angle.Deg(20)).ToCss());
    }

    [Fact]
    public void Matrix_formats_correctly() {
        Assert.Equal("matrix(1, 0, 0, 1, 0, 0)", Transform.Matrix(1, 0, 0, 1, 0, 0).ToCss());
    }

    [Fact]
    public void Then_chains_transforms() {
        var result = Transform.Rotate(Angle.Deg(45)).Then(Transform.Scale(1.5));
        Assert.Equal("rotate(45deg) scale(1.5)", result.ToCss());
    }

    [Fact]
    public void ThenRotate_convenience_chains() {
        var result = Transform.Scale(1.5).ThenRotate(Angle.Deg(45));
        Assert.Equal("scale(1.5) rotate(45deg)", result.ToCss());
    }

    [Fact]
    public void ThenScale_convenience_chains() {
        var result = Transform.Rotate(Angle.Deg(45)).ThenScale(1.5);
        Assert.Equal("rotate(45deg) scale(1.5)", result.ToCss());
    }

    [Fact]
    public void Three_function_chain() {
        var result = Transform.Translate(Length.Px(10), Length.Px(0))
            .ThenRotate(Angle.Deg(45))
            .ThenScale(2);
        Assert.Equal("translate(10px, 0px) rotate(45deg) scale(2)", result.ToCss());
    }

    [Fact]
    public void ToString_delegates_to_ToCss() {
        Assert.Equal("rotate(45deg)", Transform.Rotate(Angle.Deg(45)).ToString());
    }

    [Fact]
    public void Equal_values_are_equal() {
        Assert.Equal(Transform.Rotate(Angle.Deg(45)), Transform.Rotate(Angle.Deg(45)));
        Assert.True(Transform.None == Transform.None);
    }

    [Fact]
    public void Different_values_are_not_equal() {
        Assert.True(Transform.Rotate(Angle.Deg(45)) != Transform.Scale(1.5));
    }

    [Fact]
    public void Default_struct_ToCss_returns_null() {
        Assert.Null(default(Transform).ToCss());
    }
}
