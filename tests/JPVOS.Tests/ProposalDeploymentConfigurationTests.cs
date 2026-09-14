namespace JPVOS.Tests;

public sealed class ProposalDeploymentConfigurationTests
{
    [Fact]
    public void Production_wires_persistent_proposal_storage_and_bootstrap_once()
    {
        var root = FindRepositoryRoot();
        var render = File.ReadAllText(Path.Join(root, "render.yaml"));
        var dockerfile = File.ReadAllText(Path.Join(root, "src", "JPVOS", "Dockerfile"));
        var project = File.ReadAllText(Path.Join(root, "src", "JPVOS", "JPVOS.csproj"));
        var program = File.ReadAllText(Path.Join(root, "src", "JPVOS", "Program.cs"));

        Assert.Contains("dockerContext: .", render, StringComparison.Ordinal);
        Assert.Contains("COPY governance/proposals/JPV-PROPOSAL-REGISTRY.bootstrap.json governance/proposals/JPV-PROPOSAL-REGISTRY.bootstrap.json", dockerfile, StringComparison.Ordinal);
        Assert.Contains("JPV-PROPOSAL-REGISTRY.bootstrap.json", project, StringComparison.Ordinal);
        Assert.Contains("ProposalStoragePathResolver.Resolve", program, StringComparison.Ordinal);
        Assert.Contains("ProposalBootstrapImporter", program, StringComparison.Ordinal);
        Assert.Contains("ImportAsync", program, StringComparison.Ordinal);
        Assert.Equal(1, Count(program, "AddHostedService<SystemicAccessReconciliationService>()"));
        Assert.Equal(1, Count(program, "AddHostedService<GitHubOrgMutationHostedService>()"));
    }

    private static int Count(string body, string value) =>
        body.Split(value, StringSplitOptions.None).Length - 1;

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Join(current.FullName, "render.yaml"))) return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root containing render.yaml was not found.");
    }
}
