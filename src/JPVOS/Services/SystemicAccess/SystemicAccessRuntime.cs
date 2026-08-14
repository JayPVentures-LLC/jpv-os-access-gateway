using Microsoft.Data.Sqlite;

namespace JPVOS.Services.SystemicAccess;

public sealed class SystemicAccessRuntimeState
{
    private readonly object _gate = new();
    public bool PolicyLoaded { get; private set; }
    public SystemicAccessReconciliationSummary? LastSummary { get; private set; }
    public string? LastError { get; private set; }

    public void MarkPolicyLoaded() { lock (_gate) PolicyLoaded = true; }
    public void Record(SystemicAccessReconciliationSummary summary) { lock (_gate) { LastSummary = summary; LastError = null; } }
    public void Fail(Exception ex) { lock (_gate) LastError = ex.GetType().Name; }
}

public sealed class SystemicAccessReconciliationService : BackgroundService
{
    private readonly SystemicAccessReconciler _reconciler;
    private readonly SystemicAccessRuntimeState _state;
    private readonly TimeSpan _interval;

    public SystemicAccessReconciliationService(SystemicAccessReconciler reconciler, SystemicAccessRuntimeState state, IConfiguration configuration)
    {
        _reconciler = reconciler;
        _state = state;
        var minutes = configuration.GetValue<int?>("JPV_SYSTEMIC_ACCESS_RECONCILIATION_MINUTES") ?? 15;
        _interval = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 1440));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _state.Record(await _reconciler.RunOnceAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (SqliteException ex)
            {
                _state.Fail(ex);
            }
            catch (IOException ex)
            {
                _state.Fail(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _state.Fail(ex);
            }
            catch (InvalidOperationException ex)
            {
                _state.Fail(ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
