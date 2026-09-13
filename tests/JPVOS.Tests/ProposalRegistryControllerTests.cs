using JPVOS.Api;
using JPVOS.Services.ProposalExecution;
using Microsoft.AspNetCore.Mvc;

namespace JPVOS.Tests;

public sealed class ProposalRegistryControllerTests : IDisposable
{
    private readonly string _path = Path.Join(Path.GetTempPath(), $"jpv-proposal-api-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Status_is_hidden_until_release_review_is_approved()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-API-1", "Private proposal", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "r1", default);
        var controller = new ProposalRegistryController(service);

        Assert.IsType<NotFoundResult>(await controller.GetStatus("JPV-API-1", default));

        await service.RecordReleaseAsync("JPV-API-1", true, "Public summary", "release-1", default);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetStatus("JPV-API-1", default));
        var body = Assert.IsType<ProposalRegistryController.PublicProposalStatus>(ok.Value);
        Assert.Equal("Public summary", body.Summary);
        Assert.Equal("researching", body.Status);
        Assert.DoesNotContain("Evidence", string.Join(",", body.GetType().GetProperties().Select(x => x.Name)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Multiword_status_uses_canonical_kebab_case()
    {
        var service = CreateService();
        await service.RegisterAsync("JPV-API-2", "Wire values", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "r2", default);
        await service.RecordStatusAsync("JPV-API-2", ProposalStatus.Draft, null, null, "draft", default);
        await service.RecordStatusAsync("JPV-API-2", ProposalStatus.ApprovedInternally, null, null, "approved", default);
        await service.RecordReleaseAsync("JPV-API-2", true, "Released", "release-2", default);
        var controller = new ProposalRegistryController(service);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetStatus("JPV-API-2", default));
        var body = Assert.IsType<ProposalRegistryController.PublicProposalStatus>(ok.Value);
        Assert.Equal("approved-internally", body.Status);
    }

    private ProposalRegistryService CreateService() => new(new SqliteProposalEventStore(_path), new ProposalLifecycleValidator());

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
