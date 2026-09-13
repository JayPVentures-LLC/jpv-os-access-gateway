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

        await Assert.ThrowsAsync<ProposalValidationException>(() =>
            service.RecordStatusAsync("JPV-REG-1", ProposalStatus.Submitted, null, null, "s1", default));
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

    private ProposalRegistryService CreateService() => new(new SqliteProposalEventStore(_path), new ProposalLifecycleValidator());

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
