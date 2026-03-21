using System.Text.Json;
using BrowserApi.Dom;

namespace BrowserApi.Fetch;

public static class ResponseExtensions {
    public static async Task<T> JsonAsync<T>(this Response response) {
        var text = await response.TextAsync();
        return JsonSerializer.Deserialize<T>(text, JsonSerializerOptions.Web)!;
    }

    public static Response EnsureSuccess(this Response response) {
        if (!response.Ok)
            throw new System.Net.Http.HttpRequestException($"Response status code does not indicate success: {response.Status} ({response.StatusText}).");
        return response;
    }
}
