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

    [Fact]
    public void EnsureSuccess_returns_same_response_instance() {
        var response = new Response { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["ok"] = true;

        var result = response.EnsureSuccess();

        Assert.Same(response, result);
    }

    [Theory]
    [InlineData((ushort)200, "OK")]
    [InlineData((ushort)201, "Created")]
    [InlineData((ushort)204, "No Content")]
    [InlineData((ushort)299, "Custom Success")]
    public void EnsureSuccess_returns_self_for_various_success_statuses(ushort status, string statusText) {
        var response = new Response { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["ok"] = true;
        _mock.PropertyValues["status"] = status;
        _mock.PropertyValues["statusText"] = statusText;

        var result = response.EnsureSuccess();

        Assert.Same(response, result);
    }

    [Theory]
    [InlineData((ushort)400, "Bad Request")]
    [InlineData((ushort)401, "Unauthorized")]
    [InlineData((ushort)403, "Forbidden")]
    [InlineData((ushort)500, "Internal Server Error")]
    public void EnsureSuccess_throws_for_various_error_statuses(ushort status, string statusText) {
        var response = new Response { Handle = new JsHandle(new object()) };
        _mock.PropertyValues["ok"] = false;
        _mock.PropertyValues["status"] = status;
        _mock.PropertyValues["statusText"] = statusText;

        var ex = Assert.Throws<System.Net.Http.HttpRequestException>(() => response.EnsureSuccess());
        Assert.Contains(status.ToString(), ex.Message);
        Assert.Contains(statusText, ex.Message);
    }

    [Fact]
    public async Task JsonAsync_deserializes_response_text() {
        var response = new Response { Handle = new JsHandle(new object()) };
        _mock.InvokeAsyncReturnValue = "{\"name\":\"Alice\",\"age\":30}";

        var result = await response.JsonAsync<TestUser>();

        Assert.Equal("Alice", result.Name);
        Assert.Equal(30, result.Age);
    }

    private record TestUser {
        public string Name { get; init; } = "";
        public int Age { get; init; }
    }
}
