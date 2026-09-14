using BestStories.Services;
using BestStories.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHnHttpClient(builder.Configuration);
// Bind HackerNews options from configuration
builder.Services.Configure<HnOptions>(builder.Configuration.GetSection("HackerNews"));

builder.Services.AddScoped<IHackerNewsClient, HackerNewsClient>();

var app = builder.Build();

app.MapControllers();

app.Run();

