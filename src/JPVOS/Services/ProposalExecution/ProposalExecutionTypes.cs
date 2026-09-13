using System.Text.Json;

namespace JPVOS.Services.ProposalExecution;

public enum ProposalStatus { Researching, Draft, Validated, ApprovedInternally, Submitted, Acknowledged, UnderReview, Adopted, Implementation, Verification, Verified, Rejected, Superseded, Withdrawn, Closed }
public enum ProposalLane { Enterprise, Creator, Labs, PublicFacing }
public enum ProposalClass { InstitutionalReform, PublicPolicy, RegulatoryReferral, ResearchCollaboration, GrantProposal, OperationalStandard, GovernanceStandard, PublicInterestFramework, CommercialOrStrategicCollaboration, StrategicCollaboration, InstitutionalReview }
public enum ProposalEvidenceClass { Assertion, SourceRecord, VerifiedFact, Inference, Recommendation, UnresolvedQuestion, CompetentAuthorityDetermination, LaterDisposition }
public enum ProposalEventType { Registered, StatusChanged, EvidenceLinked, AuthorityMapped, AdoptionRouteSet, ObligationAdded, VerificationRequirementAdded, ReviewRequirementSet, OutcomeRecorded, LineageAdded, PublicationReviewed }

public sealed record AuthorityAssignment(string Role, string AuthorityReference, string? EvidenceReference);
public sealed record ClassifiedEvidenceReference(string Reference, ProposalEvidenceClass Classification);
public sealed record AdoptionRoute(
    string TargetAuthority,
    string SubmissionMechanism,
    IReadOnlyList<string> RequiredArtifacts,
    string? EligibilityBasis,
    string? AcknowledgmentMechanism,
    string? ReviewSequence,
    string? EscalationRoute,
    string? ExpirationOrResubmissionRule,
    string RequiredEvidence);
public sealed record ImplementationObligation(
    string ObligationId,
    string ResponsibleAuthority,
    string ActionRequired,
    string CompletionEvidenceRequirement,
    bool Completed,
    string? CompletionEvidenceReference,
    string? Prerequisite = null,
    DateTime? TargetDateUtc = null,
    string? ProducedArtifactOrState = null,
    string? FailureCondition = null,
    string? RemediationRoute = null);
public sealed record VerificationRequirement(string RequirementId, string Method, string EvidenceRequired, string? ExpectedRepositoryHead = null);
public sealed record ReviewRequirement(string Stage, bool IndependentReviewRequired, string Reason, string? ReviewerReference = null);
public sealed record OutcomeMeasurement(
    string Metric,
    string Disposition,
    string? EvidenceReference,
    string? Baseline = null,
    string? Target = null,
    string? Method = null,
    string? DataSource = null,
    string? ObservationWindow = null,
    string? AdverseEffectIndicator = null,
    string? ReviewOwner = null,
    string? ConfidenceNotes = null,
    string? RevisionTrigger = null);
public sealed record ProposalLineage(string Relationship, string RelatedId);

public sealed record ProposalLifecycleEvent(
    string EventId,
    string ProposalId,
    long Sequence,
    DateTime OccurredAtUtc,
    ProposalEventType Type,
    string? IdempotencyKey,
    string PayloadJson)
{
    private static ProposalLifecycleEvent Create<T>(string proposalId, ProposalEventType type, T payload, string? idempotencyKey = null) =>
        new(Guid.NewGuid().ToString("N"), proposalId, 0, DateTime.UtcNow, type, idempotencyKey, JsonSerializer.Serialize(payload));

    public static ProposalLifecycleEvent Registered(string proposalId, string title, ProposalClass @class, ProposalLane lane, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.Registered, new RegistrationPayload(title, @class, lane), idempotencyKey);

    public static ProposalLifecycleEvent StatusChanged(string proposalId, ProposalStatus status, string? evidenceReference, string? authorityReference = null, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.StatusChanged, new StatusPayload(status, evidenceReference, authorityReference), idempotencyKey);

    public static ProposalLifecycleEvent EvidenceLinked(string proposalId, ClassifiedEvidenceReference evidence, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.EvidenceLinked, evidence, idempotencyKey);

    public static ProposalLifecycleEvent AuthorityMapped(string proposalId, AuthorityAssignment authority, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.AuthorityMapped, authority, idempotencyKey);

    public static ProposalLifecycleEvent AdoptionRouteSet(string proposalId, AdoptionRoute route, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.AdoptionRouteSet, route, idempotencyKey);

    public static ProposalLifecycleEvent ObligationAdded(string proposalId, ImplementationObligation obligation, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.ObligationAdded, obligation, idempotencyKey);

    public static ProposalLifecycleEvent VerificationRequirementAdded(string proposalId, VerificationRequirement requirement, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.VerificationRequirementAdded, requirement, idempotencyKey);

    public static ProposalLifecycleEvent ReviewRequirementSet(string proposalId, ReviewRequirement requirement, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.ReviewRequirementSet, requirement, idempotencyKey);

    public static ProposalLifecycleEvent OutcomeRecorded(string proposalId, OutcomeMeasurement outcome, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.OutcomeRecorded, outcome, idempotencyKey);

    public static ProposalLifecycleEvent LineageAdded(string proposalId, ProposalLineage lineage, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.LineageAdded, lineage, idempotencyKey);

    public static ProposalLifecycleEvent PublicationReviewed(string proposalId, bool approved, string? summary, string? idempotencyKey = null) =>
        Create(proposalId, ProposalEventType.PublicationReviewed, new PublicationPayload(approved, summary), idempotencyKey);

    public sealed record RegistrationPayload(string Title, ProposalClass Class, ProposalLane Lane);
    public sealed record StatusPayload(ProposalStatus Status, string? EvidenceReference, string? AuthorityReference);
    public sealed record PublicationPayload(bool Approved, string? Summary);
}

public sealed record ProposalProjection(
    string ProposalId,
    string Title,
    ProposalClass Class,
    ProposalLane Lane,
    ProposalStatus Status,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<AuthorityAssignment> Authorities,
    IReadOnlyList<ImplementationObligation> Obligations,
    OutcomeMeasurement? LatestOutcome,
    IReadOnlyList<ProposalLineage> Lineage,
    bool PublicReleaseApproved,
    string? PublicSummary,
    DateTime LastUpdatedAtUtc)
{
    public IReadOnlyList<ClassifiedEvidenceReference> ClassifiedEvidence { get; init; } = [];
    public AdoptionRoute? AdoptionRoute { get; init; }
    public IReadOnlyList<VerificationRequirement> VerificationRequirements { get; init; } = [];
    public ReviewRequirement? ReviewRequirement { get; init; }

    public static ProposalProjection New(string proposalId, string title, ProposalClass @class, ProposalLane lane) =>
        new(proposalId, title, @class, lane, ProposalStatus.Researching, [], [], [], null, [], false, null, DateTime.UtcNow);
}

public sealed record ProposalValidationResult(bool IsValid, string Reason)
{
    public static ProposalValidationResult Valid() => new(true, "valid");
    public static ProposalValidationResult Invalid(string reason) => new(false, reason);
}
