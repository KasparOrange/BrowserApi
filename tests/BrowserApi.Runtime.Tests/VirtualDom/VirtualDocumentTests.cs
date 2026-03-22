using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests.VirtualDom;

public class VirtualDocumentTests {
    [Fact]
    public void Document_has_html_head_body_structure() {
        var doc = new VirtualDocument();

        Assert.NotNull(doc.DocumentElement);
        Assert.Equal("html", doc.DocumentElement.TagName);
        Assert.NotNull(doc.Head);
        Assert.Equal("head", doc.Head.TagName);
        Assert.NotNull(doc.Body);
        Assert.Equal("body", doc.Body.TagName);
    }

    [Fact]
    public void CreateElement_returns_element_with_tag() {
        var doc = new VirtualDocument();
        var div = doc.CreateElement("div");

        Assert.Equal("div", div.TagName);
        Assert.Equal("DIV", div.NodeName);
        Assert.Equal(1, div.NodeType);
    }

    [Fact]
    public void CreateTextNode_returns_text_node() {
        var doc = new VirtualDocument();
        var text = doc.CreateTextNode("hello");

        Assert.Equal("hello", text.Data);
        Assert.Equal(3, text.NodeType);
    }

    [Fact]
    public void AppendChild_to_body() {
        var doc = new VirtualDocument();
        var div = doc.CreateElement("div");
        doc.Body.AppendChild(div);

        Assert.Contains(div, doc.Body.ChildNodes);
        Assert.Same(doc.Body, div.ParentNode);
    }

    [Fact]
    public void GetElementById_finds_element() {
        var doc = new VirtualDocument();
        var div = doc.CreateElement("div");
        div.Id = "main";
        doc.Body.AppendChild(div);

        var found = doc.GetElementById("main");

        Assert.NotNull(found);
        Assert.Same(div, found);
    }

    [Fact]
    public void QuerySelector_finds_by_tag() {
        var doc = new VirtualDocument();
        var p = doc.CreateElement("p");
        doc.Body.AppendChild(p);

        var found = doc.QuerySelector("p");

        Assert.NotNull(found);
        Assert.Same(p, found);
    }

    [Fact]
    public void QuerySelector_finds_by_class() {
        var doc = new VirtualDocument();
        var div = doc.CreateElement("div");
        div.ClassName = "card active";
        doc.Body.AppendChild(div);

        var found = doc.QuerySelector(".card");

        Assert.NotNull(found);
        Assert.Same(div, found);
    }

    [Fact]
    public void QuerySelector_finds_by_id() {
        var doc = new VirtualDocument();
        var div = doc.CreateElement("div");
        div.Id = "hero";
        doc.Body.AppendChild(div);

        var found = doc.QuerySelector("#hero");

        Assert.NotNull(found);
        Assert.Same(div, found);
    }

    [Fact]
    public void QuerySelectorAll_returns_multiple() {
        var doc = new VirtualDocument();
        doc.Body.AppendChild(doc.CreateElement("li"));
        doc.Body.AppendChild(doc.CreateElement("li"));
        doc.Body.AppendChild(doc.CreateElement("li"));

        var items = doc.QuerySelectorAll("li");

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void QuerySelector_compound_selector() {
        var doc = new VirtualDocument();
        var div = doc.CreateElement("div");
        div.ClassName = "active";
        doc.Body.AppendChild(div);

        var found = doc.QuerySelector("div.active");

        Assert.NotNull(found);
        Assert.Same(div, found);
    }
}
