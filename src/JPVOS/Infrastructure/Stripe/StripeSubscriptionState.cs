namespace JPVOS.Infrastructure.Stripe;

public sealed class StripeSubscriptionState
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public string? Status { get; set; }
    public bool EntitlementActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
