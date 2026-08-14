using JPVOS.Models;

namespace JPVOS.Services.SystemicAccess;

public sealed class EntitlementAccessProvider : ISystemicAccessInventorySource, ISystemicAccessActionProvider
{
    private static readonly HashSet<string> KnownStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active", "trialing", "pending", "canceled", "cancelled", "revoked", "expired"
    };

    private readonly IEntitlementRepository _repository;

    public EntitlementAccessProvider(IEntitlementRepository repository) => _repository = repository;

    public Task<IReadOnlyCollection<SystemicAccessRecord>> GetRecordsAsync(CancellationToken cancellationToken)
    {
        var rows = _repository.GetAll().ToList();
        var duplicateSubscriptions = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.StripeSubscriptionId))
            .GroupBy(r => r.StripeSubscriptionId, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        var now = DateTime.UtcNow;
        var records = rows.Select(r =>
        {
            var normalizedStatus = r.AccessExpiration.HasValue && r.AccessExpiration.Value <= now
                ? "expired"
                : r.Status.Trim().ToLowerInvariant();
            var resourceId = !string.IsNullOrWhiteSpace(r.StripeSubscriptionId)
                ? r.StripeSubscriptionId
                : !string.IsNullOrWhiteSpace(r.StripeCustomerId) ? r.StripeCustomerId : $"entitlement:{r.Id}";
            var orphaned = string.IsNullOrWhiteSpace(r.StripeSubscriptionId) && string.IsNullOrWhiteSpace(r.StripeCustomerId);
            var stale = string.Equals(normalizedStatus, "pending", StringComparison.Ordinal) && r.UpdatedAt < now.AddDays(-7);
            var uncertain = !KnownStatuses.Contains(normalizedStatus);

            return new SystemicAccessRecord(
                "entitlement",
                resourceId,
                normalizedStatus,
                duplicateSubscriptions.Contains(r.StripeSubscriptionId),
                false,
                stale,
                orphaned,
                false,
                uncertain,
                false,
                $"entitlement_status={normalizedStatus};updated={r.UpdatedAt:O}");
        }).ToArray();

        return Task.FromResult<IReadOnlyCollection<SystemicAccessRecord>>(records);
    }

    public bool CanHandle(SystemicAccessRecord record, SystemicAccessDecision decision) =>
        record.ResourceType == "entitlement" && decision.Action is "REVOKE" or "EXPIRE";

    public Task<SystemicAccessActionResult> ApplyAsync(SystemicAccessRecord record, SystemicAccessDecision decision, CancellationToken cancellationToken)
    {
        if (!CanHandle(record, decision))
            return Task.FromResult(new SystemicAccessActionResult(false, "unsupported_action"));

        var bySubscription = _repository.GetByStripeSubscriptionId(record.ResourceId);
        if (bySubscription is not null)
        {
            _repository.RemoveByStripeSubscriptionId(record.ResourceId);
            return Task.FromResult(new SystemicAccessActionResult(true, "entitlement_removed_by_subscription"));
        }

        var byCustomer = _repository.GetByStripeCustomerId(record.ResourceId);
        if (byCustomer is not null)
        {
            _repository.RemoveByStripeCustomerId(record.ResourceId);
            return Task.FromResult(new SystemicAccessActionResult(true, "entitlement_removed_by_customer"));
        }

        return Task.FromResult(new SystemicAccessActionResult(false, "entitlement_already_absent"));
    }
}
