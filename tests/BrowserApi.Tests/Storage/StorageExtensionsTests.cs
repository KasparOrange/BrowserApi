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

    [Fact]
    public void OnStorageChanged_callback_receives_event_args() {
        StorageChangedEventArgs? receivedArgs = null;

        _window.OnStorageChanged(args => { receivedArgs = args; });

        // Extract the callback registered with AddEventListener and invoke it
        var addCall = _mock.Calls.First(c => c.Method == "AddEventListener" && c.Name == "storage");
        var callback = (Action<JsHandle>)addCall.Args[0]!;

        // Simulate the StorageEvent properties the callback will read
        _mock.PropertyValues["key"] = "theme";
        _mock.PropertyValues["oldValue"] = "light";
        _mock.PropertyValues["newValue"] = "dark";
        _mock.PropertyValues["url"] = "https://example.com";

        callback(new JsHandle(new object()));

        Assert.NotNull(receivedArgs);
        Assert.Equal("theme", receivedArgs!.Key);
        Assert.Equal("light", receivedArgs.OldValue);
        Assert.Equal("dark", receivedArgs.NewValue);
        Assert.Equal("https://example.com", receivedArgs.Url);
    }
}
