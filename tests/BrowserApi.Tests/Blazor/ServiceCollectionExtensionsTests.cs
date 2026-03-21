using BrowserApi.Blazor;
using BrowserApi.JSInterop;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApi.Tests.Blazor;

public class ServiceCollectionExtensionsTests {
    [Fact]
    public void AddBrowserApi_registers_JSInteropBackend() {
        var services = new ServiceCollection();

        services.AddBrowserApi();

        var descriptor = Assert.Single(services, s => s.ServiceType == typeof(JSInteropBackend));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddBrowserApi_returns_service_collection_for_chaining() {
        var services = new ServiceCollection();

        var result = services.AddBrowserApi();

        Assert.Same(services, result);
    }
}
