namespace JPVOS.Services.ProposalExecution;

public interface IProposalRegistryService
{
    Task<ProposalProjection> RegisterAsync(string proposalId, string title, ProposalClass proposalClass, ProposalLane lane, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> RecordStatusAsync(string proposalId, ProposalStatus next, string? evidenceReference, string? authorityReference, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> LinkEvidenceAsync(string proposalId, ClassifiedEvidenceReference evidence, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> MapAuthorityAsync(string proposalId, AuthorityAssignment authority, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> SetAdoptionRouteAsync(string proposalId, AdoptionRoute route, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> AddObligationAsync(string proposalId, ImplementationObligation obligation, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> AddVerificationRequirementAsync(string proposalId, VerificationRequirement requirement, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> SetReviewRequirementAsync(string proposalId, ReviewRequirement requirement, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> RecordOutcomeAsync(string proposalId, OutcomeMeasurement outcome, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> AddLineageAsync(string proposalId, ProposalLineage lineage, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection> RecordReleaseAsync(string proposalId, bool approved, string? summary, string? idempotencyKey, CancellationToken cancellationToken);
    Task<ProposalProjection?> GetAsync(string proposalId, CancellationToken cancellationToken);
}

public sealed class ProposalValidationException(string message) : Exception(message);
public sealed class ProposalPersistenceException(string message, Exception? inner = null) : Exception(message, inner);
