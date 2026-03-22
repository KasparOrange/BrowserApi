using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Dom;

[Collection("JsObject")]
public class WindowPartialTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly Window _window;

    public WindowPartialTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _window = new Window { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void Console_returns_Console_instance() {
        _mock.PropertyValues["console"] = new JsHandle(new object());

        var console = _window.Console;

        Assert.NotNull(console);
        Assert.IsType<BrowserApi.Console.Console>(console);
        Assert.Contains(_mock.Calls, c => c.Name == "console");
    }
}
