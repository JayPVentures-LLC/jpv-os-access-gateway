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
    bool ThirdPartyAuthorizationDenialSticky,
    bool AlternateRouteDoesNotCreateAuthority,
    bool SecurityTestingRequiresScopedAttributableAuthorization,
    bool AuthoritativeDenialStateRequired,
    string[] AuthorityLevels);

public static class AgencySafetyPolicyLoader
{
    public static AgencySafetyPolicy LoadAndValidate(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("AI agency safety policy is required; startup fails closed.");
        var p = JsonSerializer.Deserialize<AgencySafetyPolicy>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("AI agency safety policy is malformed.");
        if (p.PolicyId != "JPV-GOV-AI-AGENCY-SAFETY-001" || p.Mode != "fail-closed" || !p.DenyWins ||
            p.AllowWildcardScope || p.AllowAuthorityInheritance || p.AllowSelfCertification || p.AllowShutdownBypass ||
            !p.PersistenceProvenanceRequired || !p.ConsequentialIndependentVerificationRequired ||
            !p.ThirdPartyAuthorizationDenialSticky || !p.AlternateRouteDoesNotCreateAuthority ||
            !p.SecurityTestingRequiresScopedAttributableAuthorization || !p.AuthoritativeDenialStateRequired ||
            p.AuthorityLevels is null || p.AuthorityLevels.Length != 7)
            throw new InvalidOperationException("AI agency safety policy weakens canonical invariants.");
        return p;
    }
}

public sealed record AgencyTargetDenial(string TargetResourceId, string EvidenceId, DateTimeOffset DeniedAtUtc);

public interface IAgencyDenialStateStore
{
    AgencyTargetDenial? Get(string targetResourceId);
    void Record(string targetResourceId, string evidenceId);
}

public sealed class FileAgencyDenialStateStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public FileAgencyDenialStateStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
    }

    public AgencyTargetDenial? Get(string targetResourceId)
    {
        lock (_gate)
        {
            return Load().FirstOrDefault(x => string.Equals(x.TargetResourceId, targetResourceId, StringComparison.Ordinal));
        }
    }

    public void Record(string targetResourceId, string evidenceId)
    {
        if (string.IsNullOrWhiteSpace(targetResourceId) || string.IsNullOrWhiteSpace(evidenceId))
            throw new ArgumentException("Target resource and evidence id are required.");
        lock (_gate)
        {
            var items = Load();
            if (items.All(x => !string.Equals(x.TargetResourceId, targetResourceId, StringComparison.Ordinal)))
                items.Add(new(targetResourceId, evidenceId, DateTimeOffset.UtcNow));
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(items));
            File.Move(tmp, _path, true);
        }
    }

    private List<AgencyTargetDenial> Load()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<AgencyTargetDenial>>(File.ReadAllText(_path)) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Agency denial state is malformed; fail closed.", ex);
        }
    }
}

public sealed record AgencyActionRequest(
    string PrincipalId,
    string AuthorizationId,
    string AuthorityLevel,
    string Scope,
    string[] Capabilities,
    int CredentialTtlSeconds,
    string NetworkScope,
    string PersistenceScope,
    string? PersistenceProvenance,
    string AgentCommunicationScope,
    bool Revoked,
    bool ShutdownRequested,
    string? IndependentVerifier,
    bool TamperEvidentReceipt,
    string TargetResourceId,
    bool SecurityTestingAuthorization,
    string? SecurityTestingAuthorizationId,
    string? SecurityTestingTargetScope,
    string? SecurityTestingMethodScope,
    DateTimeOffset? SecurityTestingValidUntil);

public sealed record AgencySafetyDecision(bool Allowed, string Reason);

public sealed class AgencySafetyAuthorizer(AgencySafetyPolicy policy, FileAgencyDenialStateStore denialState)
{
    public void RecordAuthoritativeTargetDenial(string targetResourceId, string evidenceId) =>
        denialState.Record(targetResourceId, evidenceId);

    public AgencySafetyDecision Authorize(AgencyActionRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.PrincipalId) || string.IsNullOrWhiteSpace(r.AuthorizationId)) return Deny("missing_authority");
        if (string.IsNullOrWhiteSpace(r.TargetResourceId)) return Deny("missing_target_resource");
        var level = Array.IndexOf(policy.AuthorityLevels, r.AuthorityLevel); if (level < 0) return Deny("authority_level_exceeded");
        if (r.Scope == "*" || r.NetworkScope == "*" || r.PersistenceScope == "*") return Deny("unbounded_scope");
        if (r.Capabilities is null || r.Capabilities.Length == 0 || r.CredentialTtlSeconds <= 0) return Deny("capability_or_credential_invalid");
        if (r.AgentCommunicationScope == "inherit-authority") return Deny("implicit_agent_authority");
        if (r.PersistenceScope != "none" && string.IsNullOrWhiteSpace(r.PersistenceProvenance)) return Deny("missing_persistence_provenance");
        if (r.Revoked || r.ShutdownRequested) return Deny("shutdown_or_revocation_bypass");

        AgencyTargetDenial? priorDenial;
        try { priorDenial = denialState.Get(r.TargetResourceId); }
        catch { return Deny("authoritative_denial_state_unavailable"); }

        if (priorDenial is not null)
        {
            if (!r.SecurityTestingAuthorization) return Deny("third_party_authorization_denial_circumvention");
            if (string.IsNullOrWhiteSpace(r.SecurityTestingAuthorizationId) || r.SecurityTestingAuthorizationId == r.AuthorizationId)
                return Deny("third_party_authorization_denial_circumvention");
            if (!string.Equals(r.SecurityTestingTargetScope, r.TargetResourceId, StringComparison.Ordinal))
                return Deny("third_party_authorization_denial_circumvention");
            if (string.IsNullOrWhiteSpace(r.SecurityTestingMethodScope) || r.SecurityTestingValidUntil is null ||
                r.SecurityTestingValidUntil <= DateTimeOffset.UtcNow)
                return Deny("third_party_authorization_denial_circumvention");
        }

        var consequential = Array.IndexOf(policy.AuthorityLevels, "EXECUTE_CONSEQUENTIAL");
        if (level >= consequential && (string.IsNullOrWhiteSpace(r.IndependentVerifier) || r.IndependentVerifier == r.PrincipalId || !r.TamperEvidentReceipt))
            return Deny("verification_unavailable");
        return new(true, "allow");
    }

    private static AgencySafetyDecision Deny(string reason) => new(false, reason);
}
