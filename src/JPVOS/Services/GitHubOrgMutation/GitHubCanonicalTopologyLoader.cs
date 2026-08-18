using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JPVOS.Services.GitHubOrgMutation;

public interface IGitHubCanonicalTopologySource
{
    Task<GitHubTopology> LoadAsync(CancellationToken cancellationToken);
}

public sealed class GitHubCanonicalTopologyLoader : IGitHubCanonicalTopologySource
{
    private const string CanonicalRepository = "jaypVLabs/JPV-OS";
    private const string CanonicalPath = "governance/platform/github-team-topology.v1.json";
    private readonly HttpClient _http;
    private readonly IGitHubAppTokenProvider _tokens;
    private readonly GitHubAppAuthenticationOptions _options;

    public GitHubCanonicalTopologyLoader(HttpClient http, IGitHubAppTokenProvider tokens, GitHubAppAuthenticationOptions options)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _http.BaseAddress ??= new Uri("https://api.github.com/");
    }

    public async Task<GitHubTopology> LoadAsync(CancellationToken cancellationToken)
    {
        var token = await _tokens.GetInstallationTokenAsync(_options.InstallationJpvLabs, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{CanonicalRepository}/contents/{CanonicalPath}?ref=main");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
        request.Headers.UserAgent.ParseAdd("JPV-OS-Access-Gateway/1.0");

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Canonical GitHub topology retrieval failed with HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var envelope = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!envelope.RootElement.TryGetProperty("content", out var contentElement))
        {
            throw new InvalidOperationException("Canonical GitHub topology response did not contain file content.");
        }

        var encoded = contentElement.GetString()?.Replace("\n", string.Empty, StringComparison.Ordinal) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(encoded)) throw new InvalidOperationException("Canonical GitHub topology content is empty.");

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var topology = JsonSerializer.Deserialize<GitHubTopology>(json) ?? throw new InvalidOperationException("Canonical GitHub topology could not be parsed.");

        if (!string.Equals(topology.Authority, CanonicalRepository, StringComparison.Ordinal))
            throw new InvalidOperationException($"Canonical topology authority '{topology.Authority}' is invalid.");
        if (!string.Equals(topology.Status, "CANONICAL_DESIRED_STATE", StringComparison.Ordinal))
            throw new InvalidOperationException($"Canonical topology status '{topology.Status}' is not executable.");
        if (topology.Organizations.Count == 0)
            throw new InvalidOperationException("Canonical GitHub topology contains no organizations.");

        return topology;
    }
}
