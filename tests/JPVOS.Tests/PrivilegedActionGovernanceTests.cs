using JPVOS.Services.PrivilegedActions;

namespace JPVOS.Tests;

public sealed class PrivilegedActionGovernanceTests
{
    private static PrivilegedActionPolicy Policy() => new()
    {
        Id = "JPV-GOV-PRIVILEGED-ACTION-001",
        RiskClasses = ["ROUTINE", "ELEVATED", "PRIVILEGED", "BREAK_GLASS"],
        PrivilegedActions = ["credential_change", "break_glass_activation"],
        Invariants = new PrivilegedActionInvariants
        {
            PhishingResistantStepUpRequired = true,
            VoiceOnlyPermitted = false,
            ProviderReadbackRequired = true,
            UnknownStateDecision = "BLOCK",
            BreakGlassMaxTtlMinutes = 30,
            DurableReceiptRequired = true
        }
    };

    [Fact]
    public void Policy_rejects_voice_only_authorization()
    {
        var policy = Policy();
        var weakened = new PrivilegedActionPolicy
        {
            Id = policy.Id,
            RiskClasses = policy.RiskClasses,
            PrivilegedActions = policy.PrivilegedActions,
            Invariants = new PrivilegedActionInvariants
            {
                PhishingResistantStepUpRequired = true,
                VoiceOnlyPermitted = true,
                ProviderReadbackRequired = true,
                UnknownStateDecision = "BLOCK",
                BreakGlassMaxTtlMinutes = 30,
                DurableReceiptRequired = true
            }
        };

        Assert.Throws<InvalidOperationException>(() => PrivilegedActionPolicyLoader.Validate(weakened));
    }

    [Fact]
    public void Privileged_action_requires_phishing_resistant_step_up()
    {
        var authorizer = new PrivilegedActionAuthorizer(Policy());
        var now = DateTimeOffset.UtcNow;
        var request = new PrivilegedActionRequest("founder", "credential_change", "secret-store", PrivilegedRiskClass.Privileged, true);
        var authentication = new AuthenticationEvidence(true, false, false, now, TimeSpan.FromMinutes(5));

        var decision = authorizer.Authorize(request, authentication, now);

        Assert.Equal(PrivilegedDecisionKind.Deny, decision.Decision);
        Assert.Equal("PHISHING_RESISTANT_STEP_UP_REQUIRED", decision.ReasonCode);
    }

    [Fact]
    public void Voice_signal_cannot_replace_phishing_resistant_step_up()
    {
        var authorizer = new PrivilegedActionAuthorizer(Policy());
        var now = DateTimeOffset.UtcNow;
        var request = new PrivilegedActionRequest("founder", "credential_change", "secret-store", PrivilegedRiskClass.Privileged, true);
        var authentication = new AuthenticationEvidence(true, false, true, now, TimeSpan.FromMinutes(5));

        var decision = authorizer.Authorize(request, authentication, now);

        Assert.Equal(PrivilegedDecisionKind.Deny, decision.Decision);
        Assert.Equal("VOICE_ONLY_INSUFFICIENT", decision.ReasonCode);
    }

    [Fact]
    public void Ambiguous_entitlement_routes_to_review()
    {
        var authorizer = new PrivilegedActionAuthorizer(Policy());
        var now = DateTimeOffset.UtcNow;
        var request = new PrivilegedActionRequest("founder", "credential_change", "secret-store", PrivilegedRiskClass.Privileged, true, true);
        var authentication = new AuthenticationEvidence(true, true, false, now, TimeSpan.FromMinutes(5));

        var decision = authorizer.Authorize(request, authentication, now);

        Assert.Equal(PrivilegedDecisionKind.Review, decision.Decision);
    }

    [Fact]
    public void Break_glass_rejects_ttl_over_thirty_minutes()
    {
        var service = new BreakGlassAuthorizationService(Policy());

        Assert.Throws<InvalidOperationException>(() => service.Issue(
            "founder",
            "emergency production recovery",
            "prod/runtime",
            TimeSpan.FromMinutes(31),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Break_glass_requires_matching_active_scope()
    {
        var policy = Policy();
        var service = new BreakGlassAuthorizationService(policy);
        var authorizer = new PrivilegedActionAuthorizer(policy);
        var now = DateTimeOffset.UtcNow;
        var grant = service.Issue("founder", "emergency", "prod/runtime", TimeSpan.FromMinutes(10), now);
        var request = new PrivilegedActionRequest("founder", "break_glass_activation", "prod/runtime", PrivilegedRiskClass.BreakGlass, true);
        var authentication = new AuthenticationEvidence(true, true, false, now, TimeSpan.FromMinutes(5));

        var decision = authorizer.Authorize(request, authentication, now.AddMinutes(1), grant);

        Assert.Equal(PrivilegedDecisionKind.Allow, decision.Decision);
    }

    [Fact]
    public async Task Provider_readback_mismatch_never_returns_pass()
    {
        var policy = Policy();
        var auditPath = Path.Combine(Path.GetTempPath(), $"jpv-privileged-{Guid.NewGuid():N}.jsonl");
        var execution = new PrivilegedActionExecutionService(
            new PrivilegedActionAuthorizer(policy),
            new PrivilegedActionAuditStore(auditPath));
        var now = DateTimeOffset.UtcNow;
        var request = new PrivilegedActionRequest("founder", "credential_change", "secret-store", PrivilegedRiskClass.Privileged, true, false, "rotated");
        var authentication = new AuthenticationEvidence(true, true, false, now, TimeSpan.FromMinutes(5));

        var outcome = await execution.ExecuteAsync(request, authentication, new MismatchProvider(), now);

        Assert.Equal("DEGRADED", outcome.TerminalStatus);
        Assert.True(File.Exists(auditPath));
        File.Delete(auditPath);
    }

    private sealed class MismatchProvider : IPrivilegedActionProvider
    {
        public Task<PrivilegedProviderResult> ExecuteAsync(PrivilegedActionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PrivilegedProviderResult(true, "ACCEPTED", "rotated"));

        public Task<PrivilegedProviderResult> ReadBackAsync(PrivilegedActionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PrivilegedProviderResult(true, "OBSERVED", "stale"));
    }
}
