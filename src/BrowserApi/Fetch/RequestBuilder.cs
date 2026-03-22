using System.Text.Json;
using BrowserApi.Common;
using BrowserApi.Dom;

namespace BrowserApi.Fetch;

/// <summary>
/// A fluent builder for constructing and sending HTTP requests via the browser Fetch API.
/// </summary>
/// <remarks>
/// <para>
/// Instances are created by the factory methods on <see cref="Http"/> (e.g.,
/// <see cref="Http.Get"/>, <see cref="Http.Post"/>). Configure the request with
/// <c>With*</c> methods, then finalize with one of the send methods:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="SendAsync"/> -- returns the raw <see cref="Response"/>.</description></item>
///   <item><description><see cref="SendJsonAsync{T}"/> -- asserts success and deserializes JSON.</description></item>
///   <item><description><see cref="TrySendAsync()"/> -- returns a <see cref="FetchResult"/> without throwing.</description></item>
///   <item><description><see cref="TrySendAsync{T}()"/> -- returns a <see cref="FetchResult{T}"/> without throwing.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // POST with JSON body
/// var user = await Http.Post("/api/users")
///     .WithJsonBody(new { Name = "Alice" })
///     .WithHeader("Authorization", "Bearer token")
///     .SendJsonAsync&lt;User&gt;();
///
/// // GET with abort signal
/// var controller = new AbortController();
/// var response = await Http.Get("/api/stream")
///     .WithSignal(controller.Signal)
///     .SendAsync();
/// </code>
/// </example>
/// <seealso cref="Http"/>
/// <seealso cref="FetchResult"/>
/// <seealso cref="ResponseExtensions"/>
public sealed class RequestBuilder {
    private readonly string _url;
    private readonly string _method;
    private readonly Dictionary<string, string> _headers = new();
    private object? _body;
    private RequestMode? _mode;
    private RequestCredentials? _credentials;
    private RequestCache? _cache;
    private RequestRedirect? _redirect;
    private AbortSignal? _signal;

    internal RequestBuilder(string url, string method) {
        _url = url;
        _method = method;
    }

