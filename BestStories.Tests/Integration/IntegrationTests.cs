using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BestStories.Controllers;
using BestStories.Models;
using BestStories.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BestStories.Tests.Integration;

public class IntegrationTests()
{
    [Fact]
    public async Task GetBestStories_ExcludesNonStories()
    {
        var factory = IntegrationTestFactory.CreateFactory(new TestHelpers.NonStoryHandler());

        using var scope = factory.Services.CreateScope();

        var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        // The fake returns beststories [1,2] but items are type "job", so expect zero stories
        var stories = await svc.GetBestStoriesAsync(10, CancellationToken.None);

        Assert.NotNull(stories);
        Assert.Empty(stories);
    }

    [Fact]
    public async Task GetStory_ReturnsNull_ForNonStory()
    {
        var factory = IntegrationTestFactory.CreateFactory(new TestHelpers.NonStoryHandler());

        using var scope = factory.Services.CreateScope();

        var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        var story = await svc.GetStoryAsync(1, CancellationToken.None);
        Assert.Null(story);
    }

    [Fact]
    public async Task Controller_Validation_Behavior()
    {
        var factory = IntegrationTestFactory.CreateFactory(new TestHelpers.FakeHandler());

        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        var options = Options.Create(new HnOptions { MaxCount = 2 });
        var controller = new BestStoriesController(svc, options);

        // Negative count
        var negResult = await controller.Get(-1, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(negResult);

        // Too large
        var largeResult = await controller.Get(10, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(largeResult);

        // Valid
        var okResult = await controller.Get(2, CancellationToken.None) as OkObjectResult;
        Assert.NotNull(okResult);
        var list = Assert.IsAssignableFrom<IList<Story>>(okResult.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task CachingBehavior_ReusesCachedResponses()
    {
        var handler = new TestHelpers.CountingHandler();

        var factory = IntegrationTestFactory.CreateFactory(handler);

        using var scope1 = factory.Services.CreateScope();
        var svc1 = scope1.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        // first call should populate caches and hit endpoints
        var first = await svc1.GetBestStoriesAsync(2, CancellationToken.None);
        Assert.Equal(2, first.Count);

        // second call should use cached best ids and cached items (no extra handler calls)
        using var scope2 = factory.Services.CreateScope();
        var svc2 = scope2.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        var second = await svc2.GetBestStoriesAsync(2, CancellationToken.None);
        Assert.Equal(2, second.Count);

        // verify handler observed only one beststories request and one item request per id
        Assert.Equal(1, handler.BeststoriesCalls);
        foreach (var id in new[] { 2, 1 })
        {
            handler.ItemCalls.TryGetValue(id, out var c);
            Assert.Equal(1, c);
        }
    }

    [Fact]
    public async Task BeststoriesEndpoint_ReturnsServerError_Propagates()
    {
        var factory = IntegrationTestFactory.CreateFactory(new TestHelpers.BeststoriesErrorHandler());

        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        await Assert.ThrowsAsync<HttpRequestException>(() => svc.GetBestStoriesAsync(5, CancellationToken.None));
    }

    [Fact]
    public async Task ItemEndpoint_ReturnsServerError_IsIgnored()
    {
        var factory = IntegrationTestFactory.CreateFactory(new TestHelpers.ItemErrorHandler());

        using var scope = factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();

        var list = await svc.GetBestStoriesAsync(5, CancellationToken.None);

        // Item errors should be ignored and we should still return other items (Counting on handler to provide 2 items total)
        Assert.NotNull(list);
        Assert.True(list.Count <= 2);
    }

    [Fact]
    public async Task Parallelism_MaxDegreeOfParallelism_Respected()
    {
        // test degree = 1 (serial)
        var handler1 = new TestHelpers.ConcurrencyTrackingHandler(150);
        var factory1 = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient("hn").ConfigurePrimaryHttpMessageHandler(() => handler1);
                services.Configure<HnOptions>(o => o.MaxDegreeOfParallelism = 1);
            });
        });

        using (var scope = factory1.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();
            var list = await svc.GetBestStoriesAsync(5, CancellationToken.None);
            Assert.Equal(5, list.Count);
            Assert.InRange(handler1.MaxObservedConcurrency, 1, 1);
        }

        // test degree = 3 (parallel allowed)
        var handler3 = new TestHelpers.ConcurrencyTrackingHandler(150);
        var factory3 = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient("hn").ConfigurePrimaryHttpMessageHandler(() => handler3);
                services.Configure<HnOptions>(o => o.MaxDegreeOfParallelism = 3);
            });
        });

        using (var scope = factory3.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IHackerNewsClient>();
            var list = await svc.GetBestStoriesAsync(5, CancellationToken.None);
            Assert.Equal(5, list.Count);
            // MaxObservedConcurrency should not exceed configured degree
            Assert.InRange(handler3.MaxObservedConcurrency, 1, 3);
        }
    }
}
