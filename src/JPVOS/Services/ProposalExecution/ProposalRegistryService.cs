namespace JPVOS.Services.ProposalExecution;

public sealed class ProposalRegistryService(IProposalEventStore store, ProposalLifecycleValidator validator) : IProposalRegistryService
{
    private static readonly HashSet<string> OutcomeDispositions = new(StringComparer.OrdinalIgnoreCase)
    {
        "not-yet-measurable", "insufficient-data", "mixed", "met", "not-met", "adverse-effect-detected"
    };

    private static readonly HashSet<string> AuthorityRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "proposal-owner", "decision-authority", "implementation-authority", "funding-authority",
        "oversight-authority", "review-authority", "appeal-or-escalation-authority", "evidence-custodian"
    };

    public async Task<ProposalProjection> RegisterAsync(string proposalId, string title, ProposalClass proposalClass, ProposalLane lane, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(proposalId)) throw new ProposalValidationException("Proposal ID is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new ProposalValidationException("Proposal title is required.");

        var item = ProposalLifecycleEvent.Registered(proposalId, title, proposalClass, lane, idempotencyKey);
        var replay = await TryReplayAsync(item, cancellationToken);
        if (replay is not null) return replay;

        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        if (stream.Count > 0)
        {
            var existing = ProposalProjector.Project(stream);
            if (existing.Title == title && existing.Class == proposalClass && existing.Lane == lane) return existing;
            throw new ProposalValidationException("Proposal ID is already registered with different canonical metadata.");
        }

        await store.AppendAsync(item, expectedVersion: 0, cancellationToken);
        return await GetRequiredAsync(proposalId, cancellationToken);
    }

    public async Task<ProposalProjection> RecordStatusAsync(string proposalId, ProposalStatus next, string? evidenceReference, string? authorityReference, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var item = ProposalLifecycleEvent.StatusChanged(proposalId, next, evidenceReference, authorityReference, idempotencyKey);
        var replay = await TryReplayAsync(item, cancellationToken);
        if (replay is not null) return replay;

        var (current, version) = await GetSnapshotAsync(proposalId, cancellationToken);
        var validation = validator.ValidateTransition(current, next, evidenceReference, authorityReference);
        if (!validation.IsValid) throw new ProposalValidationException(validation.Reason);
        return await AppendAndReadAsync(item, version, cancellationToken);
    }

    public async Task<ProposalProjection> LinkEvidenceAsync(string proposalId, ClassifiedEvidenceReference evidence, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(evidence.Reference)) throw new ProposalValidationException("Evidence reference is required.");
        return await AppendGovernedAsync(ProposalLifecycleEvent.EvidenceLinked(proposalId, evidence, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> MapAuthorityAsync(string proposalId, AuthorityAssignment authority, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!AuthorityRoles.Contains(authority.Role)) throw new ProposalValidationException("Authority role is not part of the canonical authority matrix.");
        if (string.IsNullOrWhiteSpace(authority.AuthorityReference)) throw new ProposalValidationException("Authority reference is required.");
        return await AppendGovernedAsync(ProposalLifecycleEvent.AuthorityMapped(proposalId, authority, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> SetAdoptionRouteAsync(string proposalId, AdoptionRoute route, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(route.TargetAuthority) || string.IsNullOrWhiteSpace(route.SubmissionMechanism) || string.IsNullOrWhiteSpace(route.RequiredEvidence))
            throw new ProposalValidationException("Adoption route requires target authority, submission mechanism, and evidence requirement.");
        return await AppendGovernedAsync(ProposalLifecycleEvent.AdoptionRouteSet(proposalId, route, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> AddObligationAsync(string proposalId, ImplementationObligation obligation, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(obligation.ObligationId) || string.IsNullOrWhiteSpace(obligation.CompletionEvidenceRequirement))
            throw new ProposalValidationException("Obligation ID and completion evidence requirement are required.");
        if (obligation.Completed && string.IsNullOrWhiteSpace(obligation.CompletionEvidenceReference))
            throw new ProposalValidationException("Completed obligations require completion evidence.");
        return await AppendGovernedAsync(ProposalLifecycleEvent.ObligationAdded(proposalId, obligation, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> AddVerificationRequirementAsync(string proposalId, VerificationRequirement requirement, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requirement.RequirementId) || string.IsNullOrWhiteSpace(requirement.Method) || string.IsNullOrWhiteSpace(requirement.EvidenceRequired))
            throw new ProposalValidationException("Verification requirement ID, method, and evidence are required.");
        return await AppendGovernedAsync(ProposalLifecycleEvent.VerificationRequirementAdded(proposalId, requirement, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> SetReviewRequirementAsync(string proposalId, ReviewRequirement requirement, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requirement.Stage) || string.IsNullOrWhiteSpace(requirement.Reason))
            throw new ProposalValidationException("Review stage and reason are required.");
        if (requirement.Completed && (string.IsNullOrWhiteSpace(requirement.ReviewerReference) || string.IsNullOrWhiteSpace(requirement.ReviewEvidenceReference)))
            throw new ProposalValidationException("Completed review requires reviewer and review evidence.");

        var (current, _) = await GetSnapshotAsync(proposalId, cancellationToken);
        if (requirement.Completed && requirement.IndependentReviewRequired && IsConflictedReviewer(current, requirement.ReviewerReference!))
            throw new ProposalValidationException("Independent reviewer conflicts with proposal ownership or implementation authority.");

        return await AppendGovernedAsync(ProposalLifecycleEvent.ReviewRequirementSet(proposalId, requirement, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> RecordOutcomeAsync(string proposalId, OutcomeMeasurement outcome, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outcome.Metric) || !OutcomeDispositions.Contains(outcome.Disposition))
            throw new ProposalValidationException("Outcome requires a metric and canonical disposition.");

        var measured = outcome.Disposition is "mixed" or "met" or "not-met" or "adverse-effect-detected";
        if (measured && (string.IsNullOrWhiteSpace(outcome.EvidenceReference) || string.IsNullOrWhiteSpace(outcome.Method) ||
                         string.IsNullOrWhiteSpace(outcome.DataSource) || string.IsNullOrWhiteSpace(outcome.ObservationWindow) || string.IsNullOrWhiteSpace(outcome.ReviewOwner)))
            throw new ProposalValidationException("Measured outcomes require evidence, method, data source, observation window, and review owner.");

        return await AppendGovernedAsync(ProposalLifecycleEvent.OutcomeRecorded(proposalId, outcome, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> AddLineageAsync(string proposalId, ProposalLineage lineage, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lineage.Relationship) || string.IsNullOrWhiteSpace(lineage.RelatedId))
            throw new ProposalValidationException("Lineage relationship and related ID are required.");
        return await AppendGovernedAsync(ProposalLifecycleEvent.LineageAdded(proposalId, lineage, idempotencyKey), cancellationToken);
    }

    public async Task<ProposalProjection> RecordReleaseAsync(string proposalId, bool approved, string? summary, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var item = ProposalLifecycleEvent.PublicationReviewed(proposalId, approved, summary, idempotencyKey);
        var replay = await TryReplayAsync(item, cancellationToken);
        if (replay is not null) return replay;

        var (current, version) = await GetSnapshotAsync(proposalId, cancellationToken);
        if (approved && string.IsNullOrWhiteSpace(summary)) throw new ProposalValidationException("An approved release requires a public summary.");
        if (approved && current.ReviewRequirement is { Stage: "before-publication", IndependentReviewRequired: true } review)
        {
            if (!review.Completed || string.IsNullOrWhiteSpace(review.ReviewerReference) || string.IsNullOrWhiteSpace(review.ReviewEvidenceReference))
                throw new ProposalValidationException("Required independent publication review is not complete.");
            if (IsConflictedReviewer(current, review.ReviewerReference))
                throw new ProposalValidationException("Publication reviewer conflicts with proposal ownership or implementation authority.");
        }

        return await AppendAndReadAsync(item, version, cancellationToken);
    }

    public async Task<ProposalProjection?> GetAsync(string proposalId, CancellationToken cancellationToken)
    {
        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        return stream.Count == 0 ? null : ProposalProjector.Project(stream);
    }

    private async Task<ProposalProjection> AppendGovernedAsync(ProposalLifecycleEvent item, CancellationToken cancellationToken)
    {
        var replay = await TryReplayAsync(item, cancellationToken);
        if (replay is not null) return replay;
        var (_, version) = await GetSnapshotAsync(item.ProposalId, cancellationToken);
        return await AppendAndReadAsync(item, version, cancellationToken);
    }

    private async Task<ProposalProjection?> TryReplayAsync(ProposalLifecycleEvent item, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.IdempotencyKey)) return null;
        var existing = await store.FindByIdempotencyKeyAsync(item.ProposalId, item.IdempotencyKey, cancellationToken);
        if (existing is null) return null;
        if (existing.Type != item.Type || !string.Equals(existing.PayloadJson, item.PayloadJson, StringComparison.Ordinal))
            throw new ProposalIdempotencyConflictException("Idempotency key was already used with different proposal event content.");
        return await GetRequiredAsync(item.ProposalId, cancellationToken);
    }

    private async Task<(ProposalProjection Projection, long Version)> GetSnapshotAsync(string proposalId, CancellationToken cancellationToken)
    {
        var stream = await store.ReadStreamAsync(proposalId, cancellationToken);
        if (stream.Count == 0) throw new ProposalValidationException("Proposal does not exist.");
        return (ProposalProjector.Project(stream), stream[^1].Sequence);
    }

    private async Task<ProposalProjection> AppendAndReadAsync(ProposalLifecycleEvent item, long expectedVersion, CancellationToken cancellationToken)
    {
        await store.AppendAsync(item, expectedVersion, cancellationToken);
        return await GetRequiredAsync(item.ProposalId, cancellationToken);
    }

    private async Task<ProposalProjection> GetRequiredAsync(string proposalId, CancellationToken cancellationToken) =>
        await GetAsync(proposalId, cancellationToken) ?? throw new ProposalValidationException("Proposal does not exist.");

    private static bool IsConflictedReviewer(ProposalProjection current, string reviewerReference) =>
        current.Authorities.Any(x =>
            (string.Equals(x.Role, "proposal-owner", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Role, "implementation-authority", StringComparison.OrdinalIgnoreCase)) &&
            x.AuthorityReference == reviewerReference) ||
        current.Obligations.Any(x => x.ResponsibleAuthority == reviewerReference);
}
