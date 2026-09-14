API Reference — BestStories API

Overview

This service returns the top N Hacker News "best" stories (as determined by the Hacker News API) ordered by score.

Base URLs (development)
- HTTP: http://localhost:5062

Endpoints

GET /api/beststories
- Query parameters
	- count (optional, integer): number of stories to return. Default = 10. Must be > 0 and <= MaxCount (configured via HnOptions).

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
		- When count > MaxCount

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
	- Caching is implemented with IMemoryCache.
	- Best story IDs are cached using key `hn:bestIds` for `BestIdsCacheSeconds`.
	- Individual item responses are cached using key `hn:item:{id}` for `ItemCacheMinutes`.
	- Cache durations are configuration-driven via the `HackerNews` section (HnOptions).
	- Per-key semaphore locking is used to reduce cache stampedes on concurrent misses.

- Concurrency, timeout, and resiliency
	- Item fetches are performed in parallel per request with `MaxDegreeOfParallelism` (default 20).
	- The named `hn` HttpClient timeout is configurable via `TimeoutSeconds` (default 10 seconds).
	- A Polly retry policy handles transient failures (5xx responses, HttpRequestException, and timeouts) with exponential backoff + jitter.
	- A Polly bulkhead policy enforces a global outbound concurrency cap via `GlobalMaxConcurrency` (default 100).

Testing

- Unit and integration tests are included in BestStories.Tests (xUnit + WebApplicationFactory).
- Tests cover both story aggregation behavior and HTTP integration behavior using mocked upstream responses.
- To run tests locally:
	dotnet test

Configuration

- Behavior is configured via the `HackerNews` section and bound to HnOptions.
- Example configuration:

```json
"HackerNews": {
	"BaseAddress": "https://hacker-news.firebaseio.com/v0/",
	"TimeoutSeconds": 10,
	"BestIdsCacheSeconds": 60,
	"ItemCacheMinutes": 5,
	"MaxDegreeOfParallelism": 20,
	"GlobalMaxConcurrency": 100,
	"MaxCount": 500
}
```

Notes and assumptions

- This API intentionally limits the maximum count to `MaxCount` to avoid excessive work and network traffic.
- Time is returned in ISO 8601 format (UTC offset preserved).
- The service uses caching, retries, timeout, and bulkhead isolation to improve resiliency when calling Hacker News.

Enhancements (things to consider given more time)
- Add request-level rate limiting to protect both this service and Hacker News.
- Add health and metrics endpoints (Prometheus metrics, health checks).
- Add integration tests that run against a recorded HTTP response fixture or a local stub server.

Contact / Support

If you need help running the service locally or want changes to the API, open an issue in the repository or contact the maintainer.
