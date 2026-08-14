using JPVOS.Services.SystemicAccess;

namespace JPVOS.Tests;

public sealed class SystemicAccessClassifierTests
{
    [Theory]
    [InlineData("active", false, false, false, false, false, false, "VALID")]
    [InlineData("expired", false, false, false, false, false, false, "EXPIRE")]
    [InlineData("revoked", false, false, false, false, false, false, "REVOKE")]
    [InlineData("active", true, false, false, false, false, false, "DEDUPLICATE")]
    [InlineData("active", false, true, false, false, false, false, "ROTATE")]
    [InlineData("active", false, false, true, false, false, false, "QUARANTINE")]
    [InlineData("active", false, false, false, true, false, false, "QUARANTINE")]
    [InlineData("active", false, false, false, false, true, false, "QUARANTINE")]
    [InlineData("active", false, false, false, false, false, true, "REVIEW")]
    public void Classify_IsDeterministic(
        string status,
        bool duplicate,
        bool compromised,
        bool stale,
        bool orphaned,
        bool unowned,
        bool uncertain,
        string expected)
    {
        var record = new SystemicAccessRecord(
            ResourceType: "entitlement",
            ResourceId: "opaque-id",
            Status: status,
            IsDuplicate: duplicate,
            IsCompromised: compromised,
            IsStale: stale,
            IsOrphaned: orphaned,
            IsUnowned: unowned,
            IsUncertain: uncertain,
            IsFounderProtected: false,
            Evidence: "test");

        var decision = new SystemicAccessClassifier().Classify(record);
        Assert.Equal(expected, decision.Action);
    }

    [Fact]
    public void Classify_AlwaysProtectsFounderBreakGlassState()
    {
        var record = new SystemicAccessRecord("identity", "founder", "revoked", false, true, true, true, true, false, true, "test");
        var decision = new SystemicAccessClassifier().Classify(record);
        Assert.Equal("REVIEW", decision.Action);
    }
}
