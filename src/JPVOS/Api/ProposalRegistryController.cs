using JPVOS.Services.ProposalExecution;
using Microsoft.AspNetCore.Mvc;

namespace JPVOS.Api;

[ApiController]
[Route("api/proposals")]
public sealed class ProposalRegistryController(IProposalRegistryService registry) : ControllerBase
{
    [HttpGet("{proposalId}/status")]
    public async Task<IActionResult> GetStatus(string proposalId, CancellationToken cancellationToken)
    {
        var proposal = await registry.GetAsync(proposalId, cancellationToken);
        if (proposal is null || !proposal.PublicReleaseApproved) return NotFound();
        return Ok(new PublicProposalStatus(
            proposal.ProposalId,
            proposal.Title,
            proposal.Status.ToString(),
            proposal.PublicSummary,
            proposal.LastUpdatedAtUtc));
    }

    public sealed record PublicProposalStatus(string ProposalId, string Title, string Status, string? Summary, DateTime LastUpdatedAtUtc);
}
