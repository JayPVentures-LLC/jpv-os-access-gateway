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
        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        if (stream.Count == 0) throw new ProposalValidationException("Proposal does not exist.");
        var current = ProposalProjector.Project(stream);
        var validation = validator.ValidateTransition(current, next, evidenceReference, authorityReference);
        if (!validation.IsValid) throw new ProposalValidationException(validation.Reason);

        await store.AppendAsync(ProposalLifecycleEvent.StatusChanged(proposalId, next, evidenceReference, authorityReference, idempotencyKey), cancellationToken);
        return ProposalProjector.Project(await store.ReadStreamAsync(proposalId, cancellationToken));
    }

    public async Task<ProposalProjection?> GetAsync(string proposalId, CancellationToken cancellationToken)
    {
        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        return stream.Count == 0 ? null : ProposalProjector.Project(stream);
    }
}
