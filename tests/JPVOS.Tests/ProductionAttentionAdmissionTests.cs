using JPVOS.Services.Attention;

namespace JPVOS.Tests;

public sealed class ProductionAttentionAdmissionTests
{
    private static readonly ProductionAttentionAdmissionService Sut = new();

    [Fact]
    public void Rejects_demo_financial_event_for_production_surface()
    {
        var request = new ProductionAttentionRequest(
            Environment: "production",
            Surface: AttentionSurface.Push,
            EventClass: AttentionEventClass.Financial,
            IsSynthetic: false,
            IsDemo: true,
            IsFixture: false,
            IsPreview: false,
            HasAuthoritativeProvenance: true,
            HasAuthoritativeSourceReference: true,
            RecipientAuthorized: true,
            PassesFounderAttentionBoundary: true);

        var result = Sut.Evaluate(request);

        Assert.False(result.Allowed);
        Assert.Equal("non-production-content", result.Reason);
    }

    [Fact]
    public void Rejects_production_delivery_when_provenance_is_missing()
    {
        var request = ValidProductionRequest() with { HasAuthoritativeProvenance = false };

        var result = Sut.Evaluate(request);

        Assert.False(result.Allowed);
        Assert.Equal("missing-authoritative-provenance", result.Reason);
    }

    [Fact]
    public void Rejects_material_financial_event_without_source_reference()
    {
        var request = ValidProductionRequest() with
        {
            EventClass = AttentionEventClass.Financial,
            HasAuthoritativeSourceReference = false
        };

        var result = Sut.Evaluate(request);

        Assert.False(result.Allowed);
        Assert.Equal("missing-authoritative-source-reference", result.Reason);
    }

    [Fact]
    public void Rejects_unauthorized_recipient_or_surface()
    {
        var request = ValidProductionRequest() with { RecipientAuthorized = false };

        var result = Sut.Evaluate(request);

        Assert.False(result.Allowed);
        Assert.Equal("recipient-or-surface-not-authorized", result.Reason);
    }

    [Fact]
    public void Rejects_event_that_fails_founder_attention_boundary()
    {
        var request = ValidProductionRequest() with { PassesFounderAttentionBoundary = false };

        var result = Sut.Evaluate(request);

        Assert.False(result.Allowed);
        Assert.Equal("founder-attention-boundary-rejected", result.Reason);
    }

    [Fact]
    public void Rejects_missing_or_nonproduction_environment_for_production_surface()
    {
        var request = ValidProductionRequest() with { Environment = "staging" };

        var result = Sut.Evaluate(request);

        Assert.False(result.Allowed);
        Assert.Equal("environment-not-production", result.Reason);
    }

    [Fact]
    public void Allows_real_production_event_only_when_all_admission_requirements_pass()
    {
        var result = Sut.Evaluate(ValidProductionRequest());

        Assert.True(result.Allowed);
        Assert.Equal("admitted", result.Reason);
    }

    private static ProductionAttentionRequest ValidProductionRequest() => new(
        Environment: "production",
        Surface: AttentionSurface.Email,
        EventClass: AttentionEventClass.Operational,
        IsSynthetic: false,
        IsDemo: false,
        IsFixture: false,
        IsPreview: false,
        HasAuthoritativeProvenance: true,
        HasAuthoritativeSourceReference: true,
        RecipientAuthorized: true,
        PassesFounderAttentionBoundary: true);
}
