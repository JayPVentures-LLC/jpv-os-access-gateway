using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JPVOS.Services.GitHubOrgMutation;

public sealed class GitHubAppAuthenticationOptions
{
    public long AppId { get; init; }
    public string PrivateKeyPem { get; init; } = string.Empty;
    public long InstallationJpvLabs { get; init; }
    public long InstallationEnterprise { get; init; }

    public static GitHubAppAuthenticationOptions FromConfiguration(IConfiguration configuration) => new()
    {
        AppId = configuration.GetValue<long?>("JPV_GITHUB_APP_ID") ?? 0,
        PrivateKeyPem = configuration["JPV_GITHUB_APP_PRIVATE_KEY_PEM"] ?? string.Empty,
        InstallationJpvLabs = configuration.GetValue<long?>("JPV_GITHUB_INSTALLATION_JPVLABS") ?? 0,
        InstallationEnterprise = configuration.GetValue<long?>("JPV_GITHUB_INSTALLATION_ENTERPRISE") ?? 0
    };

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (AppId <= 0) errors.Add("JPV_GITHUB_APP_ID is required.");
        if (string.IsNullOrWhiteSpace(PrivateKeyPem)) errors.Add("JPV_GITHUB_APP_PRIVATE_KEY_PEM is required.");
        if (InstallationJpvLabs <= 0) errors.Add("JPV_GITHUB_INSTALLATION_JPVLABS is required.");
        if (InstallationEnterprise <= 0) errors.Add("JPV_GITHUB_INSTALLATION_ENTERPRISE is required.");
        return errors;
    }

    public long InstallationFor(string organization) => organization.ToLowerInvariant() switch
    {
        "jaypvlabs" => InstallationJpvLabs,
        "jaypventures-llc" => InstallationEnterprise,
        _ => throw new InvalidOperationException($"Organization '{organization}' is outside the JPV GitHub installation allowlist.")
    };
}

public static class GitHubAppJwtFactory
{
    public static string CreateJwt(long appId, string privateKeyPem, DateTimeOffset now)
    {
        if (appId <= 0) throw new ArgumentOutOfRangeException(nameof(appId));
        if (string.IsNullOrWhiteSpace(privateKeyPem)) throw new ArgumentException("Private key PEM is required.", nameof(privateKeyPem));

        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iat = now.AddSeconds(-60).ToUnixTimeSeconds(),
            exp = now.AddMinutes(9).ToUnixTimeSeconds(),
            iss = appId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }));
        var unsigned = $"{header}.{payload}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{unsigned}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public interface IGitHubAppTokenProvider
{
    Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken);
}

public sealed class GitHubAppTokenProvider : IGitHubAppTokenProvider
{
    private readonly HttpClient _http;
    private readonly GitHubAppAuthenticationOptions _options;

    public GitHubAppTokenProvider(HttpClient http, GitHubAppAuthenticationOptions options)
    {
        _http = http;
        _options = options;
        _http.BaseAddress ??= new Uri("https://api.github.com/");
    }

    public async Task<string> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken)
    {
        var errors = _options.Validate();
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
        if (installationId <= 0) throw new ArgumentOutOfRangeException(nameof(installationId));

        var jwt = GitHubAppJwtFactory.CreateJwt(_options.AppId, _options.PrivateKeyPem, DateTimeOffset.UtcNow);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"app/installations/{installationId}/access_tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
        request.Headers.UserAgent.ParseAdd("JPV-OS-Access-Gateway/1.0");

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub installation token request failed with HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("token", out var tokenElement) || string.IsNullOrWhiteSpace(tokenElement.GetString()))
        {
            throw new InvalidOperationException("GitHub installation token response did not contain a token.");
        }

        return tokenElement.GetString()!;
    }
}
