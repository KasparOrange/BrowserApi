using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests.VirtualDom;

public class VirtualStyleTests {
    [Fact]
    public void Indexer_get_returns_empty_string_for_unset_property() {
        var style = new VirtualStyle();
        Assert.Equal("", style["color"]);
    }

    [Fact]
    public void Indexer_set_and_get_roundtrip() {
        var style = new VirtualStyle();
        style["color"] = "red";
        Assert.Equal("red", style["color"]);
    }

    [Fact]
    public void Indexer_set_null_removes_property() {
        var style = new VirtualStyle();
        style["color"] = "red";
        Assert.Equal(1, style.Count);

        style["color"] = null!;
        Assert.Equal(0, style.Count);
        Assert.Equal("", style["color"]);
    }

    [Fact]
    public void Indexer_set_empty_string_removes_property() {
        var style = new VirtualStyle();
        style["color"] = "red";
        style["color"] = "";
        Assert.Equal(0, style.Count);
        Assert.Equal("", style["color"]);
    }

    [Fact]
    public void Indexer_overwrite_existing_property() {
        var style = new VirtualStyle();
        style["color"] = "red";
        style["color"] = "blue";
        Assert.Equal("blue", style["color"]);
        Assert.Equal(1, style.Count);
    }

    [Fact]
    public void Count_reflects_number_of_properties() {
        var style = new VirtualStyle();
        Assert.Equal(0, style.Count);

        style["color"] = "red";
        Assert.Equal(1, style.Count);

        style["font-size"] = "16px";
        Assert.Equal(2, style.Count);

        style["color"] = "";
        Assert.Equal(1, style.Count);
    }

    [Fact]
    public void CssText_get_serializes_properties() {
        var style = new VirtualStyle();
        style["color"] = "red";
        style["font-size"] = "16px";

        var cssText = style.CssText;
        Assert.Contains("color: red", cssText);
        Assert.Contains("font-size: 16px", cssText);
    }

    [Fact]
    public void CssText_get_empty_when_no_properties() {
        var style = new VirtualStyle();
        Assert.Equal("", style.CssText);
    }

    [Fact]
    public void CssText_set_parses_semicolons() {
        var style = new VirtualStyle();
        style.CssText = "color: red; font-size: 16px; display: flex";

        Assert.Equal("red", style["color"]);
        Assert.Equal("16px", style["font-size"]);
        Assert.Equal("flex", style["display"]);
        Assert.Equal(3, style.Count);
    }

    [Fact]
    public void CssText_set_clears_existing_properties() {
        var style = new VirtualStyle();
        style["background"] = "blue";
        style["margin"] = "10px";

        style.CssText = "color: red";
        Assert.Equal("red", style["color"]);
        Assert.Equal("", style["background"]);
        Assert.Equal("", style["margin"]);
        Assert.Equal(1, style.Count);
    }

    [Fact]
    public void CssText_set_empty_clears_all() {
        var style = new VirtualStyle();
        style["color"] = "red";
        style["font-size"] = "16px";

        style.CssText = "";
        Assert.Equal(0, style.Count);
    }

    [Fact]
    public void CssText_set_null_clears_all() {
        var style = new VirtualStyle();
        style["color"] = "red";

        style.CssText = null!;
        Assert.Equal(0, style.Count);
    }

    [Fact]
    public void CssText_set_handles_trailing_semicolons() {
        var style = new VirtualStyle();
        style.CssText = "color: red; font-size: 16px;";

        Assert.Equal("red", style["color"]);
        Assert.Equal("16px", style["font-size"]);
        Assert.Equal(2, style.Count);
    }

    [Fact]
    public void CssText_set_handles_extra_whitespace() {
        var style = new VirtualStyle();
        style.CssText = "  color :  red  ;  font-size  :  16px  ";

        Assert.Equal("red", style["color"]);
        Assert.Equal("16px", style["font-size"]);
    }

    [Fact]
    public void GetJsProperty_cssText_returns_csstext() {
        var style = new VirtualStyle();
        style["color"] = "red";

        var result = style.GetJsProperty("cssText");
        Assert.Equal(style.CssText, result);
    }

    [Fact]
    public void GetJsProperty_length_returns_count() {
        var style = new VirtualStyle();
        style["color"] = "red";
        style["font-size"] = "14px";

        var result = style.GetJsProperty("length");
        Assert.Equal(2, result);
    }

    [Fact]
    public void GetJsProperty_camelCase_converts_to_kebab_case() {
        var style = new VirtualStyle();
        style["background-color"] = "blue";

        var result = style.GetJsProperty("backgroundColor");
        Assert.Equal("blue", result);
    }

    [Fact]
    public void GetJsProperty_fontSize_converts_to_font_size() {
        var style = new VirtualStyle();
        style["font-size"] = "14px";

        var result = style.GetJsProperty("fontSize");
        Assert.Equal("14px", result);
    }

    [Fact]
    public void GetJsProperty_unset_returns_empty_string() {
        var style = new VirtualStyle();
        var result = style.GetJsProperty("backgroundColor");
        Assert.Equal("", result);
    }

    [Fact]
    public void GetJsProperty_simple_name_no_conversion() {
        var style = new VirtualStyle();
        style["color"] = "green";

        var result = style.GetJsProperty("color");
        Assert.Equal("green", result);
    }

    [Fact]
    public void SetJsProperty_cssText_sets_csstext() {
        var style = new VirtualStyle();
        style.SetJsProperty("cssText", "color: red; margin: 5px");

        Assert.Equal("red", style["color"]);
        Assert.Equal("5px", style["margin"]);
    }

    [Fact]
    public void SetJsProperty_camelCase_sets_kebab_property() {
        var style = new VirtualStyle();
        style.SetJsProperty("backgroundColor", "purple");

        Assert.Equal("purple", style["background-color"]);
    }

    [Fact]
    public void SetJsProperty_null_removes_property() {
        var style = new VirtualStyle();
        style["background-color"] = "red";

        style.SetJsProperty("backgroundColor", null);
        Assert.Equal("", style["background-color"]);
        Assert.Equal(0, style.Count);
    }

    [Fact]
    public void SetJsProperty_empty_string_removes_property() {
        var style = new VirtualStyle();
        style["background-color"] = "red";

        style.SetJsProperty("backgroundColor", "");
        Assert.Equal("", style["background-color"]);
        Assert.Equal(0, style.Count);
    }

    [Fact]
    public void SetJsProperty_cssText_null_clears_all() {
        var style = new VirtualStyle();
        style["color"] = "red";
        style.SetJsProperty("cssText", null);
        Assert.Equal(0, style.Count);
    }

    [Fact]
    public void InvokeJsMethod_returns_null() {
        var style = new VirtualStyle();
        var result = style.InvokeJsMethod("someMethod", []);
        Assert.Null(result);
    }

    [Fact]
    public void CamelToKebab_multiple_uppercase_letters() {
        // Test borderTopWidth -> border-top-width via SetJsProperty/GetJsProperty
        var style = new VirtualStyle();
        style.SetJsProperty("borderTopWidth", "2px");

        Assert.Equal("2px", style["border-top-width"]);
        Assert.Equal("2px", style.GetJsProperty("borderTopWidth"));
    }

    [Fact]
    public void CamelToKebab_single_word_unchanged() {
        var style = new VirtualStyle();
        style.SetJsProperty("color", "red");
        Assert.Equal("red", style["color"]);
    }

    [Fact]
    public void Indexer_case_insensitive() {
        var style = new VirtualStyle();
        style["Color"] = "red";
        Assert.Equal("red", style["color"]);
        Assert.Equal("red", style["COLOR"]);
    }
}
