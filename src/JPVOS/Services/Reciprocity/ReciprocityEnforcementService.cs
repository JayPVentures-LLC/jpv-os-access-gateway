using System.Text.Json;

namespace JPVOS.Services.Reciprocity;

public sealed class ReciprocityAuditStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ReciprocityAuditStore(string path) => _path = path;

    public async Task AppendAsync(ReciprocityAuditReceipt receipt, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(receipt) + Environment.NewLine;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_path, line, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}

public sealed class ReciprocityEnforcementService
{
    private readonly ReciprocityEvaluator _evaluator;
    private readonly ReciprocityAdmissionGate _gate;
    private readonly ReciprocityAuditStore _audit;

    public ReciprocityEnforcementService(
        ReciprocityEvaluator evaluator,
        ReciprocityAdmissionGate gate,
        ReciprocityAuditStore audit)
    {
        _evaluator = evaluator;
        _gate = gate;
        _audit = audit;
    }

    public async Task<ReciprocityAdmissionDecision> EvaluateAsync(
        ReciprocityEvidence evidence,
        ReciprocityAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(evidence.SubjectId, request.SubjectId, StringComparison.Ordinal))
            throw new InvalidOperationException("Reciprocity subject mismatch.");

        var evaluation = _evaluator.Evaluate(evidence);
        var decision = _gate.Decide(request, evaluation);
        await _audit.AppendAsync(new ReciprocityAuditReceipt(
            request.SubjectId,
            request.ResourceId,
            decision.State,
            decision.Allowed,
            decision.ReasonCode,
            DateTimeOffset.UtcNow), cancellationToken);
        return decision;
    }
}
