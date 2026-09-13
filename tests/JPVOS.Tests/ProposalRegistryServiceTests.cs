using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalRegistryServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"jpv-registry-{Guid.NewGuid():N}.db");

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
        await service.RecordStatusAsync("JPV-REG-2", ProposalStatus.Submitted, "record-2", null, "s2", default);
        var projection = await service.GetAsync("JPV-REG-2", default);
        Assert.Equal(ProposalStatus.Submitted, projection!.Status);
        Assert.Contains("record-2", projection.EvidenceReferences);
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
