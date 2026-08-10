using Microsoft.AspNetCore.Mvc;

namespace JPVOS.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            app = "JPV Nexus",
            runtime = ".NET",
            utc = DateTime.UtcNow
        });
    }
}
