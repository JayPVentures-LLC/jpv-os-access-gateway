using JPVOS.Services.GitHubOrgMutation;

namespace JPVOS.Tests;

public sealed class GitHubHealthEvidenceTests
{
    [Fact]
    public void Verified_provider_readback_maps_to_provider_authority_pass()
    {
        var result = new GitHubOrgReconciliationResult(
            "receipt-1",
            "JayPVentures-LLC",
            GitHubOrgReconciliationState.Verified,
            2,
            [],
            DateTimeOffset.Parse("2026-08-18T22:00:00Z"));

        var evidence = GitHubHealthEvidence.FromReconciliation(result, "topology-v1");

        Assert.Equal("PROVIDER_AUTHORITY_HEALTH", evidence.HealthClass);
        Assert.Equal("PASS", evidence.Status);
        Assert.Equal("PROVIDER_READBACK_VERIFIED", evidence.ReasonCode);
        Assert.False(evidence.ReleaseBlocking);
        Assert.NotEmpty(evidence.DependencyFingerprint);
        Assert.Equal("JayPVentures-LLC/jpv-governance#230", evidence.AccountableRoute);
    }

    [Fact]
    public void Drifted_provider_readback_maps_to_drifted_and_release_blocking()
    {
        var result = new GitHubOrgReconciliationResult(
            "receipt-2",
            "JayPVentures-LLC",
            GitHubOrgReconciliationState.Drifted,
            1,
            [new GitHubTeamMutation(GitHubTeamMutationKind.SetParent, "security", "enterprise")],
            DateTimeOffset.Parse("2026-08-18T22:00:00Z"));

        var evidence = GitHubHealthEvidence.FromReconciliation(result, "topology-v1");

        Assert.Equal("DRIFTED", evidence.Status);
        Assert.Equal("PROVIDER_STATE_DRIFT", evidence.ReasonCode);
        Assert.True(evidence.ReleaseBlocking);
    }

    [Fact]
    public void Dependency_fingerprint_is_stable_for_equivalent_evidence()
    {
        var completedAt = DateTimeOffset.Parse("2026-08-18T22:00:00Z");
        var first = new GitHubOrgReconciliationResult("a", "jaypVLabs", GitHubOrgReconciliationState.Drifted, 0, [], completedAt);
        var second = new GitHubOrgReconciliationResult("b", "jaypVLabs", GitHubOrgReconciliationState.Drifted, 0, [], completedAt.AddMinutes(5));

        var firstEvidence = GitHubHealthEvidence.FromReconciliation(first, "topology-v1");
        var secondEvidence = GitHubHealthEvidence.FromReconciliation(second, "topology-v1");

        Assert.Equal(firstEvidence.DependencyFingerprint, secondEvidence.DependencyFingerprint);
        Assert.Equal(firstEvidence.DeduplicationKey, secondEvidence.DeduplicationKey);
    }

    [Fact]
    public void Provider_evidence_identifies_enterprise_health_authority_separately_from_topology_authority()
    {
        var result = new GitHubOrgReconciliationResult("receipt-3", "JayPVentures-LLC", GitHubOrgReconciliationState.Verified, 0, [], DateTimeOffset.UtcNow);

        var evidence = GitHubHealthEvidence.FromReconciliation(result, "topology-v1");

        Assert.Equal("JayPVentures-LLC/jpv-governance", evidence.HealthAuthority);
        Assert.Equal("jaypVLabs/JPV-OS", evidence.TopologyAuthority);
        Assert.Equal("PROVIDER_EVIDENCE_READBACK_ONLY", evidence.ProviderRole);
    }
}
