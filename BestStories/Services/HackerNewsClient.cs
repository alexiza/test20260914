using BestStories.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BestStories.Services;

internal class HackerNewsItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? By { get; set; }
    public long Time { get; set; }
    public int Score { get; set; }
    public int? Descendants { get; set; }
    public string? Type { get; set; }
}

public class HackerNewsClient(IHttpClientFactory httpFactory, IMemoryCache cache, IOptions<HnOptions> options) : IHackerNewsClient
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IMemoryCache _cache = cache;
    private readonly HnOptions _opts = options?.Value ?? new HnOptions();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private async Task<T?> GetOrCreateWithLockAsync<T>(string key, TimeSpan expiration, Func<Task<T?>> factory)
        where T : class
    {
        if (_cache.TryGetValue<T>(key, out var existing))
        {
            return existing;
        }

        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            if (_cache.TryGetValue<T>(key, out existing))
            {
                return existing;
            }

            var value = await factory();

            if (value is not null)
            {
                _cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });
            }

            return value;
        }
        finally
        {
            sem.Release();
            _locks.TryRemove(key, out _);
        }
    }

    private async Task<Story?> GetStoryAsync(int id, HttpClient client, CancellationToken ct)
    {
        var cacheKey = $"hn:item:{id}";
        var item = await GetOrCreateWithLockAsync<HackerNewsItem>(cacheKey, TimeSpan.FromMinutes(_opts.ItemCacheMinutes), async () =>
        {
            var r = await client.GetAsync($"item/{id}.json", ct);
            if (!r.IsSuccessStatusCode) return null;
            var body = await r.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<HackerNewsItem>(body, _jsonOptions);
        });

        if (item is null || !string.Equals(item.Type, "story", StringComparison.OrdinalIgnoreCase)) return null;

        var story = new Story
        {
            Title = item.Title,
            Uri = item.Url,
            PostedBy = item.By,
            Time = DateTimeOffset.FromUnixTimeSeconds(item.Time).ToString("o"),
            Score = item.Score,
            CommentCount = item.Descendants ?? 0
        };

        return story;
    }

    public async Task<Story?> GetStoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("hn");
        return await GetStoryAsync(id, client, cancellationToken);
    }

    public async Task<IList<Story>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0) return [];

        var client = _httpFactory.CreateClient("hn");

        var ids = await GetOrCreateWithLockAsync<int[]>("hn:bestIds", TimeSpan.FromSeconds(_opts.BestIdsCacheSeconds), async () =>
        {
            var resp = await client.GetAsync("beststories.json", cancellationToken);
            resp.EnsureSuccessStatusCode();
            var s = await resp.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<int[]>(s, _jsonOptions) ?? Array.Empty<int>();
        }) ?? Array.Empty<int>();

        var bag = new ConcurrentBag<Story>();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _opts.MaxDegreeOfParallelism, CancellationToken = cancellationToken };

        await Parallel.ForEachAsync(ids, parallelOptions, async (id, ct) =>
        {
            try
            {
                var story = await GetStoryAsync(id, client, ct);
                if (story is not null)
                {
                    bag.Add(story);
                }
            }
            catch
            {
                // ignore individual failures
            }
        });

        var list = new List<Story>(bag);
        list.Sort((a, b) => b.Score.CompareTo(a.Score));
        var top = list.Count > count ? list.GetRange(0, count) : list;

        return top;
    }
}
