using System.Text.Json.Serialization;

namespace JPVOS.Services.GitHubOrgMutation;

public sealed record GitHubOrganizationTopology(
    string Organization,
    IReadOnlyList<string> ParentTeams,
    IReadOnlyList<string> LeafTeams,
    IReadOnlyDictionary<string, string> ParentByTeam);

public sealed record GitHubObservedTeam(
    long Id,
    string Name,
    string Slug,
    string? ParentSlug);

public enum GitHubTeamMutationKind
{
    Create,
    SetParent
}

public sealed record GitHubTeamMutation(
    GitHubTeamMutationKind Kind,
    string TeamSlug,
    string? ParentSlug);

public sealed record GitHubReconciliationPlan(
    string Organization,
    IReadOnlyList<GitHubTeamMutation> Mutations)
{
    public bool IsConverged => Mutations.Count == 0;
}

public enum GitHubOrgReconciliationState
{
    Verified,
    Drifted,
    AccessReconciliationRequired,
    AuthorityRequired,
    BlockedByProviderCapability,
    Failed
}

public sealed record GitHubOrgReconciliationResult(
    string ReceiptId,
    string Organization,
    GitHubOrgReconciliationState State,
    int AppliedMutations,
    IReadOnlyList<GitHubTeamMutation> RemainingMutations,
    DateTimeOffset CompletedAtUtc);

public sealed class GitHubTopology
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("authority")]
    public string Authority { get; init; } = string.Empty;

    [JsonPropertyName("organizations")]
    public Dictionary<string, GitHubTopologyOrganizationDocument> Organizations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GitHubTopologyOrganizationDocument
{
    [JsonPropertyName("parent_teams")]
    public List<string> ParentTeams { get; init; } = [];

    [JsonPropertyName("leaf_teams")]
    public List<string> LeafTeams { get; init; } = [];

    [JsonPropertyName("parent_by_team")]
    public Dictionary<string, string> ParentByTeam { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public GitHubOrganizationTopology ToTopology(string organization) =>
        new(organization, ParentTeams, LeafTeams, ParentByTeam);
}
