using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalBootstrapImporterTests : IDisposable
{
    private readonly string _directory = Path.Join(Path.GetTempPath(), $"jpv-proposal-bootstrap-{Guid.NewGuid():N}");

    [Fact]
    public async Task Importer_registers_safe_initial_states_idempotently_without_regression()
    {
        Directory.CreateDirectory(_directory);
        var dbPath = Path.Join(_directory, "registry.db");
        var manifestPath = Path.Join(_directory, "bootstrap.json");
        await File.WriteAllTextAsync(manifestPath, """
        {
          "schema_version": "1.0.0",
          "records": [
            { "proposal_id": "JPV-BOOT-1", "title": "Draft proposal", "class": "GrantProposal", "lane": "Enterprise", "status": "Draft", "truth_note": "draft only" },
            { "proposal_id": "JPV-BOOT-2", "title": "Validated proposal", "class": "GovernanceStandard", "lane": "Enterprise", "status": "Validated", "truth_note": "validated only" }
          ]
        }
        """);

        var service = new ProposalRegistryService(new SqliteProposalEventStore(dbPath), new ProposalLifecycleValidator());
        var importer = new ProposalBootstrapImporter(service);

        await importer.ImportAsync(manifestPath, default);
        await importer.ImportAsync(manifestPath, default);
        await service.RecordStatusAsync("JPV-BOOT-1", ProposalStatus.ApprovedInternally, null, null, "advance", default);
        await importer.ImportAsync(manifestPath, default);

        Assert.Equal(ProposalStatus.ApprovedInternally, (await service.GetAsync("JPV-BOOT-1", default))!.Status);
        Assert.Equal(ProposalStatus.Validated, (await service.GetAsync("JPV-BOOT-2", default))!.Status);
    }

    [Fact]
    public async Task Importer_rejects_bootstrap_states_that_assert_external_progress()
    {
        Directory.CreateDirectory(_directory);
        var dbPath = Path.Join(_directory, "registry.db");
        var manifestPath = Path.Join(_directory, "unsafe.json");
        await File.WriteAllTextAsync(manifestPath, """
        { "schema_version": "1.0.0", "records": [
          { "proposal_id": "JPV-BOOT-3", "title": "Unsafe", "class": "GrantProposal", "lane": "Enterprise", "status": "Adopted", "truth_note": "unsupported" }
        ] }
        """);

        var service = new ProposalRegistryService(new SqliteProposalEventStore(dbPath), new ProposalLifecycleValidator());
        var importer = new ProposalBootstrapImporter(service);

        await Assert.ThrowsAsync<ProposalValidationException>(() => importer.ImportAsync(manifestPath, default));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
