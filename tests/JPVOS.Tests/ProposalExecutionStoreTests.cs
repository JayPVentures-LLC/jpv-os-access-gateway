using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalExecutionStoreTests : IDisposable
{
    private readonly string _path = Path.Join(Path.GetTempPath(), $"jpv-proposals-{Guid.NewGuid():N}.db");

    [Fact]
    public void Projector_replays_status_changes()
    {
        var events = new[]
        {
            ProposalLifecycleEvent.Registered("JPV-001", "Test proposal", ProposalClass.GrantProposal, ProposalLane.Enterprise),
            ProposalLifecycleEvent.StatusChanged("JPV-001", ProposalStatus.Submitted, "record-1")
        };
        Assert.Equal(ProposalStatus.Submitted, ProposalProjector.Project(events).Status);
    }

    [Fact]
    public async Task Store_assigns_monotonic_sequence_and_deduplicates_key()
    {
        var store = new SqliteProposalEventStore(_path);
        var first = ProposalLifecycleEvent.Registered("JPV-002", "Durable", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "key-1");
        await store.AppendAsync(first, default);
        await store.AppendAsync(first, default);
        await store.AppendAsync(ProposalLifecycleEvent.StatusChanged("JPV-002", ProposalStatus.Validated, "record-2", idempotencyKey: "key-2"), default);

        var stream = await store.ReadStreamAsync("JPV-002", default);
        Assert.Equal(2, stream.Count);
        Assert.Equal(new long[] { 1, 2 }, stream.Select(x => x.Sequence));
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
