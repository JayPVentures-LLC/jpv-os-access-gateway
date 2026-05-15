using JPVOS.Models;

public interface IEntitlementRepository
{
    EntitlementRecord? GetByStripeCustomerId(string customerId);
    EntitlementRecord? GetByStripeSubscriptionId(string subscriptionId);
    EntitlementRecord? GetByDiscordUserId(string discordUserId);
    void AddOrUpdate(EntitlementRecord record);
    void RemoveByStripeCustomerId(string customerId);
    void RemoveByStripeSubscriptionId(string subscriptionId);
    IEnumerable<EntitlementRecord> GetAll();
}
