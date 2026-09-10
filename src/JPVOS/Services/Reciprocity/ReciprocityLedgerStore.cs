using System.Text.Json;

namespace JPVOS.Services.Reciprocity;

public sealed class ReciprocityLedger
{
    public List<ReciprocityLedgerEntry> Subjects { get; init; } = [];
}

public sealed class ReciprocityLedgerEntry
{
    public string SubjectId { get; init; } = string.Empty;
    public decimal JpvValueDelivered { get; init; }
    public decimal ReciprocalValueReturned { get; init; }
    public int VerifiedImbalanceObservations { get; init; }
    public bool RemediationOffered { get; init; }
    public bool RestrictionPreviouslyApplied { get; init; }
    public bool ObligationsSatisfied { get; init; }
    public bool IsExempt { get; init; }
    public bool IsMateriallyUncertain { get; init; }
    public string[] EvidenceReferences { get; init; } = [];
}

public sealed class ReciprocityLedgerStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ReciprocityLedgerStore(string path) => _path = path;

    public ReciprocityEvidence? GetEvidence(string subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId) || !File.Exists(_path)) return null;

        var ledger = JsonSerializer.Deserialize<ReciprocityLedger>(File.ReadAllText(_path), _json)
            ?? throw new InvalidOperationException("Reciprocity ledger is empty or invalid.");

        var entry = ledger.Subjects.SingleOrDefault(x =>
            string.Equals(x.SubjectId, subjectId, StringComparison.Ordinal));
        if (entry is null) return null;

        return new ReciprocityEvidence(
            entry.SubjectId,
            entry.JpvValueDelivered,
            entry.ReciprocalValueReturned,
            entry.VerifiedImbalanceObservations,
            entry.RemediationOffered,
            entry.RestrictionPreviouslyApplied,
            entry.ObligationsSatisfied,
            entry.IsExempt,
            entry.IsMateriallyUncertain)
        {
            EvidenceReferences = entry.EvidenceReferences
        };
    }
}
