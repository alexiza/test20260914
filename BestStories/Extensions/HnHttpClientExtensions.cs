
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
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => ((int)msg.StatusCode) >= 500)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100))
                );
        });

        return services;
    }
}
