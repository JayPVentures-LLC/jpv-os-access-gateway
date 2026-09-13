using JPVOS.Services.Outbound;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JPVOS.Api;

[ApiController]
[Route("api/machine/relationships/connor/conversation")]
public sealed class ConnorConversationMachineReadController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IDirectConversationStore _store;

    public ConnorConversationMachineReadController(IConfiguration configuration, IDirectConversationStore store)
    {
        _configuration = configuration;
        _store = store;
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
        var projection = new ConnorConversationProjectionService(_store);
        return Ok(await projection.ReadAsync(cancellationToken));
    }
}
