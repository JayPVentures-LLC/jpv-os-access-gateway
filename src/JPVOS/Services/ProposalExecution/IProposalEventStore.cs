namespace JPVOS.Services.ProposalExecution;

public interface IProposalEventStore
{
    Task AppendAsync(ProposalLifecycleEvent item, CancellationToken cancellationToken);
    Task AppendAsync(ProposalLifecycleEvent item, long expectedVersion, CancellationToken cancellationToken);
    Task<ProposalLifecycleEvent?> FindByIdempotencyKeyAsync(string proposalId, string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalLifecycleEvent>> ReadStreamAsync(string proposalId, CancellationToken cancellationToken);
}
