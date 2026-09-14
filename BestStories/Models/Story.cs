namespace BestStories.Models;
public sealed record Story
{
    public string? Title { get; init; }
    public string? Uri { get; init; }
    public string? PostedBy { get; init; }
    public string? Time { get; init; }
    public int Score { get; init; }
    public int CommentCount { get; init; }
}
