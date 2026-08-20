namespace JPVOS.Services.PrivilegedActions;

public sealed class PrivilegedActionAuthorizer
{
    private readonly PrivilegedActionPolicy _policy;
    private static readonly TimeSpan DefaultPrivilegedMaxAge = TimeSpan.FromMinutes(5);

    public PrivilegedActionAuthorizer(PrivilegedActionPolicy policy)
    {
        _policy = policy;
    }

    public PrivilegedActionDecision Authorize(
        PrivilegedActionRequest request,
        AuthenticationEvidence authentication,
        DateTimeOffset nowUtc,
        BreakGlassGrant? breakGlassGrant = null)
    {
        if (!authentication.IdentityVerified)
            return Deny(request, "IDENTITY_NOT_VERIFIED");

        if (request.EntitlementAmbiguous)
            return new(PrivilegedDecisionKind.Review, "ENTITLEMENT_AMBIGUOUS", request.RiskClass);

        if (!request.EntitlementValid)
            return Deny(request, "ENTITLEMENT_INVALID");

        if (request.RiskClass == PrivilegedRiskClass.Routine)
            return new(PrivilegedDecisionKind.Allow, "ROUTINE_AUTHORIZED", request.RiskClass);

        var privilegedByAction = _policy.PrivilegedActions.Contains(request.Action, StringComparer.OrdinalIgnoreCase);
        var requiresPhishingResistant = privilegedByAction || request.RiskClass is PrivilegedRiskClass.Privileged or PrivilegedRiskClass.BreakGlass;

        var allowedAge = request.RiskClass is PrivilegedRiskClass.Privileged or PrivilegedRiskClass.BreakGlass
            ? TimeSpan.FromTicks(Math.Min(authentication.MaxAge.Ticks, DefaultPrivilegedMaxAge.Ticks))
            : authentication.MaxAge;

        if (nowUtc - authentication.AuthenticatedAtUtc > allowedAge)
            return Deny(request, "STEP_UP_EXPIRED");

        if (requiresPhishingResistant && !authentication.PhishingResistant)
            return Deny(request, authentication.VoiceSignalPresent ? "VOICE_ONLY_INSUFFICIENT" : "PHISHING_RESISTANT_STEP_UP_REQUIRED");

        if (request.RiskClass == PrivilegedRiskClass.BreakGlass)
        {
            if (breakGlassGrant is null)
                return new(PrivilegedDecisionKind.BreakGlassRequired, "BREAK_GLASS_GRANT_REQUIRED", request.RiskClass);

            if (!breakGlassGrant.IsActive(nowUtc) ||
                !string.Equals(breakGlassGrant.ActorSubject, request.ActorSubject, StringComparison.Ordinal) ||
                !string.Equals(breakGlassGrant.Scope, request.Resource, StringComparison.Ordinal))
                return Deny(request, "BREAK_GLASS_GRANT_INVALID");
        }

        return new(PrivilegedDecisionKind.Allow, "AUTHORIZED", request.RiskClass);
    }

    private static PrivilegedActionDecision Deny(PrivilegedActionRequest request, string reason) =>
        new(PrivilegedDecisionKind.Deny, reason, request.RiskClass);
}

public sealed class BreakGlassAuthorizationService
{
    private readonly PrivilegedActionPolicy _policy;

    public BreakGlassAuthorizationService(PrivilegedActionPolicy policy)
    {
        _policy = policy;
    }

    public BreakGlassGrant Issue(
        string actorSubject,
        string reason,
        string scope,
        TimeSpan ttl,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(actorSubject))
            throw new ArgumentException("Actor subject is required.", nameof(actorSubject));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Break-glass reason is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Break-glass scope is required.", nameof(scope));

        var max = TimeSpan.FromMinutes(_policy.Invariants.BreakGlassMaxTtlMinutes);
        if (ttl <= TimeSpan.Zero || ttl > max)
            throw new InvalidOperationException($"Break-glass TTL must be greater than zero and no more than {max.TotalMinutes:0} minutes.");

        return new BreakGlassGrant(
            Guid.NewGuid().ToString("N"),
            actorSubject,
            reason.Trim(),
            scope.Trim(),
            nowUtc,
            nowUtc.Add(ttl));
    }
}
