using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JPVOS.Services.SystemicAccess;

public sealed class SystemicAccessAuditStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SystemicAccessAuditStore(string path) => _path = path;

    public async Task AppendAsync(SystemicAccessRecord record, SystemicAccessDecision decision, bool applied, string result, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var receipt = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            resourceType = record.ResourceType,
            resourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.ResourceId))),
            priorState = record.Status,
            evidence = record.Evidence,
            action = decision.Action,
            reason = decision.Reason,
            applied,
            result
        };

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_path, JsonSerializer.Serialize(receipt) + Environment.NewLine, cancellationToken);
        }
        finally { _gate.Release(); }
    }
}
