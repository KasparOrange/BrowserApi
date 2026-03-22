using BrowserApi.Common;
using BrowserApi.Dom;

namespace BrowserApi.Tests.Common;

[Collection("JsObject")]
public class JsObjectBulkExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public JsObjectBulkExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    [Fact]
    public async Task GetPropertiesAsync_calls_getProperties() {
        var element = new Element { Handle = new JsHandle(new object()) };
        _mock.InvokeAsyncReturnValue = new Dictionary<string, object?> {
            ["textContent"] = "hello",
            ["className"] = "active",
            ["id"] = "main"
        };

        var result = await element.GetPropertiesAsync("textContent", "className", "id");

        Assert.Equal(3, result.Count);
        Assert.Equal("hello", result["textContent"]);
        Assert.Equal("active", result["className"]);
        Assert.Equal("main", result["id"]);
        Assert.Contains(_mock.Calls, c => c.Name == "getProperties");
    }

    [Fact]
    public async Task GetPropertiesAsync_returns_empty_dict_for_null() {
        var element = new Element { Handle = new JsHandle(new object()) };
        _mock.InvokeAsyncReturnValue = null;

        var result = await element.GetPropertiesAsync("textContent");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertiesAsync_single_call_for_multiple_properties() {
        var element = new Element { Handle = new JsHandle(new object()) };
        _mock.InvokeAsyncReturnValue = new Dictionary<string, object?> {
            ["a"] = 1, ["b"] = 2, ["c"] = 3
        };

        await element.GetPropertiesAsync("a", "b", "c");

        // Should be: 1 GetGlobal("browserApi") + 1 InvokeAsync("getProperties")
        var invokeCalls = _mock.Calls.Where(c => c.Method == "InvokeAsync").ToList();
        Assert.Single(invokeCalls);
    }
}
