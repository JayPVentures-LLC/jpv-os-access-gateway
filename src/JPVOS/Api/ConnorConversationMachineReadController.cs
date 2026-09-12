using JPVOS.Services.Outbound;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JPVOS.Api;

[ApiController]
[Route("api/machine/relationships/connor/conversation")]
public sealed class ConnorConversationMachineReadController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ConnorConversationProjectionService _projection;

    public ConnorConversationMachineReadController(IConfiguration configuration, ConnorConversationProjectionService projection)
    {
        _configuration = configuration;
        _projection = projection;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var expectedDigest = _configuration["JPV_MCP_CONNOR_READ_TOKEN_SHA256"];
        if (string.IsNullOrWhiteSpace(expectedDigest))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "connor_machine_read_not_provisioned" });

        if (!MachineReadTokenAuthenticator.IsAuthorized(Request.Headers.Authorization.ToString(), expectedDigest))
            return Unauthorized(new { error = "connor_machine_read_unauthorized" });

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Ok(await _projection.ReadAsync(cancellationToken));
    }
}
