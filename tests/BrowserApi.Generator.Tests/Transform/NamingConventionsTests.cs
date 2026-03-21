using BrowserApi.Generator.Transform;

namespace BrowserApi.Generator.Tests.Transform;

public class NamingConventionsTests {
    [Theory]
    [InlineData("querySelector", "QuerySelector")]
    [InlineData("innerHTML", "InnerHtml")]
    [InlineData("bodyUsed", "BodyUsed")]
    [InlineData("className", "ClassName")]
    [InlineData("textContent", "TextContent")]
    public void ToPascalCase_camelCase(string input, string expected) {
        Assert.Equal(expected, NamingConventions.ToPascalCase(input));
    }

    [Theory]
    [InlineData("row-reverse", "RowReverse")]
    [InlineData("flex-start", "FlexStart")]
    [InlineData("no-repeat", "NoRepeat")]
    public void ToPascalCase_kebab_case(string input, string expected) {
        Assert.Equal(expected, NamingConventions.ToPascalCase(input));
    }

    [Theory]
    [InlineData("HTMLElement", "HtmlElement")]
    [InlineData("XMLHttpRequest", "XmlHttpRequest")]
    [InlineData("CSSStyleDeclaration", "CssStyleDeclaration")]
    [InlineData("DOMTokenList", "DomTokenList")]
    [InlineData("SVGElement", "SvgElement")]
    [InlineData("URLSearchParams", "UrlSearchParams")]
    public void ToPascalCase_acronyms(string input, string expected) {
        Assert.Equal(expected, NamingConventions.ToPascalCase(input));
    }

    [Theory]
    [InlineData("Node", "Node")]
    [InlineData("Element", "Element")]
    [InlineData("Request", "Request")]
    public void ToPascalCase_already_pascal(string input, string expected) {
        Assert.Equal(expected, NamingConventions.ToPascalCase(input));
    }

    [Fact]
    public void ToPascalCase_empty_string() {
        Assert.Equal("", NamingConventions.ToPascalCase(""));
    }

    [Theory]
    [InlineData("row", "Row")]
    [InlineData("row-reverse", "RowReverse")]
    [InlineData("", "Empty")]
    [InlineData("flex-start", "FlexStart")]
    public void ToEnumMemberName_basic(string input, string expected) {
        Assert.Equal(expected, NamingConventions.ToEnumMemberName(input));
    }

    [Fact]
    public void ToEnumMemberName_csharp_keyword() {
        Assert.Equal("@Default", NamingConventions.ToEnumMemberName("default"));
    }

    [Theory]
    [InlineData("tabularData", "tabularData")]
    [InlineData("innerHTML", "innerHtml")]
    [InlineData("className", "className")]
    public void ToParameterName_basic(string input, string expected) {
        Assert.Equal(expected, NamingConventions.ToParameterName(input));
    }

    [Fact]
    public void ToParameterName_keyword() {
        Assert.Equal("@object", NamingConventions.ToParameterName("object"));
    }
}
