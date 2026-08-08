using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JPVOS.Api;

[ApiController]
[Route("api/auth")]
public sealed class FounderAuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public FounderAuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    [EnableRateLimiting("FounderLogin")]
    public async Task<IActionResult> Login([FromForm] string email, [FromForm] string accessKey, [FromForm] string? returnUrl = null)
    {
        if (!IsSameOriginRequest())
        {
            return Forbid();
        }

        var founderEmail = _config["JPV_FOUNDER_EMAIL"];
        var expectedHash = _config["JPV_FOUNDER_ACCESS_KEY_SHA256"];
        if (string.IsNullOrWhiteSpace(founderEmail) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Founder identity is not provisioned.");
        }

        var providedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessKey ?? string.Empty)));
        var emailMatches = string.Equals(email?.Trim(), founderEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        var keyMatches = FixedTimeHexEquals(providedHash, expectedHash.Trim());
        if (!emailMatches || !keyMatches)
        {
            return Redirect("/login?error=1");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "founder"),
            new Claim(ClaimTypes.Name, founderEmail),
            new Claim(ClaimTypes.Email, founderEmail),
            new Claim(ClaimTypes.Role, "Founder"),
            new Claim("jpv_identity_tier", "founder"),
            new Claim("jpv_authority", "enterprise-infrastructure-authority")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

        var destination = IsLocalPath(returnUrl) ? returnUrl! : "/profile";
        return LocalRedirect(destination);
    }

    [HttpPost("logout")]
    [EnableRateLimiting("FounderLogin")]
    public async Task<IActionResult> Logout()
    {
        if (!IsSameOriginRequest())
        {
            return Forbid();
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/login");
    }

    private bool IsSameOriginRequest()
    {
        var origin = Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return string.Equals(originUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase);
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            return string.Equals(refererUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsLocalPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//");

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
