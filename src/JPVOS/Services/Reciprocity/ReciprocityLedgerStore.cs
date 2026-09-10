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
    public bool NonImpositionGateSatisfied { get; init; }
    public string[] EvidenceReferences { get; init; } = [];
}

public sealed class ReciprocityLedgerStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public ReciprocityLedgerStore(string path)
    {
        _path = path;
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(_path))
            File.WriteAllText(_path, JsonSerializer.Serialize(new ReciprocityLedger(), _json));
        _ = Load();
    }

    public ReciprocityEvidence? GetEvidence(string subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId)) return null;

        var entry = Load().Subjects.SingleOrDefault(x =>
            string.Equals(x.SubjectId, subjectId, StringComparison.Ordinal));
        if (entry is null) return null;

        return ToEvidence(entry);
    }

    public async Task UpsertAsync(ReciprocityLedgerEntry entry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entry.SubjectId))
            throw new ArgumentException("Reciprocity subject ID is required.", nameof(entry));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var ledger = Load();
            ledger.Subjects.RemoveAll(x => string.Equals(x.SubjectId, entry.SubjectId, StringComparison.Ordinal));
            ledger.Subjects.Add(entry);
            ledger.Subjects.Sort((left, right) => string.CompareOrdinal(left.SubjectId, right.SubjectId));

            var tempPath = _path + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(ledger, _json), cancellationToken);
            File.Move(tempPath, _path, true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private ReciprocityLedger Load()
    {
        var ledger = JsonSerializer.Deserialize<ReciprocityLedger>(File.ReadAllText(_path), _json)
            ?? throw new InvalidOperationException("Reciprocity ledger is empty or invalid.");

        var duplicate = ledger.Subjects
            .GroupBy(x => x.SubjectId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate reciprocity subject: {duplicate.Key}");

        return ledger;
    }

    private static ReciprocityEvidence ToEvidence(ReciprocityLedgerEntry entry) =>
        new(
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
            NonImpositionGateSatisfied = entry.NonImpositionGateSatisfied,
            EvidenceReferences = entry.EvidenceReferences
        };
}
