using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace BestStories.Tests.Integration;

public static class TestHelpers
{
    public class FakeHandler() : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
        }
    }

    public class NonStoryHandler() : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase))
            {
                var body = JsonSerializer.Serialize(new[] { 1, 2 });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
            }

            if (uri.Contains("/item/"))
            {
                var body = JsonSerializer.Serialize(new { id = 1, type = "job" });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    public class CountingHandler() : HttpMessageHandler
    {
        public int BeststoriesCalls;
        public ConcurrentDictionary<int, int> ItemCalls = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase) || uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref BeststoriesCalls);
                var body = JsonSerializer.Serialize(new[] { 2, 1 });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
            }

            if (uri.Contains("/item/"))
            {
                var seg = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var idSeg = seg[^1];
                var idStr = idSeg.Replace(".json", string.Empty);
                if (!int.TryParse(idStr, out var id))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                ItemCalls.AddOrUpdate(id, 1, (_, v) => v + 1);

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
        }
    }

    public class BeststoriesErrorHandler() : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    public class ItemErrorHandler() : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase))
            {
                var body = JsonSerializer.Serialize(new[] { 1, 2 });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
            }

            if (uri.Contains("/item/"))
            {
                // simulate server error for id 1
                if (uri.Contains("/1.json"))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

                var idSeg = uri.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
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
        }
    }

    public class ConcurrencyTrackingHandler(int delayMs) : HttpMessageHandler
    {
        private int _active;
        private int _maxObserved;
        private readonly int _delayMs = delayMs;

        public int MaxObservedConcurrency => _maxObserved;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (uri.EndsWith("beststories.json", StringComparison.OrdinalIgnoreCase))
            {
                var body = JsonSerializer.Serialize(new[] { 1, 2, 3, 4, 5 });
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
            }

            if (uri.Contains("/item/"))
            {
                var current = Interlocked.Increment(ref _active);
                try
                {
                    // record max
                    int prev;
                    do
                    {
                        prev = _maxObserved;
                        if (current <= prev) break;
                    }
                    while (Interlocked.CompareExchange(ref _maxObserved, current, prev) != prev);

                    await Task.Delay(_delayMs, cancellationToken);

                    var seg = uri.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var idSeg = seg[^1];
                    var idStr = idSeg.Replace(".json", string.Empty);
                    if (!int.TryParse(idStr, out var id))
                        return new HttpResponseMessage(HttpStatusCode.NotFound);

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
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
                }
                finally
                {
                    Interlocked.Decrement(ref _active);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
