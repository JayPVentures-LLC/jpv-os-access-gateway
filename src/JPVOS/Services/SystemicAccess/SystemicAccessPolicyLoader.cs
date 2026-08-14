using System.Text.Json;

namespace JPVOS.Services.SystemicAccess;

public static class SystemicAccessPolicyLoader
{
    private const string CanonicalId = "JPV-GOV-SYSTEMIC-ACCESS-HYGIENE-001";
    private static readonly string[] RequiredActions = ["VALID", "QUARANTINE", "REVOKE", "ROTATE", "DEDUPLICATE", "EXPIRE", "REVIEW"];

    public static SystemicAccessPolicy LoadAndValidate(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException("BOOT_BLOCKED: Systemic access hygiene policy is missing.");

        SystemicAccessPolicy? policy;
        try
        {
            policy = JsonSerializer.Deserialize<SystemicAccessPolicy>(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("BOOT_BLOCKED: Systemic access hygiene policy is malformed.", ex);
        }

        if (policy is null || !string.Equals(policy.Id, CanonicalId, StringComparison.Ordinal))
            throw new InvalidOperationException("BOOT_BLOCKED: Systemic access hygiene policy identity is invalid.");

        var missing = RequiredActions.Except(policy.Actions, StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("BOOT_BLOCKED: Systemic access hygiene policy is incomplete. Missing actions: " + string.Join(", ", missing));

        if (!policy.Interpretation.SystemicOnly || policy.Interpretation.PersonTargeting)
            throw new InvalidOperationException("BOOT_BLOCKED: Systemic access hygiene interpretation safeguards were weakened.");

        if (!string.Equals(policy.DefaultUncertainAction, "QUARANTINE", StringComparison.Ordinal) ||
            !policy.PreserveAuditReceipt ||
            !string.Equals(policy.ReEnableRequires, "explicit_current_authorization", StringComparison.Ordinal))
            throw new InvalidOperationException("BOOT_BLOCKED: Systemic access hygiene safety requirements were weakened.");

        return policy;
    }
}
