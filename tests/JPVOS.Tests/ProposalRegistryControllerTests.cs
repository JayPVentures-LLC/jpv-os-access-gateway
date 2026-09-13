using JPVOS.Api;
using JPVOS.Services.ProposalExecution;
using Microsoft.AspNetCore.Mvc;

namespace JPVOS.Tests;

public sealed class ProposalRegistryControllerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"jpv-proposal-api-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Status_is_hidden_until_release_review_is_approved()
    {
        var service = new ProposalRegistryService(new SqliteProposalEventStore(_path), new ProposalLifecycleValidator());
        await service.RegisterAsync("JPV-API-1", "Private proposal", ProposalClass.GovernanceStandard, ProposalLane.Enterprise, "r1", default);
        var controller = new ProposalRegistryController(service);

        Assert.IsType<NotFoundResult>(await controller.GetStatus("JPV-API-1", default));

        await service.RecordReleaseAsync("JPV-API-1", true, "Public summary", "release-1", default);
        var ok = Assert.IsType<OkObjectResult>(await controller.GetStatus("JPV-API-1", default));
        var body = Assert.IsType<ProposalRegistryController.PublicProposalStatus>(ok.Value);
        Assert.Equal("Public summary", body.Summary);
        Assert.DoesNotContain("Evidence", string.Join(',', body.GetType().GetProperties().Select(x => x.Name)), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
