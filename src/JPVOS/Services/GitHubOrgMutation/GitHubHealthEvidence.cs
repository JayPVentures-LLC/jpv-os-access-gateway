using System.Security.Cryptography;
using System.Text;

namespace JPVOS.Services.GitHubOrgMutation;

public sealed record GitHubHealthEvidence(
    string HealthAuthority,
    string TopologyAuthority,
    string ProviderRole,
    string HealthClass,
    string Subject,
    string Status,
    string ReasonCode,
    string DependencyFingerprint,
    string DeduplicationKey,
    IReadOnlyList<string> EvidenceReferences,
    DateTimeOffset FirstObservedAt,
    DateTimeOffset LastObservedAt,
    string AccountableRoute,
    bool ReleaseBlocking,
    string NextReevaluationTrigger)
{
    public static GitHubHealthEvidence FromReconciliation(GitHubOrgReconciliationResult result, string topologyVersion)
    {
        var (status, reasonCode, releaseBlocking) = result.State switch
        {
            GitHubOrgReconciliationState.Verified => ("PASS", "PROVIDER_READBACK_VERIFIED", false),
            GitHubOrgReconciliationState.Drifted => ("DRIFTED", "PROVIDER_STATE_DRIFT", true),
            GitHubOrgReconciliationState.BlockedByProviderCapability => ("BLOCKED", "PROVIDER_MUTATION_UNSUPPORTED", true),
            GitHubOrgReconciliationState.AuthorityRequired => ("BLOCKED", "PROVIDER_ORG_PERMISSION_MISSING", true),
            GitHubOrgReconciliationState.AccessReconciliationRequired => ("DRIFTED", "PROVIDER_STATE_DRIFT", true),
            _ => ("UNKNOWN", "PROVIDER_RULESET_READBACK_UNAVAILABLE", true)
        };

        const string healthClass = "PROVIDER_AUTHORITY_HEALTH";
        var subject = result.Organization;
        var dependencyMaterial = string.Join('|',
            topologyVersion,
            result.Organization,
            result.State,
            string.Join(',', result.RemainingMutations.Select(x => $"{x.Kind}:{x.TeamSlug}:{x.ParentSlug}")));
        var fingerprint = Sha256(dependencyMaterial);
        var deduplicationKey = string.Join('|', healthClass, subject, status, reasonCode, fingerprint);

        return new GitHubHealthEvidence(
            "JayPVentures-LLC/jpv-governance",
            "jaypVLabs/JPV-OS",
            "PROVIDER_EVIDENCE_READBACK_ONLY",
            healthClass,
            subject,
            status,
            reasonCode,
            fingerprint,
            deduplicationKey,
            [result.ReceiptId, $"topology:{topologyVersion}"],
            result.CompletedAtUtc,
            result.CompletedAtUtc,
            "JayPVentures-LLC/jpv-governance#230",
            releaseBlocking,
            status is "BLOCKED" or "UNKNOWN" ? "DEPENDENCY_FINGERPRINT_CHANGE_OR_BOUNDED_EVIDENCE_REFRESH" : "NEXT_GOVERNED_RECONCILIATION");
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
