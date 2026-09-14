using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BestStories.Tests.Integration;

public static class IntegrationTestFactory
{
    /// <summary>
    /// Create a WebApplicationFactory configured to use the provided HttpMessageHandler for the named "hn" HttpClient.
    /// An optional configureServices action may be used to register additional test-only services or options.
    /// </summary>
    public static WebApplicationFactory<Program> CreateFactory(HttpMessageHandler handler, Action<IServiceCollection> configureServices = null)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the named "hn" client primary handler so tests don't call the live API.
                services.AddHttpClient("hn").ConfigurePrimaryHttpMessageHandler(() => handler);

                configureServices?.Invoke(services);
            });
        });
    }
}
