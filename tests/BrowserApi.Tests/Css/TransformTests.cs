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

    // ── Chaining convenience methods ───────────────────────────────────

    [Fact]
    public void ThenTranslate_appends_translate() {
        var result = Transform.Rotate(Angle.Deg(45))
            .ThenTranslate(Length.Px(10), Length.Px(20));
        Assert.Equal("rotate(45deg) translate(10px, 20px)", result.ToCss());
    }

    [Fact]
    public void ThenScale_xy_appends_scale_with_two_args() {
        var result = Transform.Rotate(Angle.Deg(45))
            .ThenScale(1.5, 2);
        Assert.Equal("rotate(45deg) scale(1.5, 2)", result.ToCss());
    }

    [Fact]
    public void ThenSkewX_appends_skewX() {
        var result = Transform.Scale(2)
            .ThenSkewX(Angle.Deg(15));
        Assert.Equal("scale(2) skewX(15deg)", result.ToCss());
    }

    [Fact]
    public void ThenSkewY_appends_skewY() {
        var result = Transform.Scale(2)
            .ThenSkewY(Angle.Deg(30));
        Assert.Equal("scale(2) skewY(30deg)", result.ToCss());
    }

    [Fact]
    public void Full_chain_translate_rotate_scale() {
        var result = Transform.Translate(Length.Px(50), Length.Px(100))
            .ThenRotate(Angle.Deg(90))
            .ThenScale(0.5);
        Assert.Equal("translate(50px, 100px) rotate(90deg) scale(0.5)", result.ToCss());
    }

    // ── Equality detailed ──────────────────────────────────────────────

    [Fact]
    public void Equals_object_returns_true_for_same_value() {
        var a = Transform.Rotate(Angle.Deg(45));
        object b = Transform.Rotate(Angle.Deg(45));
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_object_returns_false_for_different_type() {
        var a = Transform.Rotate(Angle.Deg(45));
        Assert.False(a.Equals("rotate(45deg)"));
    }

    [Fact]
    public void GetHashCode_equal_for_same_values() {
        var a = Transform.Rotate(Angle.Deg(45));
        var b = Transform.Rotate(Angle.Deg(45));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Operators_eq_and_neq() {
        var a = Transform.Scale(2);
        var b = Transform.Scale(2);
        var c = Transform.Scale(3);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a != c);
        Assert.False(a == c);
    }

    [Fact]
    public void Matrix_with_decimal_values() {
        Assert.Equal("matrix(1, 0.5, -0.5, 1, 10, 20)",
            Transform.Matrix(1, 0.5, -0.5, 1, 10, 20).ToCss());
    }
}
