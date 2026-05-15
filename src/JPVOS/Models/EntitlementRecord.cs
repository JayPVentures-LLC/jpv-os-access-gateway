using System;

namespace JPVOS.Models
{
    public class EntitlementRecord
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string StripeCustomerId { get; set; } = string.Empty;
        public string StripeSubscriptionId { get; set; } = string.Empty;
        public string PackageKey { get; set; } = string.Empty;
        public string BillingInterval { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string DiscordUserId { get; set; } = string.Empty;
        public string DiscordRole { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AccessExpiration { get; set; }
    }
}
