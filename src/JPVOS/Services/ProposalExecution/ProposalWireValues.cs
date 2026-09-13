namespace JPVOS.Services.ProposalExecution;

public static class ProposalWireValues
{
    public static string Status(ProposalStatus status) => status switch
    {
        ProposalStatus.Researching => "researching",
        ProposalStatus.Draft => "draft",
        ProposalStatus.Validated => "validated",
        ProposalStatus.ApprovedInternally => "approved-internally",
        ProposalStatus.Submitted => "submitted",
        ProposalStatus.Acknowledged => "acknowledged",
        ProposalStatus.UnderReview => "under-review",
        ProposalStatus.Adopted => "adopted",
        ProposalStatus.Implementation => "implementation",
        ProposalStatus.Verification => "verification",
        ProposalStatus.Verified => "verified",
        ProposalStatus.Rejected => "rejected",
        ProposalStatus.Superseded => "superseded",
        ProposalStatus.Withdrawn => "withdrawn",
        ProposalStatus.Closed => "closed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown proposal status.")
    };
}
