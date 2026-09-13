using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalLifecycleValidatorTests
{
    [Fact]
    public void Submission_requires_submission_evidence()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-001", "Test", ProposalClass.GovernanceStandard, ProposalLane.Enterprise);
        var result = validator.ValidateTransition(projection, ProposalStatus.Submitted, null, null);
        Assert.False(result.IsValid);
        Assert.Contains("evidence", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Acknowledgment_cannot_be_inferred_from_submission()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-002", "Test", ProposalClass.GrantProposal, ProposalLane.Enterprise) with { Status = ProposalStatus.Submitted };
        Assert.False(validator.ValidateTransition(projection, ProposalStatus.Acknowledged, null, null).IsValid);
    }

    [Fact]
    public void Adoption_requires_authority_and_evidence()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-003", "Test", ProposalClass.ResearchCollaboration, ProposalLane.Labs) with { Status = ProposalStatus.UnderReview };
        Assert.False(validator.ValidateTransition(projection, ProposalStatus.Adopted, "record-1", null).IsValid);
        Assert.False(validator.ValidateTransition(projection, ProposalStatus.Adopted, null, "institution-1").IsValid);
        Assert.True(validator.ValidateTransition(projection, ProposalStatus.Adopted, "record-1", "institution-1").IsValid);
    }

    [Fact]
    public void Researching_cannot_jump_directly_to_verified()
    {
        var validator = new ProposalLifecycleValidator();
        var projection = ProposalProjection.New("JPV-TEST-004", "Test", ProposalClass.OperationalStandard, ProposalLane.Enterprise);
        Assert.False(validator.ValidateTransition(projection, ProposalStatus.Verified, "verification-1", null).IsValid);
    }

    [Fact]
    public void Verification_is_distinct_from_outcome_success()
    {
        var projection = ProposalProjection.New("JPV-TEST-005", "Test", ProposalClass.OperationalStandard, ProposalLane.Enterprise) with { Status = ProposalStatus.Verified };
        Assert.Null(projection.LatestOutcome);
    }
}
