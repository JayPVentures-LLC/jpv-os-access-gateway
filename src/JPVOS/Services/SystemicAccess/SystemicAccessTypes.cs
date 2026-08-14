namespace JPVOS.Services.SystemicAccess;

public sealed record SystemicAccessRecord(
    string ResourceType,
    string ResourceId,
    string Status,
    bool IsDuplicate,
    bool IsCompromised,
    bool IsStale,
    bool IsOrphaned,
    bool IsUnowned,
    bool IsUncertain,
    bool IsFounderProtected,
    string Evidence);

public sealed record SystemicAccessDecision(string Action, string Reason);

public sealed record SystemicAccessActionResult(bool Applied, string Result);

public sealed record SystemicAccessReconciliationSummary(int Evaluated, int ActionsApplied, int Failures, DateTimeOffset CompletedAtUtc);
