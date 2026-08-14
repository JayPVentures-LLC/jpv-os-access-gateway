namespace JPVOS.Services.SystemicAccess;

public sealed class SystemicAccessClassifier
{
    public SystemicAccessDecision Classify(SystemicAccessRecord record)
    {
        if (record.IsFounderProtected)
            return new("REVIEW", "founder_or_break_glass_protected");
        if (record.IsUncertain)
            return new("REVIEW", "material_uncertainty");
        if (record.IsCompromised)
            return new("ROTATE", "compromised_state");
        if (string.Equals(record.Status, "revoked", StringComparison.OrdinalIgnoreCase))
            return new("REVOKE", "verified_revoked_state");
        if (string.Equals(record.Status, "expired", StringComparison.OrdinalIgnoreCase))
            return new("EXPIRE", "verified_expired_state");
        if (record.IsDuplicate)
            return new("DEDUPLICATE", "verified_duplicate_state");
        if (record.IsStale || record.IsOrphaned || record.IsUnowned)
            return new("QUARANTINE", "stale_or_unowned_state");

        return new("VALID", "current_authorized_state");
    }
}