    /// <summary>
    /// Adds or replaces a single HTTP header on the request.
    /// </summary>
    /// <param name="name">The header name (e.g., <c>"Authorization"</c>, <c>"Accept"</c>).</param>
    /// <param name="value">The header value.</param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithHeader(string name, string value) {
        _headers[name] = value;
        return this;
    }

    /// <summary>
    /// Adds or replaces multiple HTTP headers on the request.
    /// </summary>
    /// <param name="headers">
    /// An array of (name, value) tuples representing the headers to set.
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var response = await Http.Get("/api/data")
    ///     .WithHeaders(
    ///         ("Accept", "application/json"),
    ///         ("X-Custom", "value"))
    ///     .SendAsync();
    /// </code>
    /// </example>
    public RequestBuilder WithHeaders(params (string name, string value)[] headers) {
        foreach (var (name, value) in headers)
            _headers[name] = value;
        return this;
    }

    /// <summary>
    /// Sets the request body to a raw string.
    /// </summary>
    /// <param name="body">The string body content.</param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithBody(string body) {
        _body = body;
        return this;
    }

    /// <summary>
    /// Serializes the given object to JSON and sets it as the request body.
    /// </summary>
    /// <param name="body">The object to serialize.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// If no <c>Content-Type</c> header has been explicitly set, this method automatically
    /// sets it to <c>"application/json"</c>. Serialization uses
    /// <see cref="JsonSerializerOptions.Web"/> for camelCase naming.
    /// </remarks>
    public RequestBuilder WithJsonBody(object body) {
        _body = JsonSerializer.Serialize(body, JsonSerializerOptions.Web);
        if (!_headers.ContainsKey("Content-Type"))
            _headers["Content-Type"] = "application/json";
        return this;
    }

    /// <summary>
    /// Sets the CORS mode for the request.
    /// </summary>
    /// <param name="mode">The request mode (e.g., <c>cors</c>, <c>no-cors</c>, <c>same-origin</c>).</param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithMode(RequestMode mode) {
        _mode = mode;
        return this;
    }

    /// <summary>
    /// Sets the credentials policy for the request.
    /// </summary>
    /// <param name="credentials">
    /// The credentials mode (e.g., <c>omit</c>, <c>same-origin</c>, <c>include</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithCredentials(RequestCredentials credentials) {
        _credentials = credentials;
        return this;
    }

    /// <summary>
    /// Sets the cache mode for the request.
    /// </summary>
    /// <param name="cache">
    /// The cache mode (e.g., <c>default</c>, <c>no-cache</c>, <c>reload</c>, <c>force-cache</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithCache(RequestCache cache) {
        _cache = cache;
        return this;
    }

    /// <summary>
    /// Sets the redirect handling mode for the request.
    /// </summary>
    /// <param name="redirect">
    /// The redirect mode (e.g., <c>follow</c>, <c>error</c>, <c>manual</c>).
    /// </param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithRedirect(RequestRedirect redirect) {
        _redirect = redirect;
        return this;
    }

    /// <summary>
    /// Associates an <see cref="AbortSignal"/> with the request, allowing it to be cancelled.
    /// </summary>
    /// <param name="signal">The abort signal to monitor for cancellation.</param>
    /// <returns>This builder for method chaining.</returns>
    public RequestBuilder WithSignal(AbortSignal signal) {
        _signal = signal;
        return this;
    }

    /// <summary>
    /// Sends the request and returns the raw <see cref="Response"/>.
    /// </summary>
    /// <returns>The HTTP <see cref="Response"/> from the Fetch API.</returns>
    /// <remarks>
    /// This method does not throw on non-success status codes. Check <see cref="Response.Ok"/>
    /// or call <see cref="ResponseExtensions.EnsureSuccess"/> on the result.
    /// </remarks>
    public async Task<Response> SendAsync() {
        var init = BuildInit();
        var window = new Window { Handle = JsObject.Backend.GetGlobal("window") };
        return await window.FetchAsync(_url, init);
    }

    /// <summary>
    /// Sends the request, asserts a success status code, and deserializes the JSON response
    /// body into <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response body into.</typeparam>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">
    /// Thrown when the response status code is outside the 200-299 range.
    /// </exception>
    public async Task<T> SendJsonAsync<T>() {
        var response = await SendAsync();
        response.EnsureSuccess();
        return await response.JsonAsync<T>();
    }

    /// <summary>
    /// Sends the request and returns a <see cref="FetchResult"/> that encapsulates either the
    /// response or the error, without throwing exceptions.
    /// </summary>
    /// <returns>
    /// A <see cref="FetchResult"/> whose <see cref="FetchResult.IsSuccess"/> indicates whether
    /// the request completed with an HTTP success status code.
    /// </returns>
    public async Task<FetchResult> TrySendAsync() {
        try {
            var response = await SendAsync();
            if (!response.Ok)
                return FetchResult.Failure(new System.Net.Http.HttpRequestException($"Response status code does not indicate success: {response.Status}."));
            return FetchResult.Success(response);
        } catch (System.Exception ex) {
            return FetchResult.Failure(ex);
        }
    }

    /// <summary>
    /// Sends the request and returns a <see cref="FetchResult{T}"/> containing the deserialized
    /// body on success, or the error on failure, without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response body into.</typeparam>
    /// <returns>
    /// A <see cref="FetchResult{T}"/> whose <see cref="FetchResult{T}.IsSuccess"/> indicates
    /// whether the request succeeded and the body was deserialized.
    /// </returns>
    public async Task<FetchResult<T>> TrySendAsync<T>() {
        try {
            var response = await SendAsync();
            if (!response.Ok)
                return FetchResult<T>.Failure(new System.Net.Http.HttpRequestException($"Response status code does not indicate success: {response.Status}."));
            var value = await response.JsonAsync<T>();
            return FetchResult<T>.Success(value, response);
        } catch (System.Exception ex) {
            return FetchResult<T>.Failure(ex);
        }
    }

    private RequestInit? BuildInit() {
        var hasOptions = _method != "GET" || _headers.Count > 0 || _body != null ||
                         _mode.HasValue || _credentials.HasValue || _cache.HasValue ||
                         _redirect.HasValue || _signal != null;

        if (!hasOptions)
            return null;

        return new RequestInit {
            Method = _method != "GET" ? _method : null,
            Headers = _headers.Count > 0 ? _headers : null,
            Body = _body!,
            Mode = _mode,
            Credentials = _credentials,
            Cache = _cache,
            Redirect = _redirect,
            Signal = _signal
        };
    }
}
