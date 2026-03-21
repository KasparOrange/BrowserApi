using BrowserApi.Generator.Css;

namespace BrowserApi.Generator.Tests.Css;

public class CssEnumEmitterTests {
    [Fact]
    public void Emits_flex_direction_enum() {
        var prop = new CssPropertyDefinition {
            Name = "flex-direction",
            ValueGrammar = "row | row-reverse | column | column-reverse"
        };

        var output = CssEnumEmitter.TryEmit(prop);
        Assert.NotNull(output);
        Assert.Contains("public enum FlexDirection", output);
        Assert.Contains("[StringValue(\"row\")]", output);
        Assert.Contains("Row,", output);
        Assert.Contains("[StringValue(\"row-reverse\")]", output);
        Assert.Contains("RowReverse,", output);
        Assert.Contains("[StringValue(\"column-reverse\")]", output);
    }

    [Fact]
    public void Returns_null_for_complex_grammar() {
        var prop = new CssPropertyDefinition {
            Name = "display",
            ValueGrammar = "[ <display-outside> || <display-inside> ] | <display-listitem>"
        };

        Assert.Null(CssEnumEmitter.TryEmit(prop));
    }

    [Fact]
    public void Creates_enum_model() {
        var prop = new CssPropertyDefinition {
            Name = "visibility",
            ValueGrammar = "visible | hidden | collapse"
        };

        var csEnum = CssEnumEmitter.TryCreate(prop);
        Assert.NotNull(csEnum);
        Assert.Equal("Visibility", csEnum.Name);
        Assert.Equal("BrowserApi.Css", csEnum.Namespace);
        Assert.Equal(3, csEnum.Members.Count);
        Assert.Equal("Visible", csEnum.Members[0].Name);
        Assert.Equal("visible", csEnum.Members[0].StringValue);
    }
}
