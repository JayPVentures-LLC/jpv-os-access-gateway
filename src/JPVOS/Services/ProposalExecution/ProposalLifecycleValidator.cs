namespace JPVOS.Services.ProposalExecution;

public sealed class ProposalLifecycleValidator
{
    public ProposalValidationResult ValidateTransition(ProposalProjection current, ProposalStatus next, string? evidenceReference, string? authorityReference)
    {
        if (next == ProposalStatus.Submitted && string.IsNullOrWhiteSpace(evidenceReference))
            return ProposalValidationResult.Invalid("submission evidence is required");

        if (next == ProposalStatus.Acknowledged && string.IsNullOrWhiteSpace(evidenceReference))
            return ProposalValidationResult.Invalid("acknowledgment evidence is required");

        if (next == ProposalStatus.Adopted && (string.IsNullOrWhiteSpace(evidenceReference) || string.IsNullOrWhiteSpace(authorityReference)))
            return ProposalValidationResult.Invalid("adoption requires authority and evidence");

        if (next == ProposalStatus.Verified && string.IsNullOrWhiteSpace(evidenceReference))
            return ProposalValidationResult.Invalid("verification evidence is required");

        return ProposalValidationResult.Valid();
    }
}
