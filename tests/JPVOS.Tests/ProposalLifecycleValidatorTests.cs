using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalLifecycleValidatorTests
{
    [Fact]
    public void Submission_requires_submission_evidence()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-001", "Test", ProposalClass.GovernanceStandard, ProposalLane.Enterprise);

        var result = validator.ValidateTransition(projection, ProposalStatus.Submitted, evidenceReference: null, authorityReference: null);

        Assert.False(result.IsValid);
        Assert.Contains("evidence", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Acknowledgment_cannot_be_inferred_from_submission()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-002", "Test", ProposalClass.PublicPolicy, ProposalLane.Enterprise)
            with { Status = ProposalStatus.Submitted };

        var result = validator.ValidateTransition(projection, ProposalStatus.Acknowledged, evidenceReference: null, authorityReference: null);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Adoption_requires_competent_authority_and_evidence()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-003", "Test", ProposalClass.ResearchCollaboration, ProposalLane.Labs)
            with { Status = ProposalStatus.UnderReview };

        Assert.False(validator.ValidateTransition(projection, ProposalStatus.Adopted, "receipt-1", null).IsValid);
        Assert.False(validator.ValidateTransition(projection, ProposalStatus.Adopted, null, "purdue-authority").IsValid);
        Assert.True(validator.ValidateTransition(projection, ProposalStatus.Adopted, "agreement-1", "purdue-authority").IsValid);
    }

    [Fact]
    public void Verification_is_distinct_from_outcome_success()
    {
        var projection = ProposalProjection.New("JPV-TEST-004", "Test", ProposalClass.OperationalStandard, ProposalLane.Enterprise)
            with { Status = ProposalStatus.Verified };

        Assert.Null(projection.LatestOutcome);
    }
}
