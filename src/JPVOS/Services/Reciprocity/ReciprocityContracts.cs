namespace JPVOS.Services.Reciprocity;

public enum ReciprocityState
{
    Healthy,
    Imbalanced,
    Remediation,
    Restricted,
    Revoked
}

public sealed record ReciprocityEvidence(
    string SubjectId,
    decimal JpvValueDelivered,
    decimal ReciprocalValueReturned,
    int VerifiedImbalanceObservations,
    bool RemediationOffered,
    bool RestrictionPreviouslyApplied,
    bool ObligationsSatisfied,
    bool IsExempt,
    bool IsMateriallyUncertain)
{
    public bool NonImpositionGateSatisfied { get; init; }
    public IReadOnlyList<string> EvidenceReferences { get; init; } = Array.Empty<string>();
}

public sealed record ReciprocityEvaluation(
    ReciprocityState State,
    string ReasonCode);

public sealed record ReciprocityAdmissionRequest(
    string SubjectId,
    string ResourceId,
    bool IsJpvOwnedOrAdministered,
    bool IsDiscretionary,
    bool IsRemediationPath);

public sealed record ReciprocityAdmissionDecision(
    bool Allowed,
    string ReasonCode,
    ReciprocityState State);

public sealed record ReciprocityAuditReceipt(
    string SubjectId,
    string ResourceId,
    ReciprocityState State,
    bool Allowed,
    string ReasonCode,
    DateTimeOffset DecidedAtUtc)
{
    public IReadOnlyList<string> EvidenceReferences { get; init; } = Array.Empty<string>();
}
