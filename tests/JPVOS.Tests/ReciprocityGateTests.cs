using JPVOS.Services.Reciprocity;

namespace JPVOS.Tests;

public sealed class ReciprocityGateTests
{
    [Fact]
    public void SingleAdverseObservation_IsImbalanced_NotRestricted()
    {
        var result = new ReciprocityEvaluator().Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 0m, 1, false, false, false, false, false));

        Assert.Equal(ReciprocityState.Imbalanced, result.State);
        Assert.Equal("single_verified_imbalance", result.ReasonCode);
    }

    [Fact]
    public void MaterialUncertainty_RoutesToRemediation()
    {
        var result = new ReciprocityEvaluator().Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 0m, 10, true, true, false, false, true));

        Assert.Equal(ReciprocityState.Remediation, result.State);
        Assert.Equal("material_uncertainty", result.ReasonCode);
    }

    [Fact]
    public void PersistentImbalance_RequiresRemediationOpportunityBeforeRestriction()
    {
        var evaluator = new ReciprocityEvaluator();
        var remediation = evaluator.Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 0m, 2, false, false, false, false, false));
        var restricted = evaluator.Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 0m, 2, true, false, false, false, false));

        Assert.Equal(ReciprocityState.Remediation, remediation.State);
        Assert.Equal(ReciprocityState.Restricted, restricted.State);
    }

    [Fact]
    public void Revocation_RequiresContinuedImbalanceAfterRestriction()
    {
        var evaluator = new ReciprocityEvaluator();
        var restricted = evaluator.Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 0m, 2, true, true, false, false, false));
        var revoked = evaluator.Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 0m, 3, true, true, false, false, false));

        Assert.Equal(ReciprocityState.Restricted, restricted.State);
        Assert.Equal(ReciprocityState.Revoked, revoked.State);
    }

    [Fact]
    public void FulfilledReciprocity_AutomaticallyRestoresHealthyState()
    {
        var result = new ReciprocityEvaluator().Evaluate(new ReciprocityEvidence(
            "subject-1", 100m, 100m, 8, true, true, true, false, false));

        Assert.Equal(ReciprocityState.Healthy, result.State);
        Assert.Equal("reciprocity_satisfied", result.ReasonCode);
    }

    [Fact]
    public void ExemptRelationship_RemainsHealthy()
    {
        var result = new ReciprocityEvaluator().Evaluate(new ReciprocityEvidence(
            "subject-1", 1000m, 0m, 20, true, true, false, true, false));

        Assert.Equal(ReciprocityState.Healthy, result.State);
        Assert.Equal("explicit_or_mandatory_exemption", result.ReasonCode);
    }

    [Theory]
    [InlineData(ReciprocityState.Healthy, true, true)]
    [InlineData(ReciprocityState.Imbalanced, true, true)]
    [InlineData(ReciprocityState.Remediation, true, true)]
    [InlineData(ReciprocityState.Restricted, true, false)]
    [InlineData(ReciprocityState.Revoked, false, false)]
    public void AdmissionGate_IsDeterministic(ReciprocityState state, bool remediationAllowed, bool valueIncreasingAllowed)
    {
        var gate = new ReciprocityAdmissionGate();

        var remediation = gate.Decide(new ReciprocityAdmissionRequest("subject-1", "resource", true, true, true), new ReciprocityEvaluation(state, "test"));
        var valueIncreasing = gate.Decide(new ReciprocityAdmissionRequest("subject-1", "resource", true, true, false), new ReciprocityEvaluation(state, "test"));

        Assert.Equal(remediationAllowed, remediation.Allowed);
        Assert.Equal(valueIncreasingAllowed, valueIncreasing.Allowed);
    }

    [Fact]
    public void AdmissionGate_DoesNotGovernResourcesOutsideJpvAuthority()
    {
        var decision = new ReciprocityAdmissionGate().Decide(
            new ReciprocityAdmissionRequest("subject-1", "third-party-resource", false, true, false),
            new ReciprocityEvaluation(ReciprocityState.Revoked, "test"));

        Assert.True(decision.Allowed);
        Assert.Equal("outside_jpv_authority", decision.ReasonCode);
    }

    [Fact]
    public void LedgerStore_ProvisionsPersistentLedger()
    {
        var root = Path.Combine(Path.GetTempPath(), "jpv-reciprocity-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "reciprocity-ledger.json");
        try
        {
            _ = new ReciprocityLedgerStore(path);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task LedgerStore_UpsertPreservesEvidenceReferences()
    {
        var root = Path.Combine(Path.GetTempPath(), "jpv-reciprocity-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "reciprocity-ledger.json");
        try
        {
            var store = new ReciprocityLedgerStore(path);
            await store.UpsertAsync(new ReciprocityLedgerEntry
            {
                SubjectId = "cus_123",
                JpvValueDelivered = 100m,
                ReciprocalValueReturned = 0m,
                VerifiedImbalanceObservations = 2,
                RemediationOffered = true,
                EvidenceReferences = ["evidence://one", "evidence://two"]
            });

            var evidence = store.GetEvidence("cus_123");
            Assert.NotNull(evidence);
            Assert.Equal(ReciprocityState.Restricted, new ReciprocityEvaluator().Evaluate(evidence!).State);
            Assert.Equal(["evidence://one", "evidence://two"], evidence.EvidenceReferences);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
