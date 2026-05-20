namespace JPVOS.Infrastructure.Discord;

public sealed class DiscordRoleSyncAuditRecord
{
    public string Action { get; set; } = "";
    public string DiscordUserId { get; set; } = "";
    public string RoleId { get; set; } = "";
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
