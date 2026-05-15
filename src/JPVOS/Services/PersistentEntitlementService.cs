using JPVOS.Models;

public class PersistentEntitlementService : IEntitlementService
{
    private readonly IEntitlementRepository _repo;
    public PersistentEntitlementService(IEntitlementRepository repo)
    {
        _repo = repo;
    }

    public Entitlement? GetByStripeCustomerId(string customerId)
    {
        var record = _repo.GetByStripeCustomerId(customerId);
        return record == null ? null : ToEntitlement(record);
    }

    public void AddOrUpdate(Entitlement ent)
    {
        var record = ToRecord(ent);
        _repo.AddOrUpdate(record);
    }

    public void RemoveByStripeCustomerId(string customerId)
    {
        _repo.RemoveByStripeCustomerId(customerId);
    }

    private Entitlement ToEntitlement(EntitlementRecord r) => new()
    {
        StripeCustomerId = r.StripeCustomerId,
        StripeSubscriptionId = r.StripeSubscriptionId,
        PackageKey = r.PackageKey,
        BillingInterval = r.BillingInterval,
        Status = r.Status,
        AccessExpiration = r.AccessExpiration,
        DiscordUserId = r.DiscordUserId,
        DiscordRole = r.DiscordRole
    };

    private EntitlementRecord ToRecord(Entitlement e) => new()
    {
        StripeCustomerId = e.StripeCustomerId,
        StripeSubscriptionId = e.StripeSubscriptionId,
        PackageKey = e.PackageKey,
        BillingInterval = e.BillingInterval,
        Status = e.Status,
        AccessExpiration = e.AccessExpiration,
        DiscordUserId = e.DiscordUserId,
        DiscordRole = e.DiscordRole,
        UpdatedAt = DateTime.UtcNow
    };
}
