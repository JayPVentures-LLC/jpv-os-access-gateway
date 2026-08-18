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

public sealed class GitHubTopology
{
    public string SchemaVersion { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Authority { get; init; } = string.Empty;
    public Dictionary<string, GitHubTopologyOrganizationDocument> Organizations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GitHubTopologyOrganizationDocument
{
    public List<string> ParentTeams { get; init; } = [];
    public List<string> LeafTeams { get; init; } = [];
    public Dictionary<string, string> ParentByTeam { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public GitHubOrganizationTopology ToTopology(string organization) =>
        new(organization, ParentTeams, LeafTeams, ParentByTeam);
}
