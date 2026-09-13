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
        return ProposalProjector.Project(await store.ReadStreamAsync(proposalId, cancellationToken));
    }

    public async Task<ProposalProjection> RecordStatusAsync(string proposalId, ProposalStatus next, string? evidenceReference, string? authorityReference, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var current = await GetRequiredAsync(proposalId, cancellationToken);
        var validation = validator.ValidateTransition(current, next, evidenceReference, authorityReference);
        if (!validation.IsValid) throw new ProposalValidationException(validation.Reason);
        await store.AppendAsync(ProposalLifecycleEvent.StatusChanged(proposalId, next, evidenceReference, authorityReference, idempotencyKey), cancellationToken);
        return await GetRequiredAsync(proposalId, cancellationToken);
    }

    public async Task<ProposalProjection> RecordReleaseAsync(string proposalId, bool approved, string? summary, string? idempotencyKey, CancellationToken cancellationToken)
    {
        _ = await GetRequiredAsync(proposalId, cancellationToken);
        if (approved && string.IsNullOrWhiteSpace(summary)) throw new ProposalValidationException("An approved release requires a public summary.");
        await store.AppendAsync(ProposalLifecycleEvent.PublicationReviewed(proposalId, approved, summary, idempotencyKey), cancellationToken);
        return await GetRequiredAsync(proposalId, cancellationToken);
    }

    public async Task<ProposalProjection?> GetAsync(string proposalId, CancellationToken cancellationToken)
    {
        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        return stream.Count == 0 ? null : ProposalProjector.Project(stream);
    }

    private async Task<ProposalProjection> GetRequiredAsync(string proposalId, CancellationToken cancellationToken) =>
        await GetAsync(proposalId, cancellationToken) ?? throw new ProposalValidationException("Proposal does not exist.");
}
