using BrowserApi.Runtime.VirtualDom;

namespace BrowserApi.Runtime.Tests.VirtualDom;

public class VirtualTextNodeTests {
    [Fact]
    public void Constructor_sets_data() {
        var text = new VirtualTextNode("hello world");
        Assert.Equal("hello world", text.Data);
    }

    [Fact]
    public void Data_get_set() {
        var text = new VirtualTextNode("initial");
        text.Data = "changed";
        Assert.Equal("changed", text.Data);
    }

    [Fact]
    public void NodeType_is_3() {
        var text = new VirtualTextNode("test");
        Assert.Equal(3, text.NodeType);
    }

    [Fact]
    public void NodeName_is_hash_text() {
        var text = new VirtualTextNode("test");
        Assert.Equal("#text", text.NodeName);
    }

    [Fact]
    public void TextContent_get_returns_data() {
        var text = new VirtualTextNode("hello");
        Assert.Equal("hello", text.TextContent);
    }

    [Fact]
    public void TextContent_set_updates_data() {
        var text = new VirtualTextNode("old");
        text.TextContent = "new";
        Assert.Equal("new", text.Data);
    }

    [Fact]
    public void TextContent_and_data_are_in_sync() {
        var text = new VirtualTextNode("start");

        text.Data = "via data";
        Assert.Equal("via data", text.TextContent);

        text.TextContent = "via textContent";
        Assert.Equal("via textContent", text.Data);
    }

    [Fact]
    public void GetJsProperty_data_returns_data() {
        var text = new VirtualTextNode("hello");
        Assert.Equal("hello", text.GetJsProperty("data"));
    }

    [Fact]
    public void GetJsProperty_length_returns_data_length() {
        var text = new VirtualTextNode("hello");
        Assert.Equal(5, text.GetJsProperty("length"));
    }

    [Fact]
    public void GetJsProperty_length_empty_string() {
        var text = new VirtualTextNode("");
        Assert.Equal(0, text.GetJsProperty("length"));
    }

    [Fact]
    public void GetJsProperty_textContent_returns_data() {
        var text = new VirtualTextNode("content");
        Assert.Equal("content", text.GetJsProperty("textContent"));
    }

    [Fact]
    public void GetJsProperty_nodeType_returns_3() {
        var text = new VirtualTextNode("test");
        Assert.Equal(3, text.GetJsProperty("nodeType"));
    }

    [Fact]
    public void GetJsProperty_nodeName_returns_hash_text() {
        var text = new VirtualTextNode("test");
        Assert.Equal("#text", text.GetJsProperty("nodeName"));
    }

    [Fact]
    public void GetJsProperty_parentNode_returns_null_when_detached() {
        var text = new VirtualTextNode("test");
        Assert.Null(text.GetJsProperty("parentNode"));
    }

    [Fact]
    public void GetJsProperty_parentNode_returns_parent_when_attached() {
        var parent = new VirtualElement("div");
        var text = new VirtualTextNode("test");
        parent.AppendChild(text);

        Assert.Same(parent, text.GetJsProperty("parentNode"));
    }

    [Fact]
    public void GetJsProperty_unknown_returns_null() {
        var text = new VirtualTextNode("test");
        Assert.Null(text.GetJsProperty("nonexistent"));
    }

    [Fact]
    public void SetJsProperty_data_updates_data() {
        var text = new VirtualTextNode("old");
        text.SetJsProperty("data", "new");
        Assert.Equal("new", text.Data);
    }

    [Fact]
    public void SetJsProperty_data_null_sets_empty() {
        var text = new VirtualTextNode("something");
        text.SetJsProperty("data", null);
        Assert.Equal("", text.Data);
    }

    [Fact]
    public void SetJsProperty_textContent_updates_data() {
        var text = new VirtualTextNode("old");
        text.SetJsProperty("textContent", "updated");
        Assert.Equal("updated", text.Data);
    }

    [Fact]
    public void SetJsProperty_unknown_is_noop() {
        var text = new VirtualTextNode("test");
        text.SetJsProperty("unknown", "value");
        Assert.Equal("test", text.Data);
    }

    [Fact]
    public void ParentNode_is_null_when_detached() {
        var text = new VirtualTextNode("test");
        Assert.Null(text.ParentNode);
    }

    [Fact]
    public void ParentNode_is_set_when_appended() {
        var parent = new VirtualElement("p");
        var text = new VirtualTextNode("child text");
        parent.AppendChild(text);

        Assert.Same(parent, text.ParentNode);
    }

    [Fact]
    public void NextSibling_and_PreviousSibling() {
        var parent = new VirtualElement("div");
        var text1 = new VirtualTextNode("first");
        var text2 = new VirtualTextNode("second");
        parent.AppendChild(text1);
        parent.AppendChild(text2);

        Assert.Same(text2, text1.NextSibling);
        Assert.Null(text2.NextSibling);
        Assert.Same(text1, text2.PreviousSibling);
        Assert.Null(text1.PreviousSibling);
    }

    [Fact]
    public void ChildNodes_is_empty() {
        var text = new VirtualTextNode("test");
        Assert.Empty(text.ChildNodes);
    }

    [Fact]
    public void FirstChild_and_LastChild_are_null() {
        var text = new VirtualTextNode("test");
        Assert.Null(text.FirstChild);
        Assert.Null(text.LastChild);
    }
}
