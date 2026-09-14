using BestStories.Models;

namespace BestStories.Services;

public interface IHackerNewsClient
{
    Task<IList<Story>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
    Task<Story?> GetStoryAsync(int id, CancellationToken cancellationToken = default);
}
