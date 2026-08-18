using System.Text.Json;

namespace JPVOS.Services.GitHubOrgMutation;

public sealed class GitHubOrganizationReconciler
{
    private readonly IGitHubCanonicalTopologySource _source;
    private readonly IGitHubOrganizationClient _client;
    private readonly GitHubOrgMutationReceiptStore _receipts;

    public GitHubOrganizationReconciler(
        IGitHubCanonicalTopologySource source,
        IGitHubOrganizationClient client,
        GitHubOrgMutationReceiptStore receipts)
    {
        _source = source;
        _client = client;
        _receipts = receipts;
    }

    public async Task<GitHubOrgReconciliationResult> ReconcileOrganizationAsync(string organization, CancellationToken cancellationToken)
    {
        var topology = await _source.LoadAsync(cancellationToken);
        if (!topology.Organizations.TryGetValue(organization, out var document))
        {
            throw new InvalidOperationException($"Organization '{organization}' is not present in canonical GitHub topology.");
        }

        var desired = document.ToTopology(organization);
        var observed = await _client.ListTeamsAsync(organization, cancellationToken);
        var plan = GitHubOrgMutationPlanner.Plan(desired, observed);
        var bySlug = observed.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
        var applied = 0;

        foreach (var mutation in plan.Mutations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long? parentId = null;
            if (!string.IsNullOrWhiteSpace(mutation.ParentSlug))
            {
                if (!bySlug.TryGetValue(mutation.ParentSlug, out var parent))
                    throw new InvalidOperationException($"Required parent team '{mutation.ParentSlug}' is not available before '{mutation.TeamSlug}'.");
                parentId = parent.Id;
            }

            GitHubObservedTeam result = mutation.Kind switch
            {
                GitHubTeamMutationKind.Create => await _client.CreateTeamAsync(organization, mutation.TeamSlug, parentId, cancellationToken),
                GitHubTeamMutationKind.SetParent => await _client.SetParentAsync(organization, mutation.TeamSlug, parentId, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported GitHub team mutation '{mutation.Kind}'.")
            };
            bySlug[result.Slug] = result;
            applied++;
        }

        var readback = await _client.ListTeamsAsync(organization, cancellationToken);
        var remaining = GitHubOrgMutationPlanner.Plan(desired, readback).Mutations;
        var state = remaining.Count == 0 ? GitHubOrgReconciliationState.Verified : GitHubOrgReconciliationState.Drifted;
        var resultReceipt = new GitHubOrgReconciliationResult(
            Guid.NewGuid().ToString("N"),
            organization,
            state,
            applied,
            remaining,
            DateTimeOffset.UtcNow);
        await _receipts.AppendAsync(resultReceipt, topology.SchemaVersion, cancellationToken);
        return resultReceipt;
    }
}

public sealed class GitHubOrgMutationReceiptStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GitHubOrgMutationReceiptStore(string path) => _path = path;

    public async Task AppendAsync(GitHubOrgReconciliationResult result, string topologyVersion, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var record = new
        {
            receipt_id = result.ReceiptId,
            canonical_authority = "jaypVLabs/JPV-OS",
            topology_version = topologyVersion,
            organization = result.Organization,
            state = result.State.ToString().ToUpperInvariant(),
            applied_mutations = result.AppliedMutations,
            remaining_mutations = result.RemainingMutations,
            completed_at_utc = result.CompletedAtUtc
        };
        var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(_path, line, cancellationToken);
    }
}
