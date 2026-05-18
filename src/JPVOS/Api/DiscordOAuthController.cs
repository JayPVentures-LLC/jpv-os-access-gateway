using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

[ApiController]
[Route("api/discord/oauth")]

public class DiscordOAuthController : ControllerBase
{
  private readonly IConfiguration _config;
  private readonly IHttpClientFactory _httpFactory;
  private readonly IEntitlementService _entitlementService;
  private readonly DiscordService _discordService;
  public DiscordOAuthController(IConfiguration config, IHttpClientFactory httpFactory, IEntitlementService entitlementService, DiscordService discordService)
  {
    _config = config;
    _httpFactory = httpFactory;
    _entitlementService = entitlementService;
    _discordService = discordService;
  }

  [HttpGet("connect")]
  public IActionResult Connect(string state)
  {
    var clientId = _config["DISCORD_CLIENT_ID"];
    var redirectUri = _config["DISCORD_REDIRECT_URI"];
    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
    {
      return BadRequest("Discord OAuth configuration is missing.");
    }
    var scope = "identify email guilds.join";
    var url = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scope)}&state={state}";
    return Redirect(url);
  }

  [HttpGet("callback")]
  public async Task<IActionResult> Callback(string code, string state)
  {
    // Validate config
    var clientId = _config["DISCORD_CLIENT_ID"];
    var clientSecret = _config["DISCORD_CLIENT_SECRET"];
    var redirectUri = _config["DISCORD_REDIRECT_URI"];
    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
    {
      return BadRequest("Discord OAuth configuration is missing.");
    }
    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
    {
      return BadRequest("Missing code or state parameter.");
    }
    var http = _httpFactory.CreateClient();
    try
    {
      // Exchange code for token
      using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
      {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
          ["client_id"] = clientId,
          ["client_secret"] = clientSecret,
          ["grant_type"] = "authorization_code",
          ["code"] = code,
          ["redirect_uri"] = redirectUri
        })
      };
      var tokenRes = await http.SendAsync(tokenReq);
      if (!tokenRes.IsSuccessStatusCode)
      {
        return StatusCode((int)tokenRes.StatusCode, "Failed to exchange code for token.");
      }
      var tokenJson = await tokenRes.Content.ReadAsStringAsync();
      using var tokenDoc = JsonDocument.Parse(tokenJson);
      if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenProp))
      {
        return BadRequest("Discord API did not return access_token.");
      }
      var accessToken = accessTokenProp.GetString();
      if (string.IsNullOrWhiteSpace(accessToken))
      {
        return BadRequest("Discord access_token is null or empty.");
      }

      // Fetch Discord user info
      var userReq = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
      userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      var userRes = await http.SendAsync(userReq);
      if (!userRes.IsSuccessStatusCode)
      {
        return StatusCode((int)userRes.StatusCode, "Failed to fetch Discord user info.");
      }
      var userJson = await userRes.Content.ReadAsStringAsync();
      using var userDoc = JsonDocument.Parse(userJson);
      if (!userDoc.RootElement.TryGetProperty("id", out var discordUserIdProp))
      {
        return BadRequest("Discord API did not return user id.");
      }
      var discordUserId = discordUserIdProp.GetString();
      if (string.IsNullOrWhiteSpace(discordUserId))
      {
        return BadRequest("Discord user id is null or empty.");
      }

      // Link Discord user to entitlement (by state = Stripe customer ID)
      var ent = _entitlementService.GetByStripeCustomerId(state);
      if (ent == null)
      {
        return BadRequest("No entitlement found for this state/Stripe customer ID.");
      }
      ent.DiscordUserId = discordUserId;
      // Assign Discord role based on package
      var roleKey = ent.PackageKey?.ToUpperInvariant();
      if (string.IsNullOrWhiteSpace(roleKey))
      {
        return BadRequest("Entitlement package key is missing.");
      }
      var roleId = _config[$"DISCORD_ROLE_{roleKey}"];
      if (string.IsNullOrWhiteSpace(roleId))
      {
        return BadRequest($"No Discord role configured for package: {roleKey}");
      }
      ent.DiscordRole = roleId;
      try
      {
        await _discordService.AssignRoleAsync(discordUserId, roleId);
      }
      catch (Exception ex)
      {
        return StatusCode(502, $"Failed to assign Discord role: {ex.Message}");
      }
      _entitlementService.AddOrUpdate(ent);
      return Redirect("/access");
    }
    catch (Exception)
    {
      // Fail closed: do not leak details, but log if needed
      return StatusCode(500, "Discord OAuth processing failed.");
    }
  }
}
