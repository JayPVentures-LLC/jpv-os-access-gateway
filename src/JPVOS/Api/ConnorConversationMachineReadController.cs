using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JPVOS.Services.Outbound;

namespace JPVOS.Api;

[ApiController]
[Route("api/machine-read/connor-conversation")]
public sealed class ConnorConversationMachineReadController : ControllerBase
{
    private readonly IDirectConversationStore _conversationStore;
    private readonly IConfiguration _configuration;

    public ConnorConversationMachineReadController(IDirectConversationStore conversationStore, IConfiguration configuration)
    {
        _conversationStore = conversationStore;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!MachineReadAuth.IsAuthorized(Request, _configuration))
            return Unauthorized(new { error = "machine_read_auth_invalid" });

        var messages = await _conversationStore.GetConversationAsync(DirectConversationService.ConnorConversationId, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            conversation = "connor-direct",
            principalId = PrincipalSmsBindingResolver.ConnorPrincipalId,
            messages = messages.Select(message => new
            {
                message.MessageId,
                direction = message.Direction.ToString().ToLowerInvariant(),
                message.Body,
                message.CreatedAt,
                deliveryState = message.DeliveryState?.ToString().ToLowerInvariant()
            })
        });
    }
}
