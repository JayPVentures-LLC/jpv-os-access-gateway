namespace JPVOS.Services.SystemicAccess;

public interface ISystemicAccessInventorySource
{
    Task<IReadOnlyCollection<SystemicAccessRecord>> GetRecordsAsync(CancellationToken cancellationToken);
}

public interface ISystemicAccessActionProvider
{
    bool CanHandle(SystemicAccessRecord record, SystemicAccessDecision decision);
    Task<SystemicAccessActionResult> ApplyAsync(SystemicAccessRecord record, SystemicAccessDecision decision, CancellationToken cancellationToken);
}
