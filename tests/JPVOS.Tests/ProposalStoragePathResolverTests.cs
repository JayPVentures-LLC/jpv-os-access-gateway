using JPVOS.Services.ProposalExecution;

namespace JPVOS.Tests;

public sealed class ProposalStoragePathResolverTests
{
    [Fact]
    public void Explicit_proposal_directory_wins()
    {
        var resolved = ProposalStoragePathResolver.Resolve("/explicit/proposals", "/shared", isDevelopment: false);
        Assert.Equal("/explicit/proposals", resolved);
    }

    [Fact]
    public void Production_uses_proposals_subdirectory_of_shared_persistent_root()
    {
        var resolved = ProposalStoragePathResolver.Resolve(null, "/var/data/jpv", isDevelopment: false);
        Assert.Equal(Path.Join("/var/data/jpv", "proposals"), resolved);
    }

    [Fact]
    public void Production_without_any_persistent_root_fails_closed()
    {
        Assert.Throws<InvalidOperationException>(() => ProposalStoragePathResolver.Resolve(null, null, isDevelopment: false));
    }

    [Fact]
    public void Development_can_use_temporary_storage()
    {
        var resolved = ProposalStoragePathResolver.Resolve(null, null, isDevelopment: true);
        Assert.Equal(Path.Join(Path.GetTempPath(), "jpv-os-proposals"), resolved);
    }
}
