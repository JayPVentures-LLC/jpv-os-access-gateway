using JPVOS.Services.SystemicAccess;

namespace JPVOS.Tests;

public sealed class SystemicAccessReconcilerTests
{
    [Fact]
    public async Task RunOnceAsync_AppliesOnlyActionableDecisionsAndAuditsAllRecords()
    {
        var records = new[]
        {
            new SystemicAccessRecord("entitlement", "valid", "active", false, false, false, false, false, false, false, "test"),
            new SystemicAccessRecord("entitlement", "expired", "expired", false, false, false, false, false, false, false, "test"),
            new SystemicAccessRecord("entitlement", "uncertain", "active", false, false, false, false, false, true, false, "test")
        };
        var source = new FakeSource(records);
        var provider = new FakeProvider();
        var auditPath = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        var audit = new SystemicAccessAuditStore(auditPath);
        var reconciler = new SystemicAccessReconciler(new[] { source }, new[] { provider }, new SystemicAccessClassifier(), audit);

        try
        {
            var summary = await reconciler.RunOnceAsync(CancellationToken.None);
            Assert.Equal(3, summary.Evaluated);
            Assert.Equal(1, summary.ActionsApplied);
            Assert.Single(provider.Applied);
            Assert.Equal("EXPIRE", provider.Applied[0].Decision.Action);
            Assert.Equal(3, File.ReadAllLines(auditPath).Length);
        }
        finally
        {
            if (File.Exists(auditPath)) File.Delete(auditPath);
        }
    }

    private sealed class FakeSource(IReadOnlyCollection<SystemicAccessRecord> records) : ISystemicAccessInventorySource
    {
        public Task<IReadOnlyCollection<SystemicAccessRecord>> GetRecordsAsync(CancellationToken cancellationToken) => Task.FromResult(records);
    }

    private sealed class FakeProvider : ISystemicAccessActionProvider
    {
        public List<(SystemicAccessRecord Record, SystemicAccessDecision Decision)> Applied { get; } = [];
        public bool CanHandle(SystemicAccessRecord record, SystemicAccessDecision decision) => decision.Action is "REVOKE" or "EXPIRE" or "ROTATE" or "DEDUPLICATE";
        public Task<SystemicAccessActionResult> ApplyAsync(SystemicAccessRecord record, SystemicAccessDecision decision, CancellationToken cancellationToken)
        {
            Applied.Add((record, decision));
            return Task.FromResult(new SystemicAccessActionResult(true, decision.Action));
        }
    }
}
