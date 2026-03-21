using System.Text.Json;
using BrowserApi.Generator.Css;

namespace BrowserApi.Generator.Tests.Css;

public class CssDataReaderTests {
    private readonly CssDataReader _reader = new();

    private List<CssPropertyDefinition> Parse(string json) {
        using var doc = JsonDocument.Parse(json);
        return _reader.ParseProperties(doc.RootElement);
    }

    [Fact]
    public void Parses_flex_direction() {
        var props = Parse("""
        {
            "properties": [
                {
                    "name": "flex-direction",
                    "value": "row | row-reverse | column | column-reverse",
                    "initial": "row",
                    "inherited": "no",
                    "animationType": "discrete",
                    "values": [
                        { "name": "row", "type": "value", "value": "row" },
                        { "name": "row-reverse", "type": "value", "value": "row-reverse" }
                    ],
                    "styleDeclaration": ["flex-direction", "flexDirection"]
                }
            ]
        }
        """);

        var prop = Assert.Single(props);
        Assert.Equal("flex-direction", prop.Name);
        Assert.Equal("row | row-reverse | column | column-reverse", prop.ValueGrammar);
        Assert.Equal("row", prop.Initial);
        Assert.False(prop.IsInherited);
        Assert.Equal(2, prop.Values.Count);
        Assert.Equal(2, prop.StyleDeclarationNames.Count);
        Assert.Contains("flexDirection", prop.StyleDeclarationNames);
    }

    [Fact]
    public void Parses_color() {
        var props = Parse("""
        {
            "properties": [
                {
                    "name": "color",
                    "value": "<color>",
                    "initial": "CanvasText",
                    "inherited": "yes",
                    "styleDeclaration": ["color"]
                }
            ]
        }
        """);

        var prop = Assert.Single(props);
        Assert.Equal("color", prop.Name);
        Assert.Equal("<color>", prop.ValueGrammar);
        Assert.True(prop.IsInherited);
    }

    [Fact]
    public void Parses_display_complex_grammar() {
        var props = Parse("""
        {
            "properties": [
                {
                    "name": "display",
                    "value": "[ <display-outside> || <display-inside> ] | <display-listitem>",
                    "initial": "inline",
                    "inherited": "no",
                    "styleDeclaration": ["display"]
                }
            ]
        }
        """);

        var prop = Assert.Single(props);
        Assert.Equal("display", prop.Name);
        Assert.Contains("<display-outside>", prop.ValueGrammar);
    }

    [Fact]
    public void Reads_real_css_flexbox_file() {
        var reader = new CssDataReader();
        var props = reader.ReadFile(SpecPath("specs/css/css-flexbox.json"));
        Assert.True(props.Count > 0);
        Assert.Contains(props, p => p.Name == "flex-direction");
        Assert.Contains(props, p => p.Name == "flex-wrap");
    }

    private static string SpecPath(string relativePath) {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "BrowserApi.sln")))
            dir = Path.GetDirectoryName(dir);
        return Path.Combine(dir!, relativePath);
    }
}
