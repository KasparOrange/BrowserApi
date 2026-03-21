using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.WebStorage;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Storage;

[Collection("JsObject")]
public class StorageExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;
    private readonly Window _window;

    public StorageExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
        _window = new Window { Handle = new JsHandle(new object()) };
    }

    public void Dispose() { }

    [Fact]
    public void TypedLocalStorage_returns_TypedStorage() {
        _mock.PropertyValues["localStorage"] = new JsHandle(new object());

        var storage = _window.TypedLocalStorage();

        Assert.NotNull(storage);
        Assert.Contains(_mock.Calls, c => c.Name == "localStorage");
    }

    [Fact]
    public void TypedSessionStorage_returns_TypedStorage() {
        _mock.PropertyValues["sessionStorage"] = new JsHandle(new object());

        var storage = _window.TypedSessionStorage();

        Assert.NotNull(storage);
        Assert.Contains(_mock.Calls, c => c.Name == "sessionStorage");
    }

    [Fact]
    public void OnStorageChanged_registers_event_listener() {
        _window.OnStorageChanged(_ => { });

        Assert.Contains(_mock.Calls, c => c.Method == "AddEventListener" && c.Name == "storage");
    }
}
