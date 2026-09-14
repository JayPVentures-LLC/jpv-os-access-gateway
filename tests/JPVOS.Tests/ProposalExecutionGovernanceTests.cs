using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalExecutionGovernanceTests
{
    [Fact]
    public void Projector_preserves_authority_obligation_outcome_and_lineage()
    {
        var events = new[]
        {
            ProposalLifecycleEvent.Registered("JPV-GOV-1", "Governed", ProposalClass.PublicInterestFramework, ProposalLane.Labs),
            ProposalLifecycleEvent.AuthorityMapped("JPV-GOV-1", new AuthorityAssignment("review-authority", "reviewer-1", "record-a")),
            ProposalLifecycleEvent.ObligationAdded("JPV-GOV-1", new ImplementationObligation("obl-1", "operator-1", "Publish verification record", "merge receipt", false, null)),
            ProposalLifecycleEvent.OutcomeRecorded("JPV-GOV-1", new OutcomeMeasurement("adoption-rate", "not-yet-measurable", null)),
            ProposalLifecycleEvent.LineageAdded("JPV-GOV-1", new ProposalLineage("derives-from", "artifact-v1"))
        };

        var projection = ProposalProjector.Project(events);

        Assert.Single(projection.Authorities);
        Assert.Single(projection.Obligations);
        Assert.Equal("not-yet-measurable", projection.LatestOutcome!.Disposition);
        Assert.Single(projection.Lineage);
    }
}
