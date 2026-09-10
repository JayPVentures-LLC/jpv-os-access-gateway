namespace JPVOS.Services.Reciprocity;

public sealed class ReciprocityEvaluator
{
    public ReciprocityEvaluation Evaluate(ReciprocityEvidence evidence)
    {
        if (evidence.IsExempt)
            return new(ReciprocityState.Healthy, "explicit_or_mandatory_exemption");

        if (evidence.IsMateriallyUncertain)
            return new(ReciprocityState.Remediation, "material_uncertainty");

        if (evidence.ObligationsSatisfied || evidence.JpvValueDelivered <= evidence.ReciprocalValueReturned)
            return new(ReciprocityState.Healthy, "reciprocity_satisfied");

        if (evidence.VerifiedImbalanceObservations <= 1)
            return new(ReciprocityState.Imbalanced, "single_verified_imbalance");

        if (!evidence.RemediationOffered)
            return new(ReciprocityState.Remediation, "persistent_imbalance_requires_remediation");

        if (!evidence.RestrictionPreviouslyApplied || evidence.VerifiedImbalanceObservations < 3)
            return new(ReciprocityState.Restricted, "persistent_imbalance_after_remediation");

        return new(ReciprocityState.Revoked, "continued_imbalance_after_restriction");
    }
}
