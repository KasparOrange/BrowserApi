using BrowserApi.Css.Authoring;

namespace BrowserApi.Tests.Css.Authoring;

/// <summary>Tests for typed attribute selectors (spec §15).</summary>
public class AttrSelectorTests {
    [Fact]
    public void Bare_attribute_emits_presence_selector() {
        Selector sel = Attr.Disabled;
        Assert.Equal("[disabled]", sel.Css);
    }

    [Fact]
    public void Equals_emits_exact_match() {
        Assert.Equal("[type=\"text\"]", Attr.Type.Equals("text").Css);
    }

    [Fact]
    public void Starts_with_uses_caret_equals() {
        Assert.Equal("[href^=\"https\"]", Attr.Href.StartsWith("https").Css);
    }

    [Fact]
    public void Ends_with_uses_dollar_equals() {
        Assert.Equal("[href$=\".pdf\"]", Attr.Href.EndsWith(".pdf").Css);
    }

    [Fact]
    public void Contains_uses_star_equals() {
        Assert.Equal("[title*=\"warning\"]", Attr.Title.Contains("warning").Css);
    }

    [Fact]
    public void Has_word_uses_tilde_equals() {
        Assert.Equal("[class~=\"primary\"]", Attr.Of("class").HasWord("primary").Css);
    }

    [Fact]
    public void Aria_attributes_resolve_to_aria_dash_name() {
        Assert.Equal("[aria-hidden=\"true\"]", Attr.Aria.Hidden.Equals("true").Css);
        Assert.Equal("[aria-label=\"Close\"]", Attr.Aria.Label.Equals("Close").Css);
    }

    [Fact]
    public void Data_factory_prepends_data_dash() {
        Assert.Equal("[data-stick-value=\"0\"]", Attr.Data("stick-value").Equals("0").Css);
    }

    [Fact]
    public void Escape_hatch_lets_arbitrary_attribute_names_through() {
        Assert.Equal("[potato=\"yes\"]", Attr.Of("potato").Equals("yes").Css);
    }

    [Fact]
    public void Attr_selector_composes_with_class_via_compound() {
        // E.g. `.btn[disabled]` — combine class with bare attribute selector.
        // The AttrSelector must be lifted to Selector explicitly (one-hop conversion
        // chain isn't compounded by C#).
        var btn = new Class { Name = "btn" };
        Selector attr = Attr.Disabled;
        Selector sel = btn * attr;
        Assert.Equal(".btn[disabled]", sel.Css);
    }
}
