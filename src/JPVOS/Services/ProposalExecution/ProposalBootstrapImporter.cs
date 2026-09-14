using System.Text.Json;
using System.Text.Json.Serialization;

namespace JPVOS.Services.ProposalExecution;

public sealed class ProposalBootstrapImporter(IProposalRegistryService registry)
{
    private static readonly IReadOnlyDictionary<ProposalStatus, int> SafeStatusRank = new Dictionary<ProposalStatus, int>
    {
        [ProposalStatus.Researching] = 0,
        [ProposalStatus.Draft] = 1,
        [ProposalStatus.Validated] = 2,
        [ProposalStatus.ApprovedInternally] = 3,
        [ProposalStatus.Submitted] = 4,
        [ProposalStatus.Acknowledged] = 5,
        [ProposalStatus.UnderReview] = 6,
        [ProposalStatus.Adopted] = 7,
        [ProposalStatus.Implementation] = 8,
        [ProposalStatus.Verification] = 9,
        [ProposalStatus.Verified] = 10,
        [ProposalStatus.Rejected] = 11,
        [ProposalStatus.Superseded] = 11,
        [ProposalStatus.Withdrawn] = 11,
        [ProposalStatus.Closed] = 12
    };

    public async Task ImportAsync(string manifestPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(manifestPath))
            throw new ProposalValidationException($"Proposal bootstrap manifest does not exist: {manifestPath}");

        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<BootstrapManifest>(stream, cancellationToken: cancellationToken)
            ?? throw new ProposalValidationException("Proposal bootstrap manifest is empty or invalid.");

        if (!string.Equals(manifest.SchemaVersion, "1.0.0", StringComparison.Ordinal))
            throw new ProposalValidationException("Unsupported proposal bootstrap schema version.");

        foreach (var record in manifest.Records)
            await ImportRecordAsync(record, cancellationToken);
    }

    private async Task ImportRecordAsync(BootstrapRecord record, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.ProposalId) || string.IsNullOrWhiteSpace(record.Title) || string.IsNullOrWhiteSpace(record.TruthNote))
            throw new ProposalValidationException("Bootstrap records require proposal ID, title, and truth note.");
        if (!Enum.TryParse<ProposalClass>(record.Class, ignoreCase: true, out var proposalClass))
            throw new ProposalValidationException($"Unknown proposal class: {record.Class}");
        if (!Enum.TryParse<ProposalLane>(record.Lane, ignoreCase: true, out var lane))
            throw new ProposalValidationException($"Unknown proposal lane: {record.Lane}");
        if (!Enum.TryParse<ProposalStatus>(record.Status, ignoreCase: true, out var targetStatus))
            throw new ProposalValidationException($"Unknown proposal status: {record.Status}");
        if (targetStatus is not (ProposalStatus.Researching or ProposalStatus.Draft or ProposalStatus.Validated))
            throw new ProposalValidationException($"Bootstrap status {targetStatus} asserts progress that requires runtime evidence.");

        var current = await registry.RegisterAsync(
            record.ProposalId,
            record.Title,
            proposalClass,
            lane,
            $"bootstrap:{record.ProposalId}:register",
            cancellationToken);

        if (SafeStatusRank[current.Status] >= SafeStatusRank[targetStatus]) return;

        await registry.RecordStatusAsync(
            record.ProposalId,
            targetStatus,
            evidenceReference: null,
            authorityReference: null,
            idempotencyKey: $"bootstrap:{record.ProposalId}:status:{targetStatus}",
            cancellationToken);
    }

    private sealed record BootstrapManifest(
        [property: JsonPropertyName("schema_version")] string SchemaVersion,
        [property: JsonPropertyName("records")] IReadOnlyList<BootstrapRecord> Records);

    private sealed record BootstrapRecord(
        [property: JsonPropertyName("proposal_id")] string ProposalId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("class")] string Class,
        [property: JsonPropertyName("lane")] string Lane,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("truth_note")] string TruthNote);
}
