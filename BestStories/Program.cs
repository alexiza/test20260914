using BestStories.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("hn", c =>
{
    c.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("BestStoriesClient/1.0");
});
builder.Services.AddScoped<IHackerNewsClient, HackerNewsClient>();

var app = builder.Build();

app.MapControllers();

app.Run();

