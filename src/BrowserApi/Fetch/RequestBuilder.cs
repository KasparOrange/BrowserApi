using System.Text.Json;
using BrowserApi.Common;
using BrowserApi.Dom;

namespace BrowserApi.Fetch;

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

    public RequestBuilder WithHeader(string name, string value) {
        _headers[name] = value;
        return this;
    }

    public RequestBuilder WithHeaders(params (string name, string value)[] headers) {
        foreach (var (name, value) in headers)
            _headers[name] = value;
        return this;
    }

    public RequestBuilder WithBody(string body) {
        _body = body;
        return this;
    }

    public RequestBuilder WithJsonBody(object body) {
        _body = JsonSerializer.Serialize(body, JsonSerializerOptions.Web);
        if (!_headers.ContainsKey("Content-Type"))
            _headers["Content-Type"] = "application/json";
        return this;
    }

    public RequestBuilder WithMode(RequestMode mode) {
        _mode = mode;
        return this;
    }

    public RequestBuilder WithCredentials(RequestCredentials credentials) {
        _credentials = credentials;
        return this;
    }

    public RequestBuilder WithCache(RequestCache cache) {
        _cache = cache;
        return this;
    }

    public RequestBuilder WithRedirect(RequestRedirect redirect) {
        _redirect = redirect;
        return this;
    }

    public RequestBuilder WithSignal(AbortSignal signal) {
        _signal = signal;
        return this;
    }

    public async Task<Response> SendAsync() {
        var init = BuildInit();
        var window = new Window { Handle = JsObject.Backend.GetGlobal("window") };
        return await window.FetchAsync(_url, init);
    }

    public async Task<T> SendJsonAsync<T>() {
        var response = await SendAsync();
        response.EnsureSuccess();
        return await response.JsonAsync<T>();
    }

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
