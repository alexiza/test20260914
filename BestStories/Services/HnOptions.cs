namespace BestStories.Services;

public class HnOptions()
{
    // Maximum number of parallel item fetches
    public int MaxDegreeOfParallelism { get; set; } = 20;

    // How long to cache the best IDs (seconds)
    public int BestIdsCacheSeconds { get; set; } = 60;

    // How long to cache individual items (minutes)
    public int ItemCacheMinutes { get; set; } = 5;

    // Maximum count accepted from callers
    public int MaxCount { get; set; } = 500;
}
