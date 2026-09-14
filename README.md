# BestStories

Simple ASP.NET Core API that returns the top N Hacker News "best" stories by score.

Prerequisites
- .NET 10 SDK installed (dotnet --version should report a 10.x SDK)

Run locally
1. Restore dependencies:

	 dotnet restore

2. Build the solution:

	 dotnet build ./BestStories.slnx --configuration Release

3. Run the API (from the repository root):

	 dotnet run --project BestStories --configuration Release

The API will listen on the default ASP.NET Core URL(s). Use the endpoint below to fetch results.

API
- GET /api/beststories?count=10

Examples
- See `requests.http` at the repository root for convenient example requests you can run from supported IDEs/editors (for example, Visual Studio/VS Code HTTP request file support or REST client extensions).

Tests
- Run the test suite:

	dotnet test ./BestStories.slnx --configuration Release

Notes
- Caches the list of best IDs for 60s and individual items for 5 minutes to avoid overloading Hacker News.
- Limits parallel item fetches to reduce remote pressure.
- Uses IMemoryCache for lightweight in-process caching.
- Resiliency: the named `hn` HttpClient is configured via `AddHnHttpClient(...)` and includes a Polly `WaitAndRetryAsync` policy with exponential backoff and jitter to handle transient errors.
- Configuration: `HnOptions` exposes runtime tunables (BaseAddress, TimeoutSeconds, MaxDegreeOfParallelism, BestIdsCacheSeconds, ItemCacheMinutes, GlobalMaxConcurrency, MaxCount) and is bound from configuration (HackerNews section).
- Tests: the repository includes unit and integration tests (xUnit + WebApplicationFactory) and a `requests.http` file with example requests for IDE convenience.
- CI: a GitHub Actions workflow runs build and tests and supports `workflow_dispatch` for manual runs.
