using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests.VirtualDom;

public class VirtualElementTests {
    [Fact]
    public void Id_get_set() {
        var el = new VirtualElement("div");
        el.Id = "main";
        Assert.Equal("main", el.Id);
    }

    [Fact]
    public void ClassName_get_set() {
        var el = new VirtualElement("div");
        el.ClassName = "card active";
        Assert.Equal("card active", el.ClassName);
    }

    [Fact]
    public void TextContent_get_set() {
        var el = new VirtualElement("p");
        el.TextContent = "hello world";
        Assert.Equal("hello world", el.TextContent);
    }

    [Fact]
    public void SetAttribute_GetAttribute() {
        var el = new VirtualElement("input");
        el.SetAttribute("type", "text");
        Assert.Equal("text", el.GetAttribute("type"));
    }

    [Fact]
    public void RemoveAttribute() {
        var el = new VirtualElement("input");
        el.SetAttribute("type", "text");
        el.RemoveAttribute("type");
        Assert.Null(el.GetAttribute("type"));
    }

    [Fact]
    public void HasAttribute() {
        var el = new VirtualElement("input");
        Assert.False(el.HasAttribute("type"));
        el.SetAttribute("type", "text");
        Assert.True(el.HasAttribute("type"));
    }

    [Fact]
    public void Style_property_access() {
        var el = new VirtualElement("div");
        el.Style["display"] = "flex";
        el.Style["gap"] = "1rem";

        Assert.Equal("flex", el.Style["display"]);
        Assert.Equal("1rem", el.Style["gap"]);
    }

    [Fact]
    public void Style_via_js_property() {
        var el = new VirtualElement("div");
        el.Style.SetJsProperty("backgroundColor", "red");

        Assert.Equal("red", el.Style["background-color"]);
    }

    [Fact]
    public void Children_returns_element_children_only() {
        var el = new VirtualElement("div");
        el.AppendChild(new VirtualElement("span"));
        el.AppendChild(new VirtualTextNode("text"));
        el.AppendChild(new VirtualElement("p"));

        Assert.Equal(3, el.ChildNodes.Count);
        Assert.Equal(2, el.Children.Count);
    }

    [Fact]
    public void InnerHtml_serializes_children() {
        var div = new VirtualElement("div");
        div.Id = "main";
        var p = new VirtualElement("p");
        p.AppendChild(new VirtualTextNode("Hello"));
        div.AppendChild(p);

        Assert.Equal("<p>Hello</p>", div.InnerHtml);
    }

    [Fact]
    public void OuterHtml_includes_self() {
        var div = new VirtualElement("div");
        div.Id = "main";
        div.ClassName = "card";
        div.AppendChild(new VirtualTextNode("Hi"));

        Assert.Equal("<div id=\"main\" class=\"card\">Hi</div>", div.OuterHtml);
    }

    [Fact]
    public void AppendChild_removes_from_previous_parent() {
        var parent1 = new VirtualElement("div");
        var parent2 = new VirtualElement("div");
        var child = new VirtualElement("span");

        parent1.AppendChild(child);
        Assert.Single(parent1.ChildNodes);

        parent2.AppendChild(child);
        Assert.Empty(parent1.ChildNodes);
        Assert.Single(parent2.ChildNodes);
        Assert.Same(parent2, child.ParentNode);
    }

    [Fact]
    public void RemoveChild_clears_parent() {
        var parent = new VirtualElement("div");
        var child = new VirtualElement("span");
        parent.AppendChild(child);

        parent.RemoveChild(child);

        Assert.Null(child.ParentNode);
        Assert.Empty(parent.ChildNodes);
    }
}
