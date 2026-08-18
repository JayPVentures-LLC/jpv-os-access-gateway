using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JPVOS.Services.GitHubOrgMutation;

namespace JPVOS.Tests;

public sealed class GitHubAppAuthenticationTests
{
    [Fact]
    public void CreateJwt_ProducesRs256TokenWithAppIdIssuer()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportPkcs8PrivateKeyPem();
        var now = new DateTimeOffset(2026, 8, 18, 21, 0, 0, TimeSpan.Zero);

        var jwt = GitHubAppJwtFactory.CreateJwt(12345, pem, now);
        var parts = jwt.Split('.');

        Assert.Equal(3, parts.Length);
        using var header = JsonDocument.Parse(Decode(parts[0]));
        using var payload = JsonDocument.Parse(Decode(parts[1]));
        Assert.Equal("RS256", header.RootElement.GetProperty("alg").GetString());
        Assert.Equal("12345", payload.RootElement.GetProperty("iss").GetString());
        Assert.True(payload.RootElement.GetProperty("exp").GetInt64() > payload.RootElement.GetProperty("iat").GetInt64());
    }

    [Fact]
    public void Options_RequireAppIdPrivateKeyAndBothInstallations()
    {
        var options = new GitHubAppAuthenticationOptions();

        var errors = options.Validate();

        Assert.Contains(errors, x => x.Contains("JPV_GITHUB_APP_ID", StringComparison.Ordinal));
        Assert.Contains(errors, x => x.Contains("JPV_GITHUB_APP_PRIVATE_KEY_PEM", StringComparison.Ordinal));
        Assert.Contains(errors, x => x.Contains("JPV_GITHUB_INSTALLATION_JPVLABS", StringComparison.Ordinal));
        Assert.Contains(errors, x => x.Contains("JPV_GITHUB_INSTALLATION_ENTERPRISE", StringComparison.Ordinal));
    }

    private static byte[] Decode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += normalized.Length % 4 switch { 2 => "==", 3 => "=", _ => string.Empty };
        return Convert.FromBase64String(normalized);
    }
}
