using System.Security.Cryptography;
using System.Text;

namespace JPVOS.Api;

public static class MachineReadAuth
{
    public const string TokenHashConfigurationKey = "JPV_MCP_CONNOR_READ_TOKEN_SHA256";

    public static bool IsAuthorized(HttpRequest request, IConfiguration configuration)
    {
        var expectedHash = configuration[TokenHashConfigurationKey];
        if (string.IsNullOrWhiteSpace(expectedHash)) return false;

        var authorization = request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token)) return false;

        var providedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        return FixedTimeHexEquals(providedHash, expectedHash.Trim());
    }

    private static bool FixedTimeHexEquals(string left, string right)
    {
        try
        {
            var a = Convert.FromHexString(left);
            var b = Convert.FromHexString(right);
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
