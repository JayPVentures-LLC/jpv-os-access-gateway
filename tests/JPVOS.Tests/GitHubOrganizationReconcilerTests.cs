using JPVOS.Services.GitHubOrgMutation;

namespace JPVOS.Tests;

public sealed class GitHubOrganizationReconcilerTests
{
    [Fact]
    public async Task ReconcileOrganizationAsync_VerifiesOnlyAfterReadbackMatchesDesired()
    {
        var source = new FakeTopologySource(new GitHubTopology
        {
            SchemaVersion = "1.1.0",
            Status = "CANONICAL_DESIRED_STATE",
            Authority = "jaypVLabs/JPV-OS",
            Organizations = new(StringComparer.OrdinalIgnoreCase)
            {
                ["JayPVentures-LLC"] = new GitHubTopologyOrganizationDocument
                {
                    ParentTeams = ["enterprise"],
                    LeafTeams = ["security"],
                    ParentByTeam = new(StringComparer.OrdinalIgnoreCase) { ["security"] = "enterprise" }
                }
            }
        });
        var client = new StatefulFakeClient();
        var receiptPath = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        var receipts = new GitHubOrgMutationReceiptStore(receiptPath);
        var reconciler = new GitHubOrganizationReconciler(source, client, receipts);

        try
        {
            var result = await reconciler.ReconcileOrganizationAsync("JayPVentures-LLC", CancellationToken.None);

            Assert.Equal(GitHubOrgReconciliationState.Verified, result.State);
            Assert.Equal(2, result.AppliedMutations);
            Assert.Equal(2, client.Teams.Count);
            Assert.Single(File.ReadAllLines(receiptPath));
        }
        finally
        {
            if (File.Exists(receiptPath)) File.Delete(receiptPath);
        }
    }

    [Fact]
    public async Task ReconcileOrganizationAsync_ReturnsDriftedWhenProviderReadbackStillDiffers()
    {
        var source = new FakeTopologySource(new GitHubTopology
        {
            SchemaVersion = "1.1.0",
            Status = "CANONICAL_DESIRED_STATE",
            Authority = "jaypVLabs/JPV-OS",
            Organizations = new(StringComparer.OrdinalIgnoreCase)
            {
                ["jaypVLabs"] = new GitHubTopologyOrganizationDocument
                {
                    ParentTeams = ["labs"],
                    LeafTeams = ["labs-operations"],
                    ParentByTeam = new(StringComparer.OrdinalIgnoreCase) { ["labs-operations"] = "labs" }
                }
            }
        });
        var client = new NonPersistingFakeClient();
        var receiptPath = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".jsonl");
        var reconciler = new GitHubOrganizationReconciler(source, client, new GitHubOrgMutationReceiptStore(receiptPath));

        try
        {
            var result = await reconciler.ReconcileOrganizationAsync("jaypVLabs", CancellationToken.None);

            Assert.Equal(GitHubOrgReconciliationState.Drifted, result.State);
            Assert.NotEmpty(result.RemainingMutations);
        }
        finally
        {
            if (File.Exists(receiptPath)) File.Delete(receiptPath);
        }
    }

    private sealed class FakeTopologySource(GitHubTopology topology) : IGitHubCanonicalTopologySource
    {
        public Task<GitHubTopology> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(topology);
    }

    private sealed class StatefulFakeClient : IGitHubOrganizationClient
    {
        private long _nextId = 1;
        public List<GitHubObservedTeam> Teams { get; } = [];

        public Task<IReadOnlyList<GitHubObservedTeam>> ListTeamsAsync(string organization, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GitHubObservedTeam>>(Teams.ToArray());

        public Task<GitHubObservedTeam> CreateTeamAsync(string organization, string name, long? parentTeamId, CancellationToken cancellationToken)
        {
            var parent = parentTeamId is null ? null : Teams.Single(x => x.Id == parentTeamId).Slug;
            var team = new GitHubObservedTeam(_nextId++, name, name, parent);
            Teams.Add(team);
            return Task.FromResult(team);
        }

        public Task<GitHubObservedTeam> SetParentAsync(string organization, string teamSlug, long? parentTeamId, CancellationToken cancellationToken)
        {
            var index = Teams.FindIndex(x => string.Equals(x.Slug, teamSlug, StringComparison.OrdinalIgnoreCase));
            var parent = parentTeamId is null ? null : Teams.Single(x => x.Id == parentTeamId).Slug;
            Teams[index] = Teams[index] with { ParentSlug = parent };
            return Task.FromResult(Teams[index]);
        }
    }

    private sealed class NonPersistingFakeClient : IGitHubOrganizationClient
    {
        public Task<IReadOnlyList<GitHubObservedTeam>> ListTeamsAsync(string organization, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GitHubObservedTeam>>([]);

        public Task<GitHubObservedTeam> CreateTeamAsync(string organization, string name, long? parentTeamId, CancellationToken cancellationToken) =>
            Task.FromResult(new GitHubObservedTeam(1, name, name, null));

        public Task<GitHubObservedTeam> SetParentAsync(string organization, string teamSlug, long? parentTeamId, CancellationToken cancellationToken) =>
            Task.FromResult(new GitHubObservedTeam(1, teamSlug, teamSlug, null));
    }
}
