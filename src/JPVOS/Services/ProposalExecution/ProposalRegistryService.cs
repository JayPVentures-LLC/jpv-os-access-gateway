namespace JPVOS.Services.ProposalExecution;

public sealed class ProposalRegistryService(IProposalEventStore store, ProposalLifecycleValidator validator) : IProposalRegistryService
{
    public async Task<ProposalProjection> RegisterAsync(string proposalId, string title, ProposalClass proposalClass, ProposalLane lane, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(proposalId)) throw new ProposalValidationException("Proposal ID is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new ProposalValidationException("Proposal title is required.");
        var existing = await store.ReadStreamAsync(proposalId, cancellationToken);
        if (existing.Count > 0) return ProposalProjector.Project(existing);
        await store.AppendAsync(ProposalLifecycleEvent.Registered(proposalId, title, proposalClass, lane, idempotencyKey), cancellationToken);
        return await GetRequiredAsync(proposalId, cancellationToken);
    }

    public async Task<ProposalProjection> RecordStatusAsync(string proposalId, ProposalStatus next, string? evidenceReference, string? authorityReference, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var current = await GetRequiredAsync(proposalId, cancellationToken);
        var validation = validator.ValidateTransition(current, next, evidenceReference, authorityReference);
        if (!validation.IsValid) throw new ProposalValidationException(validation.Reason);
        return await AppendAndReadAsync(ProposalLifecycleEvent.StatusChanged(proposalId, next, evidenceReference, authorityReference, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> MapAuthorityAsync(string proposalId, AuthorityAssignment authority, string? idempotencyKey, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(proposalId, cancellationToken);
        if (string.IsNullOrWhiteSpace(authority.Role) || string.IsNullOrWhiteSpace(authority.AuthorityReference)) throw new ProposalValidationException("Authority role and reference are required.");
        return await AppendAndReadAsync(ProposalLifecycleEvent.AuthorityMapped(proposalId, authority, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> AddObligationAsync(string proposalId, ImplementationObligation obligation, string? idempotencyKey, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(proposalId, cancellationToken);
        if (string.IsNullOrWhiteSpace(obligation.ObligationId) || string.IsNullOrWhiteSpace(obligation.CompletionEvidenceRequirement)) throw new ProposalValidationException("Obligation ID and completion evidence requirement are required.");
        return await AppendAndReadAsync(ProposalLifecycleEvent.ObligationAdded(proposalId, obligation, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> RecordOutcomeAsync(string proposalId, OutcomeMeasurement outcome, string? idempotencyKey, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(proposalId, cancellationToken);
        if (string.IsNullOrWhiteSpace(outcome.Metric) || string.IsNullOrWhiteSpace(outcome.Disposition)) throw new ProposalValidationException("Outcome metric and disposition are required.");
        return await AppendAndReadAsync(ProposalLifecycleEvent.OutcomeRecorded(proposalId, outcome, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> AddLineageAsync(string proposalId, ProposalLineage lineage, string? idempotencyKey, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(proposalId, cancellationToken);
        if (string.IsNullOrWhiteSpace(lineage.Relationship) || string.IsNullOrWhiteSpace(lineage.RelatedId)) throw new ProposalValidationException("Lineage relationship and related ID are required.");
        return await AppendAndReadAsync(ProposalLifecycleEvent.LineageAdded(proposalId, lineage, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> RecordReleaseAsync(string proposalId, bool approved, string? summary, string? idempotencyKey, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(proposalId, cancellationToken);
        if (approved && string.IsNullOrWhiteSpace(summary)) throw new ProposalValidationException("An approved release requires a public summary.");
        return await AppendAndReadAsync(ProposalLifecycleEvent.PublicationReviewed(proposalId, approved, summary, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection?> GetAsync(string proposalId, CancellationToken cancellationToken)
    {
        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        return stream.Count == 0 ? null : ProposalProjector.Project(stream);
    }

    private async Task<ProposalProjection> AppendAndReadAsync(ProposalLifecycleEvent item, CancellationToken cancellationToken)
    {
        await store.AppendAsync(item, cancellationToken);
        return await GetRequiredAsync(item.ProposalId, cancellationToken);
    }

    private async Task<ProposalProjection> GetRequiredAsync(string proposalId, CancellationToken cancellationToken) =>
        await GetAsync(proposalId, cancellationToken) ?? throw new ProposalValidationException("Proposal does not exist.");
}
