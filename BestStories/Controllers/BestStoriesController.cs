using BestStories.Services;
using Microsoft.AspNetCore.Mvc;

namespace BestStories.Controllers;

[ApiController]
[Route("api/beststories")]
public class BestStoriesController(IHackerNewsClient hn) : ControllerBase
{
    private readonly IHackerNewsClient _hn = hn;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        if (count <= 0) return BadRequest("count must be greater than zero");
        if (count > 500) return BadRequest("count must be 500 or less");

        var stories = await _hn.GetBestStoriesAsync(count, cancellationToken);
        return Ok(stories);
    }
}
