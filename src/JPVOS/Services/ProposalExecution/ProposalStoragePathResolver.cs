namespace JPVOS.Services.ProposalExecution;

public static class ProposalStoragePathResolver
{
    public static string Resolve(string? explicitProposalDirectory, string? sharedPersistentRoot, bool isDevelopment)
    {
        if (!string.IsNullOrWhiteSpace(explicitProposalDirectory)) return explicitProposalDirectory;
        if (!string.IsNullOrWhiteSpace(sharedPersistentRoot)) return Path.Join(sharedPersistentRoot, "proposals");
        if (isDevelopment) return Path.Join(Path.GetTempPath(), "jpv-os-proposals");

        throw new InvalidOperationException(
            "Persistent proposal storage is required outside Development. Configure JPV_PROPOSAL_DATA_DIR or JPV_OUTBOUND_DATA_DIR.");
    }
}
