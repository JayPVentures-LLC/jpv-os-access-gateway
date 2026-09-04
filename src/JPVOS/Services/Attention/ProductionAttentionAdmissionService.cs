namespace JPVOS.Services.Attention;

public enum AttentionSurface
{
    Email,
    Push,
    Sms,
    Dashboard,
    Feed,
    Banner,
    Badge,
    Call
}

public enum AttentionEventClass
{
    Operational,
    Financial,
    Legal,
    Security,
    Account,
    Deadline,
    General
}

public sealed record ProductionAttentionRequest(
    string? Environment,
    AttentionSurface Surface,
    AttentionEventClass EventClass,
    bool IsSynthetic,
    bool IsDemo,
    bool IsFixture,
    bool IsPreview,
    bool HasAuthoritativeProvenance,
    bool HasAuthoritativeSourceReference,
    bool RecipientAuthorized,
    bool PassesFounderAttentionBoundary);

public sealed record ProductionAttentionDecision(bool Allowed, string Reason);

public sealed class ProductionAttentionAdmissionService
{
    private static bool IsMaterial(AttentionEventClass eventClass) => eventClass is
        AttentionEventClass.Financial or
        AttentionEventClass.Legal or
        AttentionEventClass.Security or
        AttentionEventClass.Account or
        AttentionEventClass.Deadline;

    public ProductionAttentionDecision Evaluate(ProductionAttentionRequest request)
    {
        if (!string.Equals(request.Environment, "production", StringComparison.OrdinalIgnoreCase))
        {
            return Reject("environment-not-production");
        }

        if (request.IsSynthetic || request.IsDemo || request.IsFixture || request.IsPreview)
        {
            return Reject("non-production-content");
        }

        if (!request.HasAuthoritativeProvenance)
        {
            return Reject("missing-authoritative-provenance");
        }

        if (!request.RecipientAuthorized)
        {
            return Reject("recipient-or-surface-not-authorized");
        }

        if (IsMaterial(request.EventClass) && !request.HasAuthoritativeSourceReference)
        {
            return Reject("missing-authoritative-source-reference");
        }

        if (!request.PassesFounderAttentionBoundary)
        {
            return Reject("founder-attention-boundary-rejected");
        }

        return new ProductionAttentionDecision(true, "admitted");
    }

    private static ProductionAttentionDecision Reject(string reason) => new(false, reason);
}
