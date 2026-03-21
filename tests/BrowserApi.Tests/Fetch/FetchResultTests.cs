using BrowserApi.Common;
using BrowserApi.Dom;
using BrowserApi.Fetch;

namespace BrowserApi.Tests.Fetch;

public class FetchResultTests {
    [Fact]
    public void Success_creates_successful_result() {
        var response = new Response { Handle = new JsHandle(new object()) };
        var result = FetchResult.Success(response);

        Assert.True(result.IsSuccess);
        Assert.Same(response, result.Response);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_creates_failed_result() {
        var error = new InvalidOperationException("test error");
        var result = FetchResult.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Response);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Generic_Success_creates_result_with_value() {
        var response = new Response { Handle = new JsHandle(new object()) };
        var result = FetchResult<string>.Success("hello", response);

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
        Assert.Same(response, result.Response);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Generic_Failure_creates_result_with_error() {
        var error = new InvalidOperationException("test error");
        var result = FetchResult<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Null(result.Response);
        Assert.Same(error, result.Error);
    }
}
