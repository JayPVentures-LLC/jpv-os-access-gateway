using JPVOS.Services.GitHubOrgMutation;

namespace JPVOS.Tests;

public sealed class GitHubOrgMutationPlannerTests
{
    [Fact]
    public void Plan_CreatesParentsBeforeChildren()
    {
        var desired = new GitHubOrganizationTopology(
            "JayPVentures-LLC",
            ["enterprise"],
            ["security"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["security"] = "enterprise"
            });

        var plan = GitHubOrgMutationPlanner.Plan(desired, []);

        Assert.Equal(2, plan.Mutations.Count);
        Assert.Equal(GitHubTeamMutationKind.Create, plan.Mutations[0].Kind);
        Assert.Equal("enterprise", plan.Mutations[0].TeamSlug);
        Assert.Null(plan.Mutations[0].ParentSlug);
        Assert.Equal(GitHubTeamMutationKind.Create, plan.Mutations[1].Kind);
        Assert.Equal("security", plan.Mutations[1].TeamSlug);
        Assert.Equal("enterprise", plan.Mutations[1].ParentSlug);
    }

    [Fact]
    public void Plan_UpdatesIncorrectParentAssignment()
    {
        var desired = new GitHubOrganizationTopology(
            "JayPVentures-LLC",
            ["enterprise", "creator"],
            ["security"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["security"] = "enterprise"
            });
        var observed = new[]
        {
            new GitHubObservedTeam(1, "enterprise", "enterprise", null),
            new GitHubObservedTeam(2, "creator", "creator", null),
            new GitHubObservedTeam(3, "security", "security", "creator")
        };

        var plan = GitHubOrgMutationPlanner.Plan(desired, observed);

        var mutation = Assert.Single(plan.Mutations);
        Assert.Equal(GitHubTeamMutationKind.SetParent, mutation.Kind);
        Assert.Equal("security", mutation.TeamSlug);
        Assert.Equal("enterprise", mutation.ParentSlug);
    }

    [Fact]
    public void Plan_IsNoOpWhenObservedMatchesDesired()
    {
        var desired = new GitHubOrganizationTopology(
            "jaypVLabs",
            ["labs"],
            ["labs-operations"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["labs-operations"] = "labs"
            });
        var observed = new[]
        {
            new GitHubObservedTeam(1, "labs", "labs", null),
            new GitHubObservedTeam(2, "labs-operations", "labs-operations", "labs")
        };

        var plan = GitHubOrgMutationPlanner.Plan(desired, observed);

        Assert.Empty(plan.Mutations);
        Assert.True(plan.IsConverged);
    }

    [Fact]
    public void Plan_RejectsParentMappingOutsideDeclaredTeams()
    {
        var desired = new GitHubOrganizationTopology(
            "JayPVentures-LLC",
            ["enterprise"],
            ["security"],
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["security"] = "missing-parent"
            });

        var error = Assert.Throws<InvalidOperationException>(() => GitHubOrgMutationPlanner.Plan(desired, []));

        Assert.Contains("missing-parent", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
