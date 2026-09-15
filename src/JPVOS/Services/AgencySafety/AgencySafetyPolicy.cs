using System.Text.Json;

namespace JPVOS.Services.AgencySafety;

public sealed record AgencySafetyPolicy(
    string PolicyId,
    string Mode,
    bool DenyWins,
    bool AllowWildcardScope,
    bool AllowAuthorityInheritance,
    bool AllowSelfCertification,
    bool AllowShutdownBypass,
    bool PersistenceProvenanceRequired,
    bool ConsequentialIndependentVerificationRequired,
    string[] AuthorityLevels);

public static class AgencySafetyPolicyLoader
{
    public static AgencySafetyPolicy LoadAndValidate(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("AI agency safety policy is required; startup fails closed.");
        var p = JsonSerializer.Deserialize<AgencySafetyPolicy>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("AI agency safety policy is malformed.");
        if (p.PolicyId != "JPV-GOV-AI-AGENCY-SAFETY-001" || p.Mode != "fail-closed" || !p.DenyWins || p.AllowWildcardScope || p.AllowAuthorityInheritance || p.AllowSelfCertification || p.AllowShutdownBypass || !p.PersistenceProvenanceRequired || !p.ConsequentialIndependentVerificationRequired || p.AuthorityLevels is null || p.AuthorityLevels.Length != 7)
            throw new InvalidOperationException("AI agency safety policy weakens canonical invariants.");
        return p;
    }
}

public sealed record AgencyActionRequest(string PrincipalId,string AuthorizationId,string AuthorityLevel,string Scope,string[] Capabilities,int CredentialTtlSeconds,string NetworkScope,string PersistenceScope,string? PersistenceProvenance,string AgentCommunicationScope,bool Revoked,bool ShutdownRequested,string? IndependentVerifier,bool TamperEvidentReceipt);
public sealed record AgencySafetyDecision(bool Allowed,string Reason);

public sealed class AgencySafetyAuthorizer(AgencySafetyPolicy policy)
{
    public AgencySafetyDecision Authorize(AgencyActionRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.PrincipalId) || string.IsNullOrWhiteSpace(r.AuthorizationId)) return Deny("missing_authority");
        var level = Array.IndexOf(policy.AuthorityLevels, r.AuthorityLevel); if (level < 0) return Deny("authority_level_exceeded");
        if (r.Scope == "*" || r.NetworkScope == "*" || r.PersistenceScope == "*") return Deny("unbounded_scope");
        if (r.Capabilities is null || r.Capabilities.Length == 0 || r.CredentialTtlSeconds <= 0) return Deny("capability_or_credential_invalid");
        if (r.AgentCommunicationScope == "inherit-authority") return Deny("implicit_agent_authority");
        if (r.PersistenceScope != "none" && string.IsNullOrWhiteSpace(r.PersistenceProvenance)) return Deny("missing_persistence_provenance");
        if (r.Revoked || r.ShutdownRequested) return Deny("shutdown_or_revocation_bypass");
        var consequential = Array.IndexOf(policy.AuthorityLevels, "EXECUTE_CONSEQUENTIAL");
        if (level >= consequential && (string.IsNullOrWhiteSpace(r.IndependentVerifier) || r.IndependentVerifier == r.PrincipalId || !r.TamperEvidentReceipt)) return Deny("verification_unavailable");
        return new(true,"allow");
    }
    private static AgencySafetyDecision Deny(string reason) => new(false, reason);
}
