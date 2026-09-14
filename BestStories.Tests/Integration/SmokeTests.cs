using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BestStories.Tests.Integration;

public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // use the injected factory and replace the named "hn" client handler for deterministic tests
    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("hn").ConfigurePrimaryHttpMessageHandler(() => new TestHelpers.FakeHandler());
        });
    });

    [Fact]
    public async Task Get_Default_ReturnsOk()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<BestStories.Services.IHackerNewsClient>();

        var stories = await svc.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.NotNull(stories);
        Assert.Equal(2, stories.Count);
    }
}
