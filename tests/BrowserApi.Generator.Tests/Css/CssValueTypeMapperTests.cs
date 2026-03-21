using BrowserApi.Generator.Css;

namespace BrowserApi.Generator.Tests.Css;

public class CssValueTypeMapperTests {
    [Theory]
    [InlineData("row | row-reverse | column | column-reverse", "enum")]
    [InlineData("nowrap | wrap | wrap-reverse", "enum")]
    [InlineData("visible | hidden | collapse", "enum")]
    public void Pure_keywords_map_to_enum(string grammar, string expected) {
        Assert.Equal(expected, CssValueTypeMapper.MapToCSharpType(grammar));
    }

    [Theory]
    [InlineData("<color>", "CssColor")]
    [InlineData("<length>", "Length")]
    [InlineData("<time>", "Duration")]
    [InlineData("<angle>", "Angle")]
    [InlineData("<percentage>", "Percentage")]
    public void Known_css_types_map_correctly(string grammar, string expected) {
        Assert.Equal(expected, CssValueTypeMapper.MapToCSharpType(grammar));
    }

    [Theory]
    [InlineData("<number>", "double")]
    [InlineData("<integer>", "int")]
    [InlineData("<resolution>", "Resolution")]
    [InlineData("<flex>", "Flex")]
    public void Numeric_types_map_correctly(string grammar, string expected) {
        Assert.Equal(expected, CssValueTypeMapper.MapToCSharpType(grammar));
    }

    [Theory]
    [InlineData("[ <display-outside> || <display-inside> ] | <display-listitem>", "string")]
    [InlineData("", "string")]
    public void Complex_grammar_maps_to_string(string grammar, string expected) {
        Assert.Equal(expected, CssValueTypeMapper.MapToCSharpType(grammar));
    }

    [Theory]
    [InlineData("transform", "Transform")]
    [InlineData("box-shadow", "Shadow")]
    [InlineData("text-shadow", "Shadow")]
    [InlineData("transition", "Transition")]
    [InlineData("transition-duration", "Duration")]
    [InlineData("transition-timing-function", "Easing")]
    [InlineData("animation-duration", "Duration")]
    [InlineData("animation-timing-function", "Easing")]
    [InlineData("perspective", "Length")]
    public void Property_name_overrides_take_precedence(string propertyName, string expected) {
        Assert.Equal(expected, CssValueTypeMapper.MapToCSharpType("complex | grammar", propertyName));
    }

    [Theory]
    [InlineData("row | row-reverse | column | column-reverse", true)]
    [InlineData("visible | hidden", true)]
    [InlineData("<color>", false)]
    [InlineData("none | <length>", false)]
    [InlineData("", false)]
    public void IsPureKeywordGrammar_detects_correctly(string grammar, bool expected) {
        Assert.Equal(expected, CssValueTypeMapper.IsPureKeywordGrammar(grammar));
    }
}
