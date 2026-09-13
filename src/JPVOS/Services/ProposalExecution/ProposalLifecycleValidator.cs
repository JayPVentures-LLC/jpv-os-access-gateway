namespace JPVOS.Services.ProposalExecution;

public sealed class ProposalLifecycleValidator
{
    public bool IsKnownStatus(string value) => !string.IsNullOrWhiteSpace(value);
}
