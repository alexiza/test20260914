API Reference — BestStories API

Overview

This service returns the top N Hacker News "best" stories (as determined by the Hacker News API) ordered by score.

Base URLs (development)
- HTTP: http://localhost:5062

Endpoints

GET /api/beststories
- Query parameters
	- count (optional, integer): number of stories to return. Default = 10. Must be > 0 and <= 500.

- Success response (200 OK)
	- Content-Type: application/json
	- Body: JSON array of story objects in descending order of score.

	Story object schema

```json
{
	"title": "A story title or null",
	"uri": "https://example.org/story-url or null",
	"postedBy": "username or null",
	"time": "2019-10-12T13:43:01+00:00",
	"score": 1716,
	"commentCount": 572
}
```

- Client errors
	- 400 Bad Request
		- When count <= 0
		- When count > 500

Examples

Curl
- Get the top 1 story over HTTP
	curl "http://localhost:5062/api/beststories?count=1" -H "Accept: application/json"

- Use the provided requests.http file (for REST client extensions)
	- edit requests.http variables at the top to match your base URL
	- open requests.http and send the request(s)

Behavior & Implementation Notes

- Hacker News integration
	- The service fetches the list of best story IDs from the Hacker News endpoint /v0/beststories.json.
	- The service then fetches individual item details from /v0/item/{id}.json and returns the top N by score.

- Caching
	- The list of best IDs is cached for 60 seconds to reduce pressure on Hacker News.
	- Individual item responses are cached for 5 minutes.
	- Caching is implemented with IMemoryCache in-memory cache. These durations are currently hard-coded in the service and may be changed in code (BestStories/Services/HackerNewsClient.cs).

- Concurrency
	- Item fetches are performed in parallel with a configured maximum degree of parallelism (20 by default) to balance speed and remote load.

Testing

- Unit tests are included in BestStories.Tests. The tests mock HTTP responses and exercise GetBestStoriesAsync and GetStoryAsync.
- To run tests locally:
	dotnet test

Configuration

- To change the base address used to call Hacker News, update the named HttpClient configuration in Program.cs:
	builder.Services.AddHttpClient("hn", c => { c.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/"); });

- To change cache lifetimes or parallelism, edit BestStories/Services/HackerNewsClient.cs.

Notes and assumptions

- This API intentionally limits the maximum count to 500 to avoid excessive work and network traffic.
- Time is returned in ISO 8601 format (UTC offset preserved).
- The service does not implement request throttling or API key management — these would be recommended enhancements for production usage.

Enhancements (things to consider given more time)
- Make cache TTLs and parallelism configurable via appsettings.json.
- Add request-level rate limiting to protect both this service and Hacker News.
- Add health and metrics endpoints (Prometheus metrics, health checks).
- Add integration tests that run against a recorded HTTP response fixture or a local stub server.

Contact / Support

If you need help running the service locally or want changes to the API, open an issue in the repository or contact the maintainer.
