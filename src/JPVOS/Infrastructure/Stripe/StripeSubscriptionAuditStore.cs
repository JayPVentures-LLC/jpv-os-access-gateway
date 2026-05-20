using System.Text.Json;

namespace JPVOS.Infrastructure.Stripe;

public sealed class StripeSubscriptionAuditStore
{
    private readonly IWebHostEnvironment _env;
    private readonly object _lock = new();

    public StripeSubscriptionAuditStore(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string StorePath
    {
        get
        {
            var dir = Path.Combine(_env.ContentRootPath, "App_Data", "audit");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "stripe-subscription-state.json");
        }
    }

    public void Append(StripeSubscriptionState state)
    {
        lock (_lock)
        {
            var records = Load();
            records.Add(state);

            File.WriteAllText(
                StorePath,
                JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public List<StripeSubscriptionState> Load()
    {
        if (!File.Exists(StorePath))
        {
            return new List<StripeSubscriptionState>();
        }

        var json = File.ReadAllText(StorePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<StripeSubscriptionState>();
        }

        return JsonSerializer.Deserialize<List<StripeSubscriptionState>>(json)
            ?? new List<StripeSubscriptionState>();
    }
}
