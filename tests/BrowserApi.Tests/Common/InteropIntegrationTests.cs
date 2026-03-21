using BrowserApi.Common;
using BrowserApi.Dom;

namespace BrowserApi.Tests.Common;

[Collection("JsObject")]
public class InteropIntegrationTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public InteropIntegrationTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    [Fact]
    public void Node_AppendChild_delegates_to_Invoke() {
        var parent = new Node { Handle = new JsHandle(new object()) };
        var childHandle = new JsHandle("child-ref");
        var child = new Node { Handle = childHandle };

        _mock.InvokeReturnValue = new JsHandle("result-ref");
        parent.AppendChild(child);

        Assert.Single(_mock.Calls);
        Assert.Equal("Invoke", _mock.Calls[0].Method);
        Assert.Equal("appendChild", _mock.Calls[0].Name);
        Assert.Equal(childHandle, _mock.Calls[0].Args[0]);
    }

    [Fact]
    public void Element_GetAttribute_delegates_to_Invoke() {
        var element = new Element { Handle = new JsHandle(new object()) };
        _mock.InvokeReturnValue = "my-class";

        var result = element.GetAttribute("class");

        Assert.Equal("my-class", result);
        Assert.Single(_mock.Calls);
        Assert.Equal("Invoke", _mock.Calls[0].Method);
        Assert.Equal("getAttribute", _mock.Calls[0].Name);
        Assert.Equal("class", _mock.Calls[0].Args[0]);
    }

    [Fact]
    public void Element_SetAttribute_delegates_to_InvokeVoid() {
        var element = new Element { Handle = new JsHandle(new object()) };

        element.SetAttribute("id", "main");

        Assert.Single(_mock.Calls);
        Assert.Equal("InvokeVoid", _mock.Calls[0].Method);
        Assert.Equal("setAttribute", _mock.Calls[0].Name);
        Assert.Equal("id", _mock.Calls[0].Args[0]);
        Assert.Equal("main", _mock.Calls[0].Args[1]);
    }

    [Fact]
    public void Node_readonly_property_delegates_to_GetProperty() {
        var node = new Node { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["nodeType"] = (ushort)1;

        var result = node.NodeType;

        Assert.Equal((ushort)1, result);
        Assert.Single(_mock.Calls);
        Assert.Equal("GetProperty", _mock.Calls[0].Method);
        Assert.Equal("nodeType", _mock.Calls[0].Name);
    }

    [Fact]
    public void Node_readwrite_property_delegates_to_SetProperty() {
        var node = new Node { Handle = new JsHandle(new object()) };

        node.TextContent = "hello";

        Assert.Single(_mock.Calls);
        Assert.Equal("SetProperty", _mock.Calls[0].Method);
        Assert.Equal("textContent", _mock.Calls[0].Name);
        Assert.Equal("hello", _mock.Calls[0].Args[0]);
    }

    [Fact]
    public void EventTarget_is_root_JsObject() {
        Assert.True(typeof(EventTarget).IsSubclassOf(typeof(JsObject)));
    }

    [Fact]
    public void Node_inherits_JsObject_through_EventTarget() {
        Assert.True(typeof(Node).IsSubclassOf(typeof(JsObject)));
    }
}
