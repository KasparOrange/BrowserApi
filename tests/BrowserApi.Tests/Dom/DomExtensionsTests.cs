using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Dom;

[Collection("JsObject")]
public class DomExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public DomExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    [Fact]
    public void QuerySelector_generic_on_Document_returns_typed_element() {
        _mock.InvokeReturnValue = new JsHandle(new object());
        var doc = new Document { Handle = new JsHandle(new object()) };

        var input = doc.QuerySelector<HtmlInputElement>("#email");

        Assert.NotNull(input);
        Assert.IsType<HtmlInputElement>(input);
        Assert.Contains(_mock.Calls, c => c.Name == "querySelector");
    }

    [Fact]
    public void QuerySelector_generic_returns_null_when_not_found() {
        _mock.InvokeReturnValue = null;
        var doc = new Document { Handle = new JsHandle(new object()) };

        var input = doc.QuerySelector<HtmlInputElement>("#missing");

        Assert.Null(input);
    }

    [Fact]
    public void QuerySelector_generic_on_Element() {
        _mock.InvokeReturnValue = new JsHandle(new object());
        var element = new Element { Handle = new JsHandle(new object()) };

        var button = element.QuerySelector<HtmlButtonElement>("button");

        Assert.NotNull(button);
        Assert.IsType<HtmlButtonElement>(button);
    }

    [Fact]
    public void CreateElement_calls_createElement_with_derived_tag_name() {
        _mock.InvokeReturnValue = new JsHandle(new object());
        var doc = new Document { Handle = new JsHandle(new object()) };

        var input = doc.CreateElement<HtmlInputElement>();

        Assert.NotNull(input);
        Assert.IsType<HtmlInputElement>(input);
        var call = Assert.Single(_mock.Calls, c => c.Name == "createElement");
        Assert.Equal("input", call.Args[0]);
    }

    [Fact]
    public void CreateElement_derives_div_tag_name() {
        _mock.InvokeReturnValue = new JsHandle(new object());
        var doc = new Document { Handle = new JsHandle(new object()) };

        doc.CreateElement<HtmlDivElement>();

        var call = Assert.Single(_mock.Calls, c => c.Name == "createElement");
        Assert.Equal("div", call.Args[0]);
    }
}
