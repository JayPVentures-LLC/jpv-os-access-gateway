using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace JPVOS.Services.GitHubOrgMutation;

public interface IGitHubOrganizationClient
{
    Task<IReadOnlyList<GitHubObservedTeam>> ListTeamsAsync(string organization, CancellationToken cancellationToken);
    Task<GitHubObservedTeam> CreateTeamAsync(string organization, string name, long? parentTeamId, CancellationToken cancellationToken);
    Task<GitHubObservedTeam> SetParentAsync(string organization, string teamSlug, long? parentTeamId, CancellationToken cancellationToken);
}

public sealed class GitHubOrganizationClient : IGitHubOrganizationClient
{
    private readonly HttpClient _http;
    private readonly IGitHubAppTokenProvider _tokens;
    private readonly GitHubAppAuthenticationOptions _options;

    public GitHubOrganizationClient(HttpClient http, IGitHubAppTokenProvider tokens, GitHubAppAuthenticationOptions options)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _http.BaseAddress ??= new Uri("https://api.github.com/");
    }

    public async Task<IReadOnlyList<GitHubObservedTeam>> ListTeamsAsync(string organization, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"orgs/{Uri.EscapeDataString(organization)}/teams?per_page=100", organization, cancellationToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        EnsureSuccess(response, "list teams");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.EnumerateArray().Select(ParseTeam).ToArray();
    }

    public async Task<GitHubObservedTeam> CreateTeamAsync(string organization, string name, long? parentTeamId, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, $"orgs/{Uri.EscapeDataString(organization)}/teams", organization, cancellationToken);
        request.Content = JsonContent.Create(new Dictionary<string, object?>
        {
            ["name"] = name,
            ["privacy"] = "closed",
            ["parent_team_id"] = parentTeamId
        });
        using var response = await _http.SendAsync(request, cancellationToken);
        EnsureSuccess(response, $"create team '{name}'");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParseTeam(document.RootElement);
    }

    public async Task<GitHubObservedTeam> SetParentAsync(string organization, string teamSlug, long? parentTeamId, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Patch, $"orgs/{Uri.EscapeDataString(organization)}/teams/{Uri.EscapeDataString(teamSlug)}", organization, cancellationToken);
        request.Content = JsonContent.Create(new Dictionary<string, object?> { ["parent_team_id"] = parentTeamId });
        using var response = await _http.SendAsync(request, cancellationToken);
        EnsureSuccess(response, $"update team '{teamSlug}'");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParseTeam(document.RootElement);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, string organization, CancellationToken cancellationToken)
    {
        var installationId = _options.InstallationFor(organization);
        var token = await _tokens.GetInstallationTokenAsync(installationId, cancellationToken);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
        request.Headers.UserAgent.ParseAdd("JPV-OS-Access-Gateway/1.0");
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub {operation} failed with HTTP {(int)response.StatusCode}.");
        }
    }

    private static GitHubObservedTeam ParseTeam(JsonElement element)
    {
        string? parentSlug = null;
        if (element.TryGetProperty("parent", out var parent) && parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty("slug", out var slug))
        {
            parentSlug = slug.GetString();
        }

        return new GitHubObservedTeam(
            element.GetProperty("id").GetInt64(),
            element.GetProperty("name").GetString() ?? string.Empty,
            element.GetProperty("slug").GetString() ?? string.Empty,
            parentSlug);
    }
}
