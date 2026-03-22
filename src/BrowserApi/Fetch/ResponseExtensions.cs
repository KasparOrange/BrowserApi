using System.Text.Json;
using BrowserApi.Dom;

namespace BrowserApi.Fetch;

/// <summary>
/// Provides extension methods on <see cref="Response"/> for common response-handling patterns
/// such as JSON deserialization and success assertion.
/// </summary>
/// <seealso cref="Response"/>
/// <seealso cref="RequestBuilder"/>
public static class ResponseExtensions {
    /// <summary>
    /// Reads the response body as text and deserializes it from JSON into <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response body into.</typeparam>
    /// <param name="response">The HTTP response to read.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// Uses <see cref="JsonSerializerOptions.Web"/> for camelCase property name mapping and
    /// other web-friendly defaults. The response body is consumed by this call.
    /// </remarks>
    /// <example>
    /// <code>
    /// var response = await Http.Get("/api/users/1").SendAsync();
    /// var user = await response.JsonAsync&lt;User&gt;();
    /// Console.WriteLine(user.Name);
    /// </code>
    /// </example>
    public static async Task<T> JsonAsync<T>(this Response response) {
        var text = await response.TextAsync();
        return JsonSerializer.Deserialize<T>(text, JsonSerializerOptions.Web)!;
    }

    /// <summary>
    /// Throws an <see cref="System.Net.Http.HttpRequestException"/> if the response status code
    /// does not indicate success (i.e., <see cref="Response.Ok"/> is <see langword="false"/>).
    /// </summary>
    /// <param name="response">The HTTP response to check.</param>
    /// <returns>The same <see cref="Response"/> for method chaining when successful.</returns>
    /// <exception cref="System.Net.Http.HttpRequestException">
    /// Thrown when the response status code is outside the 200-299 range.
    /// </exception>
    /// <example>
    /// <code>
    /// var response = await Http.Get("/api/data").SendAsync();
    /// response.EnsureSuccess(); // throws if status is 4xx or 5xx
    /// var text = await response.TextAsync();
    /// </code>
    /// </example>
    public static Response EnsureSuccess(this Response response) {
        if (!response.Ok)
            throw new System.Net.Http.HttpRequestException($"Response status code does not indicate success: {response.Status} ({response.StatusText}).");
        return response;
    }
}
