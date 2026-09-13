namespace JPVOS.Services.ProposalExecution;

public interface IProposalEventStore
{
    Task AppendAsync(ProposalLifecycleEvent item, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalLifecycleEvent>> ReadStreamAsync(string proposalId, CancellationToken cancellationToken);
}
