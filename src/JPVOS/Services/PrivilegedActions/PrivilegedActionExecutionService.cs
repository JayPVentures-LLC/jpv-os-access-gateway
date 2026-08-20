using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JPVOS.Services.PrivilegedActions;

public sealed record PrivilegedProviderResult(bool Success, string ResultCode, string State);

public interface IPrivilegedActionProvider
{
    Task<PrivilegedProviderResult> ExecuteAsync(PrivilegedActionRequest request, CancellationToken cancellationToken);
    Task<PrivilegedProviderResult> ReadBackAsync(PrivilegedActionRequest request, CancellationToken cancellationToken);
}

public sealed class PrivilegedActionAuditStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _previousReceiptHash;

    public PrivilegedActionAuditStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
    }

    public async Task AppendAsync(PrivilegedActionReceipt receipt, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var linked = receipt with { PreviousReceiptHash = _previousReceiptHash };
            var json = JsonSerializer.Serialize(linked);
            await File.AppendAllTextAsync(_path, json + Environment.NewLine, cancellationToken);
            _previousReceiptHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        }
        finally
        {
            _mutex.Release();
        }
    }
}

public sealed record PrivilegedExecutionOutcome(string TerminalStatus, string ReasonCode, PrivilegedActionReceipt? Receipt);

public sealed class PrivilegedActionExecutionService
{
    private readonly PrivilegedActionAuthorizer _authorizer;
    private readonly PrivilegedActionAuditStore _auditStore;

    public PrivilegedActionExecutionService(
        PrivilegedActionAuthorizer authorizer,
        PrivilegedActionAuditStore auditStore)
    {
        _authorizer = authorizer;
        _auditStore = auditStore;
    }

    public async Task<PrivilegedExecutionOutcome> ExecuteAsync(
        PrivilegedActionRequest request,
        AuthenticationEvidence authentication,
        IPrivilegedActionProvider provider,
        DateTimeOffset nowUtc,
        BreakGlassGrant? breakGlassGrant = null,
        CancellationToken cancellationToken = default)
    {
        var decision = _authorizer.Authorize(request, authentication, nowUtc, breakGlassGrant);
        if (decision.Decision != PrivilegedDecisionKind.Allow)
            return new PrivilegedExecutionOutcome("DENIED", decision.ReasonCode, null);

        var execution = await provider.ExecuteAsync(request, cancellationToken);
        var observed = execution.Success
            ? await provider.ReadBackAsync(request, cancellationToken)
            : new PrivilegedProviderResult(false, "EXECUTION_FAILED", string.Empty);

        var terminal = !execution.Success
            ? "FAILED"
            : !observed.Success || !string.Equals(observed.State, request.DesiredState ?? observed.State, StringComparison.Ordinal)
                ? "DEGRADED"
                : "PASS";

        var receipt = new PrivilegedActionReceipt(
            Guid.NewGuid().ToString("N"),
            request.ActorSubject,
            request.Action,
            request.Resource,
            request.RiskClass,
            authentication.PhishingResistant ? "PHISHING_RESISTANT" : "SESSION",
            nowUtc,
            request.DesiredState ?? string.Empty,
            execution.ResultCode,
            observed.State,
            terminal,
            null);

        await _auditStore.AppendAsync(receipt, cancellationToken);
        return new PrivilegedExecutionOutcome(terminal, terminal, receipt);
    }
}
