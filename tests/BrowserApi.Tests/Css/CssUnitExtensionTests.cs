using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class CssUnitExtensionTests {
    [Fact]
    public void Int_Px() => Assert.Equal("16px", 16.Px.ToCss());

    [Fact]
    public void Double_Px() => Assert.Equal("1.5px", 1.5.Px.ToCss());

    [Fact]
    public void Double_Em() => Assert.Equal("1.5em", 1.5.Em.ToCss());

    [Fact]
    public void Double_Rem() => Assert.Equal("1.5rem", 1.5.Rem.ToCss());

    [Fact]
    public void Double_Vh() => Assert.Equal("100vh", 100.0.Vh.ToCss());

    [Fact]
    public void Double_Vw() => Assert.Equal("50vw", 50.0.Vw.ToCss());

    [Fact]
    public void Int_Ms() => Assert.Equal("300ms", 300.Ms.ToCss());

    [Fact]
    public void Double_S() => Assert.Equal("0.3s", 0.3.S.ToCss());

    [Fact]
    public void Int_Deg() => Assert.Equal("45deg", 45.Deg.ToCss());

    [Fact]
    public void Int_Percent() => Assert.Equal("50%", 50.Percent.ToCss());

    [Fact]
    public void Int_Fr() => Assert.Equal("1fr", 1.Fr.ToCss());

    [Fact]
    public void Double_Fr() => Assert.Equal("1.5fr", 1.5.Fr.ToCss());

    // ── Additional int/double coverage ─────────────────────────────────

    [Fact]
    public void Int_Px_zero() => Assert.Equal("0px", 0.Px.ToCss());

    [Fact]
    public void Double_Px_zero() => Assert.Equal("0px", 0.0.Px.ToCss());

    [Fact]
    public void Double_Em_whole_number() => Assert.Equal("2em", 2.0.Em.ToCss());

    [Fact]
    public void Double_Rem_whole_number() => Assert.Equal("1rem", 1.0.Rem.ToCss());

    [Fact]
    public void Double_Percent() => Assert.Equal("33.3%", 33.3.Percent.ToCss());

    [Fact]
    public void Double_Deg() => Assert.Equal("45.5deg", 45.5.Deg.ToCss());

    [Fact]
    public void Double_Ms() => Assert.Equal("250.5ms", 250.5.Ms.ToCss());

    [Fact]
    public void Double_S_whole_number() => Assert.Equal("1s", 1.0.S.ToCss());

    [Fact]
    public void Int_Fr_zero() => Assert.Equal("0fr", 0.Fr.ToCss());

    [Fact]
    public void Int_Px_negative() => Assert.Equal("-10px", (-10).Px.ToCss());

    [Fact]
    public void Int_Deg_negative() => Assert.Equal("-90deg", (-90).Deg.ToCss());

    [Fact]
    public void Int_Percent_hundred() => Assert.Equal("100%", 100.Percent.ToCss());

    [Fact]
    public void Int_Ms_large() => Assert.Equal("1000ms", 1000.Ms.ToCss());
}
