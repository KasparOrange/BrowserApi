using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Fetch;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Fetch;

[Collection("JsObject")]
public class ResponseExtensionsTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public ResponseExtensionsTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    [Fact]
    public void EnsureSuccess_returns_response_when_ok() {
        var response = new Response { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["ok"] = true;

        var result = response.EnsureSuccess();

        Assert.Same(response, result);
    }

    [Fact]
    public void EnsureSuccess_throws_when_not_ok() {
        var response = new Response { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["ok"] = false;
        _mock.PropertyValues["status"] = (ushort)404;
        _mock.PropertyValues["statusText"] = "Not Found";

        Assert.Throws<System.Net.Http.HttpRequestException>(() => response.EnsureSuccess());
    }
}
