using BrowserApi.JSInterop;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApi.Blazor;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddBrowserApi(this IServiceCollection services) {
        services.AddScoped<JSInteropBackend>();
        return services;
    }
}
