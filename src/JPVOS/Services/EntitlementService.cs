using JPVOS.Models;

/// <summary>
/// DEVELOPMENT-ONLY: In-memory entitlement store. Replace with persistent storage for production.
/// </summary>
public interface IEntitlementService
{
    Entitlement? GetByStripeCustomerId(string customerId);
    void AddOrUpdate(Entitlement ent);
    void RemoveByStripeCustomerId(string customerId);
}

public class InMemoryEntitlementService : IEntitlementService
{
    // DEVELOPMENT-ONLY: This is not safe for production. Use a persistent store for real deployments.
    private readonly List<Entitlement> _entitlements = new();

    public Entitlement? GetByStripeCustomerId(string customerId) =>
        _entitlements.FirstOrDefault(e => e.StripeCustomerId == customerId);

    public void AddOrUpdate(Entitlement ent)
    {
        var existing = _entitlements.FirstOrDefault(e => e.StripeCustomerId == ent.StripeCustomerId);
        if (existing != null)
        {
            existing.StripeSubscriptionId = ent.StripeSubscriptionId;
            existing.PackageKey = ent.PackageKey;
            existing.BillingInterval = ent.BillingInterval;
            existing.Status = ent.Status;
            existing.AccessExpiration = ent.AccessExpiration;
            existing.DiscordUserId = ent.DiscordUserId;
            existing.DiscordRole = ent.DiscordRole;
        }
        else
        {
            _entitlements.Add(ent);
        }
    }

    public void RemoveByStripeCustomerId(string customerId)
    {
        _entitlements.RemoveAll(e => e.StripeCustomerId == customerId);
    }
}
