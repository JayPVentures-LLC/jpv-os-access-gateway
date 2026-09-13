namespace JPVOS.Services.ProposalExecution;

public enum ProposalStatus { Researching, Draft, Validated, ApprovedInternally, Submitted, Acknowledged, UnderReview, Adopted, Implementation, Verification, Verified, Rejected, Superseded, Withdrawn, Closed }
public enum ProposalLane { Enterprise, Creator, Labs, PublicFacing }
public enum ProposalClass { ResearchCollaboration, GrantProposal, OperationalStandard, GovernanceStandard, PublicInterestFramework, StrategicCollaboration, InstitutionalReview }

public sealed record OutcomeMeasurement(string Metric, string Disposition, string? EvidenceReference);

public sealed record ProposalProjection(string ProposalId, string Title, ProposalClass Class, ProposalLane Lane, ProposalStatus Status, OutcomeMeasurement? LatestOutcome, DateTime LastUpdatedAtUtc)
{
    public static ProposalProjection New(string proposalId, string title, ProposalClass @class, ProposalLane lane) => new(proposalId, title, @class, lane, ProposalStatus.Researching, null, DateTime.UtcNow);
}

public sealed record ProposalValidationResult(bool IsValid, string Reason)
{
    public static ProposalValidationResult Valid() => new(true, "valid");
    public static ProposalValidationResult Invalid(string reason) => new(false, reason);
}
