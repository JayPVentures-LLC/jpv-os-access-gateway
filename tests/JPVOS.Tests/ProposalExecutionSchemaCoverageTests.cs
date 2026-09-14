using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalExecutionSchemaCoverageTests
{
    [Fact]
    public void Projector_preserves_evidence_route_verification_and_review_contracts()
    {
        var route = new AdoptionRoute(
            "institution-1", "official-portal", ["proposal.pdf"], "eligible applicant",
            "receipt-id", "triage then review", "alternate office", "resubmit on material revision", "submission receipt");
        var verification = new VerificationRequirement("verify-1", "repository-exact-head", "merge SHA and passing CI");
        var review = new ReviewRequirement("before-publication", true, "independent reviewer required");
        var evidence = new ClassifiedEvidenceReference("evidence-1", ProposalEvidenceClass.SourceRecord);

        var events = new[]
        {
            ProposalLifecycleEvent.Registered("JPV-SCHEMA-1", "Schema", ProposalClass.PublicPolicy, ProposalLane.Enterprise),
            ProposalLifecycleEvent.EvidenceLinked("JPV-SCHEMA-1", evidence),
            ProposalLifecycleEvent.AdoptionRouteSet("JPV-SCHEMA-1", route),
            ProposalLifecycleEvent.VerificationRequirementAdded("JPV-SCHEMA-1", verification),
            ProposalLifecycleEvent.ReviewRequirementSet("JPV-SCHEMA-1", review)
        };

        var projection = ProposalProjector.Project(events);

        Assert.Contains(evidence, projection.ClassifiedEvidence);
        var replayedRoute = Assert.IsType<AdoptionRoute>(projection.AdoptionRoute);
        Assert.Equal(route.TargetAuthority, replayedRoute.TargetAuthority);
        Assert.Equal(route.SubmissionMechanism, replayedRoute.SubmissionMechanism);
        Assert.Equal(route.RequiredArtifacts, replayedRoute.RequiredArtifacts);
        Assert.Equal(route.RequiredEvidence, replayedRoute.RequiredEvidence);
        Assert.Contains(verification, projection.VerificationRequirements);
        Assert.Equal(review, projection.ReviewRequirement);
    }

    [Fact]
    public void Outcome_contract_carries_measurement_method_and_revision_trigger()
    {
        var outcome = new OutcomeMeasurement(
            "adoption-rate", "not-yet-measurable", null,
            Baseline: "0", Target: "25%", Method: "registry count",
            DataSource: "proposal ledger", ObservationWindow: "90 days",
            AdverseEffectIndicator: "false-positive adoption state",
            ReviewOwner: "JPV-OS", ConfidenceNotes: "initial baseline",
            RevisionTrigger: "material measurement drift");

        Assert.Equal("registry count", outcome.Method);
        Assert.Equal("material measurement drift", outcome.RevisionTrigger);
    }
}
