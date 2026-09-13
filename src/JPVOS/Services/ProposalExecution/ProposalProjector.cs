using System.Text.Json;

namespace JPVOS.Services.ProposalExecution;

public static class ProposalProjector
{
    public static ProposalProjection Project(IEnumerable<ProposalLifecycleEvent> events)
    {
        ProposalProjection? projection = null;

        foreach (var item in events.OrderBy(x => x.Sequence))
        {
            switch (item.Type)
            {
                case ProposalEventType.Registered:
                {
                    var payload = Read<ProposalLifecycleEvent.RegistrationPayload>(item);
                    projection = ProposalProjection.New(item.ProposalId, payload.Title, payload.Class, payload.Lane)
                        with { LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.StatusChanged when projection is not null:
                {
                    var payload = Read<ProposalLifecycleEvent.StatusPayload>(item);
                    var evidence = projection.EvidenceReferences.ToList();
                    if (!string.IsNullOrWhiteSpace(payload.EvidenceReference) && !evidence.Contains(payload.EvidenceReference)) evidence.Add(payload.EvidenceReference);
                    var authorities = projection.Authorities.ToList();
                    if (!string.IsNullOrWhiteSpace(payload.AuthorityReference) && !authorities.Any(x => x.AuthorityReference == payload.AuthorityReference))
                        authorities.Add(new AuthorityAssignment("decision-authority", payload.AuthorityReference, payload.EvidenceReference));
                    projection = projection with { Status = payload.Status, EvidenceReferences = evidence, Authorities = authorities, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.EvidenceLinked when projection is not null:
                {
                    var classified = projection.ClassifiedEvidence.ToList();
                    var evidence = Read<ClassifiedEvidenceReference>(item);
                    if (!classified.Contains(evidence)) classified.Add(evidence);
                    var references = projection.EvidenceReferences.ToList();
                    if (!references.Contains(evidence.Reference)) references.Add(evidence.Reference);
                    projection = projection with { ClassifiedEvidence = classified, EvidenceReferences = references, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.AuthorityMapped when projection is not null:
                {
                    var authorities = projection.Authorities.ToList();
                    authorities.Add(Read<AuthorityAssignment>(item));
                    projection = projection with { Authorities = authorities, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.AdoptionRouteSet when projection is not null:
                    projection = projection with { AdoptionRoute = Read<AdoptionRoute>(item), LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                case ProposalEventType.ObligationAdded when projection is not null:
                {
                    var obligations = projection.Obligations.ToList();
                    obligations.Add(Read<ImplementationObligation>(item));
                    projection = projection with { Obligations = obligations, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.VerificationRequirementAdded when projection is not null:
                {
                    var requirements = projection.VerificationRequirements.ToList();
                    requirements.Add(Read<VerificationRequirement>(item));
                    projection = projection with { VerificationRequirements = requirements, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.ReviewRequirementSet when projection is not null:
                    projection = projection with { ReviewRequirement = Read<ReviewRequirement>(item), LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                case ProposalEventType.OutcomeRecorded when projection is not null:
                    projection = projection with { LatestOutcome = Read<OutcomeMeasurement>(item), LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                case ProposalEventType.LineageAdded when projection is not null:
                {
                    var lineage = projection.Lineage.ToList();
                    lineage.Add(Read<ProposalLineage>(item));
                    projection = projection with { Lineage = lineage, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.PublicationReviewed when projection is not null:
                {
                    var payload = Read<ProposalLifecycleEvent.PublicationPayload>(item);
                    projection = projection with { PublicReleaseApproved = payload.Approved, PublicSummary = payload.Summary, LastUpdatedAtUtc = item.OccurredAtUtc };
                    break;
                }
            }
        }

        return projection ?? throw new ProposalValidationException("Proposal stream has no registration event.");
    }

    private static T Read<T>(ProposalLifecycleEvent item) =>
        JsonSerializer.Deserialize<T>(item.PayloadJson) ?? throw new ProposalValidationException($"Invalid {item.Type} payload.");
}
