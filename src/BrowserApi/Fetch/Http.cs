namespace BrowserApi.Fetch;

public static class Http {
    public static RequestBuilder Get(string url) => new(url, "GET");
    public static RequestBuilder Post(string url) => new(url, "POST");
    public static RequestBuilder Put(string url) => new(url, "PUT");
    public static RequestBuilder Patch(string url) => new(url, "PATCH");
    public static RequestBuilder Delete(string url) => new(url, "DELETE");

    public static Task<T> GetAsync<T>(string url) => Get(url).SendJsonAsync<T>();
}
