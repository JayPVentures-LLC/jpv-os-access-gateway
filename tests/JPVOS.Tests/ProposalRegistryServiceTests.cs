using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalRegistryServiceTests : IDisposable
{
    private readonly string _path = Path.Join(Path.GetTempPath(), $"jpv-registry-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Registry_rejects_status_without_required_record()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-1", "Registry", ProposalClass.GrantProposal, ProposalLane.Enterprise, "r1", default);
        await Assert.ThrowsAsync<ProposalValidationException>(() => service.RecordStatusAsync("JPV-REG-1", ProposalStatus.Submitted, null, null, "s1", default));
    }

    [Fact]
    public async Task Registry_records_evidenced_status_and_replays_it()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-2", "Registry", ProposalClass.ResearchCollaboration, ProposalLane.Labs, "r2", default);
        await service.LinkEvidenceAsync("JPV-REG-2", new ClassifiedEvidenceReference("record-2", ProposalEvidenceClass.SourceRecord), "e2", default);
        await service.RecordStatusAsync("JPV-REG-2", ProposalStatus.Submitted, "record-2", null, "s2", default);
        var projection = await service.GetAsync("JPV-REG-2", default);
        Assert.Equal(ProposalStatus.Submitted, projection!.Status);
        Assert.Contains("record-2", projection.EvidenceReferences);
    }

    [Fact]
    public async Task Retry_is_idempotent_after_lifecycle_has_advanced()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-RETRY", "Retry", ProposalClass.GrantProposal, ProposalLane.Enterprise, "r-retry", default);
        await service.LinkEvidenceAsync("JPV-REG-RETRY", new ClassifiedEvidenceReference("submission-1", ProposalEvidenceClass.SourceRecord), "submission-evidence", default);
        await service.LinkEvidenceAsync("JPV-REG-RETRY", new ClassifiedEvidenceReference("ack-1", ProposalEvidenceClass.LaterDisposition), "ack-evidence", default);
        await service.RecordStatusAsync("JPV-REG-RETRY", ProposalStatus.Submitted, "submission-1", null, "submit-key", default);
        await service.RecordStatusAsync("JPV-REG-RETRY", ProposalStatus.Acknowledged, "ack-1", null, "ack-key", default);
        await service.RecordStatusAsync("JPV-REG-RETRY", ProposalStatus.UnderReview, null, null, "review-key", default);

        var replay = await service.RecordStatusAsync("JPV-REG-RETRY", ProposalStatus.Submitted, "submission-1", null, "submit-key", default);

        Assert.Equal(ProposalStatus.UnderReview, replay.Status);
    }

    [Fact]
    public async Task Registry_rejects_completed_obligation_without_completion_evidence()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-OBL", "Obligation", ProposalClass.OperationalStandard, ProposalLane.Enterprise, "r-obl", default);
        await Assert.ThrowsAsync<ProposalValidationException>(() => service.AddObligationAsync(
            "JPV-REG-OBL",
            new ImplementationObligation("obl-1", "operator", "Execute", "receipt", true, null),
            "obl-key", default));
    }

    [Fact]
    public async Task Registry_requires_measurement_evidence_for_measured_outcomes()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-OUT", "Outcome", ProposalClass.PublicInterestFramework, ProposalLane.Labs, "r-out", default);
        await Assert.ThrowsAsync<ProposalValidationException>(() => service.RecordOutcomeAsync(
            "JPV-REG-OUT", new OutcomeMeasurement("adoption", "met", null), "out-1", default));
        await Assert.ThrowsAsync<ProposalValidationException>(() => service.RecordOutcomeAsync(
            "JPV-REG-OUT", new OutcomeMeasurement("adoption", "MET", null), "out-uppercase", default));

        var recorded = await service.RecordOutcomeAsync(
            "JPV-REG-OUT",
            new OutcomeMeasurement("adoption", "MET", "measurement-1", Method: "ledger", DataSource: "proposal-ledger", ObservationWindow: "90 days", ReviewOwner: "reviewer"),
            "out-2", default);
        Assert.Equal("met", recorded.LatestOutcome!.Disposition);
    }

    [Fact]
    public async Task Registry_blocks_release_until_required_independent_review_is_complete()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-REL", "Release", ProposalClass.PublicPolicy, ProposalLane.Enterprise, "r-rel", default);
        await service.MapAuthorityAsync("JPV-REG-REL", new AuthorityAssignment("proposal-owner", "owner-1", null), "owner-map", default);

        await Assert.ThrowsAsync<ProposalValidationException>(() => service.RecordReleaseAsync("JPV-REG-REL", true, "Public", "release-missing-review", default));

        await service.SetReviewRequirementAsync("JPV-REG-REL", new ReviewRequirement("before-publication", true, "independent review"), "review-required", default);
        await Assert.ThrowsAsync<ProposalValidationException>(() => service.RecordReleaseAsync("JPV-REG-REL", true, "Public", "release-blocked", default));

        await service.SetReviewRequirementAsync("JPV-REG-REL", new ReviewRequirement("before-publication", true, "independent review", "reviewer-1", true, "review-evidence-1"), "review-complete", default);
        var released = await service.RecordReleaseAsync("JPV-REG-REL", true, "Public", "release-ok", default);
        Assert.True(released.PublicReleaseApproved);
    }

    [Fact]
    public async Task Registry_requires_actor_and_action_for_implementation_obligations()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-OBL-FIELDS", "Obligation fields", ProposalClass.OperationalStandard, ProposalLane.Enterprise, "r", default);
        await Assert.ThrowsAsync<ProposalValidationException>(() => service.AddObligationAsync(
            "JPV-REG-OBL-FIELDS",
            new ImplementationObligation("obl-1", "", "", "receipt", false, null),
            "o", default));
    }

    [Fact]
    public async Task Registry_records_all_governance_contracts()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-REG-3", "Governed", ProposalClass.PublicInterestFramework, ProposalLane.Labs, "r3", default);
        await service.LinkEvidenceAsync("JPV-REG-3", new ClassifiedEvidenceReference("record-source", ProposalEvidenceClass.SourceRecord), "e1", default);
        await service.MapAuthorityAsync("JPV-REG-3", new AuthorityAssignment("review-authority", "reviewer-1", "record-a"), "a1", default);
        await service.SetAdoptionRouteAsync("JPV-REG-3", new AdoptionRoute("institution-1", "official-route", ["proposal.pdf"], null, "receipt", "review", "alternate", null, "submission receipt"), "route1", default);
        await service.AddObligationAsync("JPV-REG-3", new ImplementationObligation("obl-1", "operator-1", "Publish verification record", "merge receipt", false, null), "o1", default);
        await service.AddVerificationRequirementAsync("JPV-REG-3", new VerificationRequirement("verify-1", "exact-head", "merge SHA"), "v1", default);
        await service.SetReviewRequirementAsync("JPV-REG-3", new ReviewRequirement("before-publication", true, "independent review"), "review1", default);
        await service.RecordOutcomeAsync("JPV-REG-3", new OutcomeMeasurement("adoption-rate", "not-yet-measurable", null), "m1", default);
        await service.AddLineageAsync("JPV-REG-3", new ProposalLineage("derives-from", "artifact-v1"), "l1", default);

        var projection = await service.GetAsync("JPV-REG-3", default);
        Assert.Single(projection!.ClassifiedEvidence);
        Assert.Single(projection.Authorities);
        Assert.NotNull(projection.AdoptionRoute);
        Assert.Single(projection.Obligations);
        Assert.Single(projection.VerificationRequirements);
        Assert.NotNull(projection.ReviewRequirement);
        Assert.NotNull(projection.LatestOutcome);
        Assert.Single(projection.Lineage);
    }

    private ProposalRegistryService CreateService() => new(new SqliteProposalEventStore(_path), new ProposalLifecycleValidator());

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
