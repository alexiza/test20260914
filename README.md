# BestStories

Simple ASP.NET Core API that returns the top N Hacker News "best" stories by score.

Run:

dotnet run

GET /api/beststories?count=10

Notes:
- Caches list of best IDs for 60s and individual items for 5 minutes to avoid overloading HN.
- Limits parallel item fetches to reduce remote pressure.
