namespace BrowserApi.Fetch;

/// <summary>
/// The primary entry point for the fluent HTTP (Fetch API) client. Provides factory methods
/// for each HTTP method that return a <see cref="RequestBuilder"/> for further configuration.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Http"/> mirrors the browser Fetch API with a fluent, strongly-typed C# surface.
/// Each method creates a <see cref="RequestBuilder"/> pre-configured with the URL and HTTP method;
/// from there you can add headers, set a body, configure CORS mode, and finally send the request.
/// </para>
/// <para>
/// For the simplest case -- a JSON GET request -- use the <see cref="GetAsync{T}"/> shortcut,
/// which sends the request, asserts success, and deserializes the response in one call.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple GET and deserialize
/// var user = await Http.GetAsync&lt;User&gt;("/api/users/42");
///
/// // POST with JSON body and custom headers
/// var created = await Http.Post("/api/users")
///     .WithJsonBody(new { Name = "Alice", Email = "alice@example.com" })
///     .WithHeader("Authorization", "Bearer token123")
///     .SendJsonAsync&lt;User&gt;();
///
/// // Non-throwing pattern
/// var result = await Http.Get("/api/data").TrySendAsync();
/// if (result.IsSuccess)
///     Console.WriteLine(result.Response!.Status);
/// </code>
/// </example>
/// <seealso cref="RequestBuilder"/>
/// <seealso cref="FetchResult"/>
/// <seealso cref="FetchResult{T}"/>
public static class Http {
    /// <summary>
    /// Creates a <see cref="RequestBuilder"/> for an HTTP <c>GET</c> request to the specified URL.
    /// </summary>
    /// <param name="url">The request URL (absolute or relative).</param>
    /// <returns>A <see cref="RequestBuilder"/> for further configuration before sending.</returns>
    public static RequestBuilder Get(string url) => new(url, "GET");

    /// <summary>
    /// Creates a <see cref="RequestBuilder"/> for an HTTP <c>POST</c> request to the specified URL.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns>A <see cref="RequestBuilder"/> for further configuration before sending.</returns>
    public static RequestBuilder Post(string url) => new(url, "POST");

    /// <summary>
    /// Creates a <see cref="RequestBuilder"/> for an HTTP <c>PUT</c> request to the specified URL.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns>A <see cref="RequestBuilder"/> for further configuration before sending.</returns>
    public static RequestBuilder Put(string url) => new(url, "PUT");

    /// <summary>
    /// Creates a <see cref="RequestBuilder"/> for an HTTP <c>PATCH</c> request to the specified URL.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns>A <see cref="RequestBuilder"/> for further configuration before sending.</returns>
    public static RequestBuilder Patch(string url) => new(url, "PATCH");

    /// <summary>
    /// Creates a <see cref="RequestBuilder"/> for an HTTP <c>DELETE</c> request to the specified URL.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <returns>A <see cref="RequestBuilder"/> for further configuration before sending.</returns>
    public static RequestBuilder Delete(string url) => new(url, "DELETE");

    /// <summary>
    /// Sends an HTTP <c>GET</c> request, asserts success, and deserializes the JSON response body
    /// into <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response into.</typeparam>
    /// <param name="url">The request URL.</param>
    /// <returns>The deserialized response body.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">
    /// Thrown if the response status code does not indicate success.
    /// </exception>
    /// <example>
    /// <code>
    /// var users = await Http.GetAsync&lt;List&lt;User&gt;&gt;("/api/users");
    /// </code>
    /// </example>
    public static Task<T> GetAsync<T>(string url) => Get(url).SendJsonAsync<T>();
}
