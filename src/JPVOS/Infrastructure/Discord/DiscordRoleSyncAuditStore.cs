using System.Text.Json;

namespace JPVOS.Infrastructure.Discord;

public sealed class DiscordRoleSyncAuditStore
{
    private readonly IWebHostEnvironment _env;
    private readonly object _lock = new();

    public DiscordRoleSyncAuditStore(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string StorePath
    {
        get
        {
            var dir = Path.Combine(_env.ContentRootPath, "App_Data", "audit");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "discord-role-sync-audit.json");
        }
    }

    public void Append(DiscordRoleSyncAuditRecord record)
    {
        lock (_lock)
        {
            var records = Load();
            records.Add(record);

            File.WriteAllText(
                StorePath,
                JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public List<DiscordRoleSyncAuditRecord> Load()
    {
        if (!File.Exists(StorePath))
        {
            return new List<DiscordRoleSyncAuditRecord>();
        }

        var json = File.ReadAllText(StorePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<DiscordRoleSyncAuditRecord>();
        }

        return JsonSerializer.Deserialize<List<DiscordRoleSyncAuditRecord>>(json)
            ?? new List<DiscordRoleSyncAuditRecord>();
    }
}
