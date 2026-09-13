using System.Text.Json;

namespace JPVOS.Services.ProposalExecution;

public static class ProposalProjector
{
    public static ProposalProjection Project(IEnumerable<ProposalLifecycleEvent> events)
    {
        ProposalProjection? projection = null;

        foreach (var @event in events.OrderBy(x => x.Sequence))
        {
            switch (@event.Type)
            {
                case ProposalEventType.Registered:
                {
                    var payload = JsonSerializer.Deserialize<ProposalLifecycleEvent.RegistrationPayload>(@event.PayloadJson)
                                  ?? throw new ProposalValidationException("Invalid registration payload.");
                    projection = ProposalProjection.New(@event.ProposalId, payload.Title, payload.Class, payload.Lane)
                        with { LastUpdatedAtUtc = @event.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.StatusChanged when projection is not null:
                {
                    var payload = JsonSerializer.Deserialize<ProposalLifecycleEvent.StatusPayload>(@event.PayloadJson)
                                  ?? throw new ProposalValidationException("Invalid status payload.");
                    var evidence = projection.EvidenceReferences.ToList();
                    if (!string.IsNullOrWhiteSpace(payload.EvidenceReference) && !evidence.Contains(payload.EvidenceReference)) evidence.Add(payload.EvidenceReference);
                    var authorities = projection.Authorities.ToList();
                    if (!string.IsNullOrWhiteSpace(payload.AuthorityReference) && !authorities.Any(x => x.AuthorityReference == payload.AuthorityReference))
                        authorities.Add(new AuthorityAssignment("decision-authority", payload.AuthorityReference, payload.EvidenceReference));
                    projection = projection with { Status = payload.Status, EvidenceReferences = evidence, Authorities = authorities, LastUpdatedAtUtc = @event.OccurredAtUtc };
                    break;
                }
                case ProposalEventType.PublicationReviewed when projection is not null:
                {
                    var payload = JsonSerializer.Deserialize<ProposalLifecycleEvent.PublicationPayload>(@event.PayloadJson)
                                  ?? throw new ProposalValidationException("Invalid publication payload.");
                    projection = projection with { PublicReleaseApproved = payload.Approved, PublicSummary = payload.Summary, LastUpdatedAtUtc = @event.OccurredAtUtc };
                    break;
                }
            }
        }

        return projection ?? throw new ProposalValidationException("Proposal stream has no registration event.");
    }
}
