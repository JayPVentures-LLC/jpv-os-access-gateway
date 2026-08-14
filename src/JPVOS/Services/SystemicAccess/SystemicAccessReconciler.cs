namespace JPVOS.Services.SystemicAccess;

public sealed class SystemicAccessReconciler
{
    private readonly IEnumerable<ISystemicAccessInventorySource> _sources;
    private readonly IEnumerable<ISystemicAccessActionProvider> _providers;
    private readonly SystemicAccessClassifier _classifier;
    private readonly SystemicAccessAuditStore _audit;

    public SystemicAccessReconciler(
        IEnumerable<ISystemicAccessInventorySource> sources,
        IEnumerable<ISystemicAccessActionProvider> providers,
        SystemicAccessClassifier classifier,
        SystemicAccessAuditStore audit)
    {
        _sources = sources;
        _providers = providers;
        _classifier = classifier;
        _audit = audit;
    }

    public async Task<SystemicAccessReconciliationSummary> RunOnceAsync(CancellationToken cancellationToken)
    {
        var evaluated = 0;
        var applied = 0;
        var failures = 0;

        foreach (var source in _sources)
        {
            var records = await source.GetRecordsAsync(cancellationToken);
            foreach (var record in records)
            {
                evaluated++;
                var decision = _classifier.Classify(record);
                var provider = _providers.FirstOrDefault(p => p.CanHandle(record, decision));

                if (provider is null)
                {
                    await _audit.AppendAsync(record, decision, false, "no_action_required_or_provider", cancellationToken);
                    continue;
                }

                try
                {
                    var result = await provider.ApplyAsync(record, decision, cancellationToken);
                    if (result.Applied) applied++;
                    await _audit.AppendAsync(record, decision, result.Applied, result.Result, cancellationToken);
                }
                catch (Exception ex)
                {
                    failures++;
                    await _audit.AppendAsync(record, decision, false, "provider_failure:" + ex.GetType().Name, cancellationToken);
                }
            }
        }

        return new SystemicAccessReconciliationSummary(evaluated, applied, failures, DateTimeOffset.UtcNow);
    }
}
