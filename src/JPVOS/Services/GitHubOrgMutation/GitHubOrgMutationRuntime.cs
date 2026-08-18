namespace JPVOS.Services.GitHubOrgMutation;

public sealed class GitHubOrgMutationRuntimeState
{
    private readonly object _gate = new();
    public bool Configured { get; private set; }
    public bool CanonicalPolicyLoaded { get; private set; }
    public GitHubOrgReconciliationState? LastReconciliationState { get; private set; }
    public string? LastReceiptId { get; private set; }
    public string? LastError { get; private set; }

    public void MarkConfigured(bool configured) { lock (_gate) Configured = configured; }
    public void Record(GitHubOrgReconciliationResult result)
    {
        lock (_gate)
        {
            CanonicalPolicyLoaded = true;
            LastReconciliationState = result.State;
            LastReceiptId = result.ReceiptId;
            LastError = null;
        }
    }
    public void Fail(Exception exception)
    {
        lock (_gate) LastError = exception.GetType().Name;
    }
}

public sealed class GitHubOrgMutationHostedService : BackgroundService
{
    private static readonly string[] Organizations = ["jaypVLabs", "JayPVentures-LLC"];
    private readonly GitHubOrganizationReconciler _reconciler;
    private readonly GitHubOrgMutationRuntimeState _state;
    private readonly GitHubAppAuthenticationOptions _options;
    private readonly TimeSpan _interval;

    public GitHubOrgMutationHostedService(
        GitHubOrganizationReconciler reconciler,
        GitHubOrgMutationRuntimeState state,
        GitHubAppAuthenticationOptions options,
        IConfiguration configuration)
    {
        _reconciler = reconciler;
        _state = state;
        _options = options;
        var minutes = configuration.GetValue<int?>("JPV_GITHUB_ORG_RECONCILIATION_MINUTES") ?? 15;
        _interval = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 1440));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configured = _options.Validate().Count == 0;
        _state.MarkConfigured(configured);
        if (!configured) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var organization in Organizations)
            {
                try
                {
                    _state.Record(await _reconciler.ReconcileOrganizationAsync(organization, stoppingToken));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (HttpRequestException ex)
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
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
