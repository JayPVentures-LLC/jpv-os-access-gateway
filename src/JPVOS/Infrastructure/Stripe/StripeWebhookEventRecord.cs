namespace JPVOS.Infrastructure.Stripe;

public sealed class StripeWebhookEventRecord
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
