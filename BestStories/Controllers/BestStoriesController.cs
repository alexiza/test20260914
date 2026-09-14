using BestStories.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BestStories.Controllers;

[ApiController]
[Route("api/beststories")]
public class BestStoriesController(IHackerNewsClient hn, IOptions<HnOptions> options) : ControllerBase
{
    private readonly IHackerNewsClient _hn = hn;
    private readonly HnOptions _opts = options.Value;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        if (count <= 0) return BadRequest("count must be greater than zero");
        if (count > _opts.MaxCount) return BadRequest($"count must be {_opts.MaxCount} or less");

        var stories = await _hn.GetBestStoriesAsync(count, cancellationToken);
        return Ok(stories);
    }
}
