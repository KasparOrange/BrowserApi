using BrowserApi.Dom;

namespace BrowserApi.Fetch;

/// <summary>
/// Represents the outcome of an HTTP request that may have succeeded or failed, without throwing
/// exceptions on HTTP errors.
/// </summary>
/// <remarks>
/// <para>
/// Use this type when you want to handle HTTP failures as values rather than exceptions.
/// It is returned by <see cref="RequestBuilder.TrySendAsync()"/>.
/// </para>
/// <para>
/// Check <see cref="IsSuccess"/> first. On success, <see cref="Response"/> contains the
/// <see cref="Dom.Response"/>. On failure, <see cref="Error"/> contains the exception.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await Http.Get("/api/data").TrySendAsync();
/// if (result.IsSuccess)
///     Console.WriteLine($"Status: {result.Response!.Status}");
/// else
///     Console.WriteLine($"Error: {result.Error!.Message}");
/// </code>
/// </example>
/// <seealso cref="FetchResult{T}"/>
/// <seealso cref="RequestBuilder.TrySendAsync()"/>
public readonly struct FetchResult {
    /// <summary>
    /// Gets a value indicating whether the request completed successfully with an HTTP success status code.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the HTTP response when <see cref="IsSuccess"/> is <see langword="true"/>; otherwise <see langword="null"/>.
    /// </summary>
    public Response? Response { get; }

    /// <summary>
    /// Gets the exception that caused the failure when <see cref="IsSuccess"/> is <see langword="false"/>; otherwise <see langword="null"/>.
    /// </summary>
    public System.Exception? Error { get; }

    private FetchResult(bool isSuccess, Response? response, System.Exception? error) {
        IsSuccess = isSuccess;
        Response = response;
        Error = error;
    }

    /// <summary>
    /// Creates a successful <see cref="FetchResult"/> wrapping the given response.
    /// </summary>
    /// <param name="response">The successful HTTP response.</param>
    /// <returns>A <see cref="FetchResult"/> with <see cref="IsSuccess"/> set to <see langword="true"/>.</returns>
    public static FetchResult Success(Response response) => new(true, response, null);

    /// <summary>
    /// Creates a failed <see cref="FetchResult"/> wrapping the given exception.
    /// </summary>
    /// <param name="error">The exception that describes the failure.</param>
    /// <returns>A <see cref="FetchResult"/> with <see cref="IsSuccess"/> set to <see langword="false"/>.</returns>
    public static FetchResult Failure(System.Exception error) => new(false, null, error);
}

/// <summary>
/// Represents the outcome of an HTTP request that, on success, includes a deserialized value of
/// type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type that the response body was deserialized into.</typeparam>
/// <remarks>
/// <para>
/// This is the generic counterpart of <see cref="FetchResult"/>. It is returned by
/// <see cref="RequestBuilder.TrySendAsync{T}()"/>. On success, <see cref="Value"/> contains the
/// deserialized body and <see cref="Response"/> contains the raw HTTP response.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await Http.Get("/api/users/42").TrySendAsync&lt;User&gt;();
/// if (result.IsSuccess)
///     Console.WriteLine($"User: {result.Value!.Name}");
/// else
///     Console.WriteLine($"Failed: {result.Error!.Message}");
/// </code>
/// </example>
/// <seealso cref="FetchResult"/>
/// <seealso cref="RequestBuilder.TrySendAsync{T}()"/>
public readonly struct FetchResult<T> {
    /// <summary>
    /// Gets a value indicating whether the request completed successfully and the body was
    /// deserialized without error.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the deserialized response body when <see cref="IsSuccess"/> is <see langword="true"/>;
    /// otherwise the default value of <typeparamref name="T"/>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the HTTP response when <see cref="IsSuccess"/> is <see langword="true"/>; otherwise <see langword="null"/>.
    /// </summary>
    public Response? Response { get; }

    /// <summary>
    /// Gets the exception that caused the failure when <see cref="IsSuccess"/> is <see langword="false"/>; otherwise <see langword="null"/>.
    /// </summary>
    public System.Exception? Error { get; }

    private FetchResult(bool isSuccess, T? value, Response? response, System.Exception? error) {
        IsSuccess = isSuccess;
        Value = value;
        Response = response;
        Error = error;
    }

    /// <summary>
    /// Creates a successful <see cref="FetchResult{T}"/> with the deserialized value and response.
    /// </summary>
    /// <param name="value">The deserialized response body.</param>
    /// <param name="response">The successful HTTP response.</param>
    /// <returns>A <see cref="FetchResult{T}"/> with <see cref="IsSuccess"/> set to <see langword="true"/>.</returns>
    public static FetchResult<T> Success(T value, Response response) => new(true, value, response, null);

    /// <summary>
    /// Creates a failed <see cref="FetchResult{T}"/> wrapping the given exception.
    /// </summary>
    /// <param name="error">The exception that describes the failure.</param>
    /// <returns>A <see cref="FetchResult{T}"/> with <see cref="IsSuccess"/> set to <see langword="false"/>.</returns>
    public static FetchResult<T> Failure(System.Exception error) => new(false, default, null, error);
}
