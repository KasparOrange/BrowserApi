using BrowserApi.Common;
using BrowserApi.Fetch;
using BrowserApi.Tests.Common;

namespace BrowserApi.Tests.Fetch;

[Collection("JsObject")]
public class RequestBuilderTests : IDisposable {
    private readonly MockBrowserBackend _mock;

    public RequestBuilderTests() {
        _mock = new MockBrowserBackend();
        JsObject.Backend = _mock;
    }

    public void Dispose() { }

    [Fact]
    public void Get_creates_builder_with_GET_method() {
        var builder = Http.Get("https://example.com/api");
        Assert.NotNull(builder);
    }

    [Fact]
    public void Post_creates_builder_with_POST_method() {
        var builder = Http.Post("https://example.com/api");
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithHeader_returns_same_builder_for_chaining() {
        var builder = Http.Get("https://example.com");
        var result = builder.WithHeader("Accept", "application/json");
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithJsonBody_returns_same_builder_for_chaining() {
        var builder = Http.Post("https://example.com");
        var result = builder.WithJsonBody(new { name = "test" });
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithBody_returns_same_builder_for_chaining() {
        var builder = Http.Post("https://example.com");
        var result = builder.WithBody("raw body");
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithCredentials_returns_same_builder_for_chaining() {
        var builder = Http.Get("https://example.com");
        var result = builder.WithCredentials(RequestCredentials.Include);
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithMode_returns_same_builder_for_chaining() {
        var builder = Http.Get("https://example.com");
        var result = builder.WithMode(RequestMode.Cors);
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithCache_returns_same_builder_for_chaining() {
        var builder = Http.Get("https://example.com");
        var result = builder.WithCache(RequestCache.NoCache);
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithRedirect_returns_same_builder_for_chaining() {
        var builder = Http.Get("https://example.com");
        var result = builder.WithRedirect(RequestRedirect.Follow);
        Assert.Same(builder, result);
    }

    [Fact]
    public void All_http_methods_available() {
        Assert.NotNull(Http.Get("url"));
        Assert.NotNull(Http.Post("url"));
        Assert.NotNull(Http.Put("url"));
        Assert.NotNull(Http.Patch("url"));
        Assert.NotNull(Http.Delete("url"));
    }

    [Fact]
    public void WithHeaders_accepts_multiple_headers() {
        var builder = Http.Get("https://example.com");
        var result = builder.WithHeaders(
            ("Accept", "application/json"),
            ("X-Custom", "value")
        );
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithSignal_returns_same_builder_for_chaining() {
        var builder = Http.Get("https://example.com");
        var signal = new BrowserApi.Dom.AbortSignal { Handle = new JsHandle(new object()) };
        var result = builder.WithSignal(signal);
        Assert.Same(builder, result);
    }

    [Fact]
    public async Task SendAsync_calls_fetch_on_window() {
        var builder = Http.Post("https://example.com/api")
            .WithBody("test body");

        // The mock will return a handle for GetGlobal("window") and
        // a handle for InvokeAsync("fetch", ...) which represents the Response
        _mock.InvokeAsyncReturnValue = new JsHandle(new object());

        var response = await builder.SendAsync();

        Assert.NotNull(response);
        Assert.Contains(_mock.Calls, c => c.Method == "GetGlobal" && c.Name == "window");
        Assert.Contains(_mock.Calls, c => c.Method == "InvokeAsync" && c.Name == "fetch");
    }

    [Fact]
    public async Task SendAsync_GET_with_no_options_passes_null_init() {
        var builder = Http.Get("https://example.com");

        _mock.InvokeAsyncReturnValue = new JsHandle(new object());

        await builder.SendAsync();

        var fetchCall = _mock.Calls.First(c => c.Method == "InvokeAsync" && c.Name == "fetch");
        // For a simple GET with no headers/body/options, init should be null
        Assert.Equal("https://example.com", fetchCall.Args[0]);
        Assert.Null(fetchCall.Args[1]);
    }

    [Fact]
    public async Task SendAsync_POST_passes_method_in_init() {
        var builder = Http.Post("https://example.com/api");

        _mock.InvokeAsyncReturnValue = new JsHandle(new object());

        await builder.SendAsync();

        var fetchCall = _mock.Calls.First(c => c.Method == "InvokeAsync" && c.Name == "fetch");
        Assert.Equal("https://example.com/api", fetchCall.Args[0]);
        // init should not be null for POST
        Assert.NotNull(fetchCall.Args[1]);
    }
}
