using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalExecutionStoreTests
{
    [Fact]
    public void Projector_replays_status_changes()
    {
        var events = new[]
        {
            ProposalLifecycleEvent.Registered("JPV-001", "Test proposal", ProposalClass.GrantProposal, ProposalLane.Enterprise),
            ProposalLifecycleEvent.StatusChanged("JPV-001", ProposalStatus.Submitted, "record-1")
        };

        var projection = ProposalProjector.Project(events);

        Assert.Equal(ProposalStatus.Submitted, projection.Status);
        Assert.Equal("Test proposal", projection.Title);
    }
}
