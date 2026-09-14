namespace JPVOS.Services.ProposalExecution;

public sealed class ProposalLifecycleValidator
{
    public ProposalValidationResult ValidateTransition(ProposalProjection current, ProposalStatus next, string? evidenceReference, string? authorityReference)
    {
        if (!IsAllowed(current.Status, next))
            return ProposalValidationResult.Invalid($"Unsupported lifecycle transition: {current.Status} -> {next}.");

        if (next is ProposalStatus.Submitted or ProposalStatus.Acknowledged)
        {
            if (string.IsNullOrWhiteSpace(evidenceReference))
                return ProposalValidationResult.Invalid(next == ProposalStatus.Submitted ? "submission evidence is required" : "acknowledgment evidence is required");
            if (!HasRecordedExternalEvidence(current, evidenceReference))
                return ProposalValidationResult.Invalid(next == ProposalStatus.Submitted
                    ? "submission evidence must already be recorded as external evidence"
                    : "acknowledgment evidence must already be recorded as external evidence");
        }

        if (next == ProposalStatus.Adopted)
        {
            if (string.IsNullOrWhiteSpace(evidenceReference) || string.IsNullOrWhiteSpace(authorityReference))
                return ProposalValidationResult.Invalid("adoption requires authority and evidence");
            if (!current.Authorities.Any(x => string.Equals(x.Role, "decision-authority", StringComparison.OrdinalIgnoreCase) && x.AuthorityReference == authorityReference))
                return ProposalValidationResult.Invalid("adoption authority must already be mapped as decision-authority");
            if (!current.ClassifiedEvidence.Any(x => x.Reference == evidenceReference && x.Classification == ProposalEvidenceClass.CompetentAuthorityDetermination))
                return ProposalValidationResult.Invalid("adoption evidence must be a recorded competent-authority determination");
        }

        if (next == ProposalStatus.Implementation)
        {
            if (string.IsNullOrWhiteSpace(evidenceReference) || !HasRecordedExternalEvidence(current, evidenceReference))
                return ProposalValidationResult.Invalid("implementation requires recorded authorization evidence");
            if (current.Obligations.Count == 0)
                return ProposalValidationResult.Invalid("implementation requires at least one explicit implementation obligation");
        }

        if (next == ProposalStatus.Verification)
        {
            if (string.IsNullOrWhiteSpace(evidenceReference))
                return ProposalValidationResult.Invalid("verification requires implementation evidence");
            if (current.Obligations.Any(x => !x.Completed || string.IsNullOrWhiteSpace(x.CompletionEvidenceReference)))
                return ProposalValidationResult.Invalid("all implementation obligations must be completed with evidence before verification");
        }

        if (next == ProposalStatus.Verified)
        {
            if (string.IsNullOrWhiteSpace(evidenceReference))
                return ProposalValidationResult.Invalid("verification evidence is required");
            foreach (var requirement in current.VerificationRequirements)
            {
                if (!string.IsNullOrWhiteSpace(requirement.ExpectedRepositoryHead) &&
                    !string.Equals(evidenceReference, requirement.ExpectedRepositoryHead, StringComparison.Ordinal))
                    return ProposalValidationResult.Invalid($"verification evidence does not satisfy requirement {requirement.RequirementId}");
            }
        }

        return ProposalValidationResult.Valid();
    }

    private static bool HasRecordedExternalEvidence(ProposalProjection current, string reference) =>
        current.ClassifiedEvidence.Any(x => x.Reference == reference && x.Classification is
            ProposalEvidenceClass.SourceRecord or
            ProposalEvidenceClass.VerifiedFact or
            ProposalEvidenceClass.CompetentAuthorityDetermination or
            ProposalEvidenceClass.LaterDisposition);

    private static bool IsAllowed(ProposalStatus current, ProposalStatus next)
    {
        if (current == next) return true;
        if (next is ProposalStatus.Rejected or ProposalStatus.Withdrawn or ProposalStatus.Closed or ProposalStatus.Superseded) return true;

        return next switch
        {
            ProposalStatus.Draft => current == ProposalStatus.Researching,
            ProposalStatus.Validated => current is ProposalStatus.Researching or ProposalStatus.Draft,
            ProposalStatus.ApprovedInternally => current is ProposalStatus.Draft or ProposalStatus.Validated,
            ProposalStatus.Submitted => current is ProposalStatus.Researching or ProposalStatus.Draft or ProposalStatus.Validated or ProposalStatus.ApprovedInternally,
            ProposalStatus.Acknowledged => current == ProposalStatus.Submitted,
            ProposalStatus.UnderReview => current is ProposalStatus.Submitted or ProposalStatus.Acknowledged,
            ProposalStatus.Adopted => current is ProposalStatus.UnderReview or ProposalStatus.Acknowledged or ProposalStatus.ApprovedInternally,
            ProposalStatus.Implementation => current is ProposalStatus.Adopted or ProposalStatus.ApprovedInternally,
            ProposalStatus.Verification => current == ProposalStatus.Implementation,
            ProposalStatus.Verified => current == ProposalStatus.Verification,
            ProposalStatus.Researching => false,
            _ => false
        };
    }
}
