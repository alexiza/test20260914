
using Polly;

namespace BestStories.Extensions;

public static class HnHttpClientExtensions
{
    /// <summary>
    /// Register the named "hn" HttpClient with base address and a Polly retry policy with jitter.
    /// </summary>
    public static IServiceCollection AddHnHttpClient(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddHttpClient("hn", c =>
        {
            c.BaseAddress = new Uri(configuration.GetValue<string>("HackerNews:BaseAddress") ?? "https://hacker-news.firebaseio.com/v0/");
            c.DefaultRequestHeaders.UserAgent.ParseAdd("BestStoriesClient/1.0");
        })
        .AddPolicyHandler(request =>
        {
            var retry = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => ((int)msg.StatusCode) >= 500)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100))
                );

            // Bulkhead to limit global concurrency across all callers. We resolve the option here
            // to avoid a captured service provider at startup; the delegate runs once when building
            // the pipeline, so it's acceptable to read configuration directly.
            var max = configuration.GetValue<int?>("HackerNews:GlobalMaxConcurrency") ?? 100;
            var bulk = Policy.BulkheadAsync<HttpResponseMessage>(max, int.MaxValue);

            return Policy.WrapAsync(bulk, retry);
        });

        return services;
    }
}
