namespace JPVOS.Services.Reciprocity;

public sealed class ReciprocityAdmissionGate
{
    public ReciprocityAdmissionDecision Decide(
        ReciprocityAdmissionRequest request,
        ReciprocityEvaluation evaluation)
    {
        if (!request.IsJpvOwnedOrAdministered)
            return new(true, "outside_jpv_authority", evaluation.State);

        if (!request.IsDiscretionary)
            return new(true, "mandatory_or_contractual_access", evaluation.State);

        return evaluation.State switch
        {
            ReciprocityState.Restricted when request.IsRemediationPath =>
                new(true, "restricted_remediation_path", evaluation.State),
            ReciprocityState.Restricted =>
                new(false, "restricted_discretionary_access", evaluation.State),
            ReciprocityState.Revoked =>
                new(false, "revoked_discretionary_access", evaluation.State),
            _ => new(true, "reciprocity_access_allowed", evaluation.State)
        };
    }
}
