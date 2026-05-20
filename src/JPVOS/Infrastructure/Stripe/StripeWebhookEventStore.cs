using System.Text.Json;

namespace JPVOS.Infrastructure.Stripe;

public sealed class StripeWebhookEventStore
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<StripeWebhookEventStore> _logger;
    private readonly object _lock = new();

    public StripeWebhookEventStore(
        IWebHostEnvironment env,
        ILogger<StripeWebhookEventStore> logger)
    {
        _env = env;
        _logger = logger;
    }

    private string StorePath
    {
        get
        {
            var dir = Path.Combine(_env.ContentRootPath, "App_Data", "audit");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "stripe-webhook-events.json");
        }
    }

    public bool HasProcessed(string eventId)
    {
        lock (_lock)
        {
            return Load().Any(x => x.EventId == eventId);
        }
    }

    public void MarkProcessed(string eventId, string eventType)
    {
        lock (_lock)
        {
            var records = Load();

            if (records.Any(x => x.EventId == eventId))
            {
                _logger.LogInformation("Stripe webhook event already processed: {EventId}", eventId);
                return;
            }

            records.Add(new StripeWebhookEventRecord
            {
                EventId = eventId,
                EventType = eventType,
                ProcessedAt = DateTimeOffset.UtcNow
            });

            File.WriteAllText(
                StorePath,
                JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private List<StripeWebhookEventRecord> Load()
    {
        if (!File.Exists(StorePath))
        {
            return new List<StripeWebhookEventRecord>();
        }

        var json = File.ReadAllText(StorePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<StripeWebhookEventRecord>();
        }

        return JsonSerializer.Deserialize<List<StripeWebhookEventRecord>>(json)
            ?? new List<StripeWebhookEventRecord>();
    }
}
