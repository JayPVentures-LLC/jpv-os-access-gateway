namespace JPVOS.Services.GitHubOrgMutation;

public static class GitHubOrgMutationPlanner
{
    public static GitHubReconciliationPlan Plan(
        GitHubOrganizationTopology desired,
        IReadOnlyCollection<GitHubObservedTeam> observed)
    {
        Validate(desired);

        var observedBySlug = observed.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
        var mutations = new List<GitHubTeamMutation>();

        foreach (var parent in desired.ParentTeams)
        {
            if (!observedBySlug.ContainsKey(parent))
            {
                mutations.Add(new GitHubTeamMutation(GitHubTeamMutationKind.Create, parent, null));
            }
        }

        foreach (var leaf in desired.LeafTeams)
        {
            desired.ParentByTeam.TryGetValue(leaf, out var desiredParent);
            if (!observedBySlug.TryGetValue(leaf, out var current))
            {
                mutations.Add(new GitHubTeamMutation(GitHubTeamMutationKind.Create, leaf, desiredParent));
                continue;
            }

            if (!string.Equals(current.ParentSlug, desiredParent, StringComparison.OrdinalIgnoreCase))
            {
                mutations.Add(new GitHubTeamMutation(GitHubTeamMutationKind.SetParent, leaf, desiredParent));
            }
        }

        return new GitHubReconciliationPlan(desired.Organization, mutations);
    }

    private static void Validate(GitHubOrganizationTopology desired)
    {
        if (string.IsNullOrWhiteSpace(desired.Organization))
        {
            throw new InvalidOperationException("Organization is required.");
        }

        var declared = desired.ParentTeams
            .Concat(desired.LeafTeams)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (child, parent) in desired.ParentByTeam)
        {
            if (!declared.Contains(child))
            {
                throw new InvalidOperationException($"Parent mapping child '{child}' is not a declared team.");
            }

            if (!declared.Contains(parent))
            {
                throw new InvalidOperationException($"Parent mapping references undeclared parent '{parent}'.");
            }

            if (string.Equals(child, parent, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Team '{child}' cannot be its own parent.");
            }
        }
    }
}
