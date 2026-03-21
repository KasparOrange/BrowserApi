using BrowserApi.Dom;

namespace BrowserApi.Fetch;

public readonly struct FetchResult {
    public bool IsSuccess { get; }
    public Response? Response { get; }
    public System.Exception? Error { get; }

    private FetchResult(bool isSuccess, Response? response, System.Exception? error) {
        IsSuccess = isSuccess;
        Response = response;
        Error = error;
    }

    public static FetchResult Success(Response response) => new(true, response, null);
    public static FetchResult Failure(System.Exception error) => new(false, null, error);
}

public readonly struct FetchResult<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Response? Response { get; }
    public System.Exception? Error { get; }

    private FetchResult(bool isSuccess, T? value, Response? response, System.Exception? error) {
        IsSuccess = isSuccess;
        Value = value;
        Response = response;
        Error = error;
    }

    public static FetchResult<T> Success(T value, Response response) => new(true, value, response, null);
    public static FetchResult<T> Failure(System.Exception error) => new(false, default, null, error);
}
