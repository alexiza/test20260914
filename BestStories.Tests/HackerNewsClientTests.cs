using BestStories.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http;
using Moq;
using Moq.Protected;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using Xunit;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace BestStories.Tests;

public class HackerNewsClientTests()
{
    private static IHttpClientFactory CreateFactory(HttpClient client)
    {
        return new SimpleFactory(client);
    }

    private class SimpleFactory(HttpClient client) : IHttpClientFactory
    {
        private readonly HttpClient _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsTopN()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, ct) =>
            {
                var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (uri.EndsWith("/beststories.json", StringComparison.OrdinalIgnoreCase) || uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase))
                {
                    var body = JsonSerializer.Serialize(new[] { 2, 1, 3 });
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
                }

                if (uri.Contains("/item/"))
                {
                    var seg = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var idSeg = seg[^1];
                    var idStr = idSeg.Replace(".json", string.Empty);
                    if (!int.TryParse(idStr, out var id))
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                    var item = new Dictionary<string, object>
                    {
                        ["id"] = id,
                        ["title"] = $"Title {id}",
                        ["url"] = $"https://example.org/{id}",
                        ["by"] = $"user{id}",
                        ["time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ["score"] = id * 10,
                        ["descendants"] = id * 2,
                        ["type"] = "story"
                    };

                    var body = JsonSerializer.Serialize(item);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        var client = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/") };
        var factory = CreateFactory(client);
        var cache = new MemoryCache(new MemoryCacheOptions());

        var hn = new HackerNewsClient(factory, cache, Options.Create(new HnOptions()));

        var stories = await hn.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.NotNull(stories);
        Assert.Equal(2, stories.Count);
        // ids returned by beststories.json were [2,1,3] and score = id*10, so highest score is id 3 (score 30)
        Assert.Equal("Title 3", stories[0].Title);
        Assert.Equal(30, stories[0].Score);
    }

    [Fact]
    public async Task GetStoryAsync_ReturnsSingleStory()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, ct) =>
            {
                var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (uri.Contains("/item/"))
                {
                    var seg = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var idSeg = seg[^1];
                    var idStr = idSeg.Replace(".json", string.Empty);
                    if (!int.TryParse(idStr, out var id))
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                    var item = new Dictionary<string, object>
                    {
                        ["id"] = id,
                        ["title"] = $"Title {id}",
                        ["url"] = $"https://example.org/{id}",
                        ["by"] = $"user{id}",
                        ["time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ["score"] = id * 10,
                        ["descendants"] = id * 2,
                        ["type"] = "story"
                    };

                    var body = JsonSerializer.Serialize(item);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        var client = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/") };
        var factory = CreateFactory(client);
        var cache = new MemoryCache(new MemoryCacheOptions());

        var hn = new HackerNewsClient(factory, cache, Options.Create(new HnOptions()));

        var story = await hn.GetStoryAsync(1, CancellationToken.None);

        Assert.NotNull(story);
        Assert.Equal("Title 1", story!.Title);
        Assert.Equal(10, story.Score);
    }
}
