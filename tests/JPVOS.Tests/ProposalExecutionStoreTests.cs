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
    public async Task Store_assigns_monotonic_sequence_and_deduplicates_equivalent_key()
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

    [Fact]
    public async Task Store_rejects_idempotency_key_reuse_with_different_payload()
    {
        var store = new SqliteProposalEventStore(_path);
        await store.AppendAsync(ProposalLifecycleEvent.Registered("JPV-003", "Original", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "same-key"), default);

        await Assert.ThrowsAsync<ProposalIdempotencyConflictException>(() =>
            store.AppendAsync(ProposalLifecycleEvent.Registered("JPV-003", "Different", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "same-key"), default));
    }

    [Fact]
    public async Task Store_rejects_stale_expected_version()
    {
        var store = new SqliteProposalEventStore(_path);
        await store.AppendAsync(ProposalLifecycleEvent.Registered("JPV-004", "Versioned", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "r1"), default);
        await store.AppendAsync(ProposalLifecycleEvent.StatusChanged("JPV-004", ProposalStatus.Validated, "e1", idempotencyKey: "s1"), expectedVersion: 1, default);

        await Assert.ThrowsAsync<ProposalConcurrencyException>(() =>
            store.AppendAsync(ProposalLifecycleEvent.StatusChanged("JPV-004", ProposalStatus.ApprovedInternally, "e2", idempotencyKey: "s2"), expectedVersion: 1, default));
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
