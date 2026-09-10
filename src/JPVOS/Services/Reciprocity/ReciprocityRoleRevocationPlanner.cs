namespace JPVOS.Services.Reciprocity;

public sealed record ReciprocityRoleRevocationPlan(
    bool HasStoredAssignment,
    string? DiscordUserId,
    string? RoleId);

public sealed class ReciprocityRoleRevocationPlanner
{
    public ReciprocityRoleRevocationPlan Plan(
        string? storedDiscordUserId,
        string? storedRoleId,
        string currentDiscordUserId,
        string currentRoleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDiscordUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentRoleId);

        if (string.IsNullOrWhiteSpace(storedDiscordUserId) || string.IsNullOrWhiteSpace(storedRoleId))
            return new(false, null, null);

        return new(true, storedDiscordUserId, storedRoleId);
    }
}
