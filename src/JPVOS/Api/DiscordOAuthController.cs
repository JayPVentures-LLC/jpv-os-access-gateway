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
    private readonly EntitlementService _entitlementService;
    private readonly DiscordService _discordService;
    public DiscordOAuthController(IConfiguration config, IHttpClientFactory httpFactory, EntitlementService entitlementService, DiscordService discordService)
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
        var scope = "identify email guilds.join";
        var url = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scope)}&state={state}";
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code, string state)
    {
        var clientId = _config["DISCORD_CLIENT_ID"];
        var clientSecret = _config["DISCORD_CLIENT_SECRET"];
        var redirectUri = _config["DISCORD_REDIRECT_URI"];
        var http = _httpFactory.CreateClient();
        var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
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
        var tokenJson = await tokenRes.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(tokenJson).RootElement;
        var accessToken = token.GetProperty("access_token").GetString();

        // Fetch Discord user info
        var userReq = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userRes = await http.SendAsync(userReq);
        var userJson = await userRes.Content.ReadAsStringAsync();
        var user = JsonDocument.Parse(userJson).RootElement;
        var discordUserId = user.GetProperty("id").GetString();

        // Link Discord user to entitlement (by state = Stripe customer ID)
        var ent = _entitlementService.GetByStripeCustomerId(state);
        if (ent != null)
        {
            ent.DiscordUserId = discordUserId;
            // Assign Discord role based on package
            var roleKey = ent.PackageKey.ToUpperInvariant();
            var roleId = _config[$"DISCORD_ROLE_{roleKey}"];
            if (!string.IsNullOrEmpty(roleId))
            {
                ent.DiscordRole = roleId;
                await _discordService.AssignRoleAsync(discordUserId, roleId);
            }
            _entitlementService.AddOrUpdate(ent);
        }
        return Redirect("/access");
    }
}
