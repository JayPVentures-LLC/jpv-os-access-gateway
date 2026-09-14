namespace JPVOS.Tests;

public sealed class ProposalDeploymentConfigurationTests
{
    [Fact]
    public void Render_uses_repository_root_docker_context_and_bootstrap_is_published()
    {
        var root = FindRepositoryRoot();
        var render = File.ReadAllText(Path.Join(root, "render.yaml"));
        var dockerfile = File.ReadAllText(Path.Join(root, "src", "JPVOS", "Dockerfile"));
        var project = File.ReadAllText(Path.Join(root, "src", "JPVOS", "JPVOS.csproj"));

        Assert.Contains("dockerContext: .", render, StringComparison.Ordinal);
        Assert.Contains("COPY governance/proposals/JPV-PROPOSAL-REGISTRY.bootstrap.json governance/proposals/JPV-PROPOSAL-REGISTRY.bootstrap.json", dockerfile, StringComparison.Ordinal);
        Assert.Contains("JPV-PROPOSAL-REGISTRY.bootstrap.json", project, StringComparison.Ordinal);
    }

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
