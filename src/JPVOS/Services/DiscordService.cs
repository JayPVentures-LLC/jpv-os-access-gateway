using System.Net.Http.Headers;
using System.Text.Json;
using JPVOS.Infrastructure.Discord;

public class DiscordService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordService> _logger;
    private readonly DiscordRoleSyncAuditStore _auditStore;

    public DiscordService(
        HttpClient http,
        IConfiguration config,
        ILogger<DiscordService> logger,
        DiscordRoleSyncAuditStore auditStore)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _auditStore = auditStore;
    }

    public async Task AssignRoleAsync(string discordUserId, string roleId)
    {
        await SendRoleRequestAsync(HttpMethod.Put, discordUserId, roleId, "assign");
    }

    public async Task RemoveRoleAsync(string discordUserId, string roleId)
    {
        await SendRoleRequestAsync(HttpMethod.Delete, discordUserId, roleId, "remove");
    }

    private async Task SendRoleRequestAsync(
        HttpMethod method,
        string discordUserId,
        string roleId,
        string action)
    {
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            throw new ArgumentException("Discord user ID is required.", nameof(discordUserId));
        }

        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("Discord role ID is required.", nameof(roleId));
        }

        var guildId = _config["DISCORD_GUILD_ID"];
        var botToken = _config["DISCORD_BOT_TOKEN"];

        if (string.IsNullOrWhiteSpace(guildId))
        {
            throw new InvalidOperationException("DISCORD_GUILD_ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(botToken))
        {
            throw new InvalidOperationException("DISCORD_BOT_TOKEN is not configured.");
        }

        var url =
            $"https://discord.com/api/v10/guilds/{guildId}/members/{discordUserId}/roles/{roleId}";

        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bot", botToken);

        using var response = await _http.SendAsync(req);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _auditStore.Append(new DiscordRoleSyncAuditRecord
            {
                Action = action,
                DiscordUserId = discordUserId,
                RoleId = roleId,
                Success = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = responseBody,
                CreatedAt = DateTimeOffset.UtcNow
            });
            _logger.LogWarning(
                "Discord role {Action} failed for user {DiscordUserId}, role {RoleId}. Status: {StatusCode}. Body: {Body}",
                action,
                discordUserId,
                roleId,
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Discord role {action} failed with status {(int)response.StatusCode}.");
        }

        _auditStore.Append(new DiscordRoleSyncAuditRecord
        {
            Action = action,
            DiscordUserId = discordUserId,
            RoleId = roleId,
            Success = true,
            StatusCode = (int)response.StatusCode,
            ErrorMessage = null,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _logger.LogInformation(
            "Discord role {Action} succeeded for user {DiscordUserId}, role {RoleId}.",
            action,
            discordUserId,
            roleId);
    }
}

