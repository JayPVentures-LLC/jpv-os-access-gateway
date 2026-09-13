namespace JPVOS.Services.ProposalExecution;

public interface IProposalRegistryService
{
    Task<ProposalProjection> RegisterAsync(string proposalId, string title, ProposalClass proposalClass, ProposalLane lane, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> RecordStatusAsync(string proposalId, ProposalStatus next, string? evidenceReference, string? authorityReference, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> RecordReleaseAsync(string proposalId, bool approved, string? summary, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection?> GetAsync(string proposalId, CancellationToken cancellationToken);
}

public sealed class ProposalValidationException(string message) : Exception(message);
public sealed class ProposalPersistenceException(string message, Exception? inner = null) : Exception(message, inner);
