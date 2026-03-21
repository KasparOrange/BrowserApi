using BrowserApi.Css;

namespace BrowserApi.Tests.Css;

public class CssUnitExtensionTests {
    [Fact]
    public void Int_Px() => Assert.Equal("16px", 16.Px().ToCss());

    [Fact]
    public void Double_Px() => Assert.Equal("1.5px", 1.5.Px().ToCss());

    [Fact]
    public void Double_Em() => Assert.Equal("1.5em", 1.5.Em().ToCss());

    [Fact]
    public void Double_Rem() => Assert.Equal("1.5rem", 1.5.Rem().ToCss());

    [Fact]
    public void Double_Vh() => Assert.Equal("100vh", 100.0.Vh().ToCss());

    [Fact]
    public void Double_Vw() => Assert.Equal("50vw", 50.0.Vw().ToCss());

    [Fact]
    public void Int_Ms() => Assert.Equal("300ms", 300.Ms().ToCss());

    [Fact]
    public void Double_S() => Assert.Equal("0.3s", 0.3.S().ToCss());

    [Fact]
    public void Int_Deg() => Assert.Equal("45deg", 45.Deg().ToCss());

    [Fact]
    public void Int_Percent() => Assert.Equal("50%", 50.Percent().ToCss());

    [Fact]
    public void Int_Fr() => Assert.Equal("1fr", 1.Fr().ToCss());

    [Fact]
    public void Double_Fr() => Assert.Equal("1.5fr", 1.5.Fr().ToCss());
}
