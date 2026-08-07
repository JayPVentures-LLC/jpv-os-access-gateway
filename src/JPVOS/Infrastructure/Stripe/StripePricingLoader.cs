using System.Text.Json;

namespace JPVOS.Infrastructure.Stripe;

public sealed class StripePricingLoader
{
    private const string CanonicalPricingAuthority = "JPV-OS-v2.1.0";

    private static readonly IReadOnlyDictionary<string, (int Amount, string Interval)> CanonicalPrices =
        new Dictionary<string, (int Amount, string Interval)>(StringComparer.Ordinal)
        {
            ["member_access_monthly"] = (2000, "month"),
            ["member_access_annual"] = (20000, "year"),
            ["creator_infrastructure_monthly"] = (50000, "month"),
            ["creator_infrastructure_annual"] = (500000, "year"),
            ["partner_infrastructure_monthly"] = (250000, "month"),
            ["partner_infrastructure_annual"] = (2500000, "year"),
            ["enterprise_infrastructure_monthly"] = (1000000, "month"),
            ["enterprise_infrastructure_annual"] = (10000000, "year")
        };

    private static readonly HashSet<string> LegacyLookupKeys = new(StringComparer.Ordinal)
    {
        "vip_venture_monthly",
        "vip_venture_annual",
        "creator_lane_monthly",
        "operator_monthly",
        "enterprise_monthly"
    };

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<StripePricingLoader> _logger;
    private StripePricingMap? _cache;

    public StripePricingLoader(
        IWebHostEnvironment env,
        ILogger<StripePricingLoader> logger)
    {
        _env = env;
        _logger = logger;
    }

    public StripePricingMap Load()
    {
        if (_cache != null)
        {
            return _cache;
        }

        var root = Directory.GetParent(_env.ContentRootPath)?.Parent?.Parent?.FullName;

        if (root is null)
        {
            throw new InvalidOperationException("Unable to resolve repo root.");
        }

        var mode = Environment.GetEnvironmentVariable("STRIPE_MODE") ?? "test";

        var path = Path.Combine(
            root,
            "infrastructure",
            "stripe",
            "generated",
            $"stripe-pricing.{mode}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Stripe pricing map missing: {path}");
        }

        var json = File.ReadAllText(path);

        var result = JsonSerializer.Deserialize<StripePricingMap>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result is null)
        {
            throw new InvalidOperationException(
                "Failed to deserialize Stripe pricing map.");
        }

        ValidateCanonicalPricing(result, path);

        _cache = result;

        _logger.LogInformation(
            "Stripe pricing map loaded for mode {Mode} under {PricingAuthority}",
            mode,
            CanonicalPricingAuthority);

        return result;
    }

    public StripePriceDefinition Resolve(string lookupKey)
    {
        if (!CanonicalPrices.ContainsKey(lookupKey))
        {
            throw new KeyNotFoundException(
                $"Checkout lookup key is not governed by {CanonicalPricingAuthority}: {lookupKey}");
        }

        var map = Load();

        if (!map.Prices.TryGetValue(lookupKey, out var result))
        {
            throw new KeyNotFoundException(
                $"Stripe lookup key not found: {lookupKey}");
        }

        return result;
    }

    private static void ValidateCanonicalPricing(StripePricingMap map, string path)
    {
        if (!string.Equals(map.Pricing_Authority, CanonicalPricingAuthority, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stripe pricing map is stale or unauthoritative: {path}. Expected pricing_authority={CanonicalPricingAuthority}; found {map.Pricing_Authority ?? "<missing>"}.");
        }

        foreach (var legacyKey in LegacyLookupKeys)
        {
            if (map.Prices.ContainsKey(legacyKey))
            {
                throw new InvalidOperationException(
                    $"Legacy Stripe lookup key is prohibited under {CanonicalPricingAuthority}: {legacyKey}");
            }
        }

        foreach (var expected in CanonicalPrices)
        {
            if (!map.Prices.TryGetValue(expected.Key, out var price))
            {
                throw new InvalidOperationException(
                    $"Canonical Stripe lookup key missing: {expected.Key}");
            }

            if (price.Amount != expected.Value.Amount)
            {
                throw new InvalidOperationException(
                    $"Canonical price mismatch for {expected.Key}: expected {expected.Value.Amount}, found {price.Amount}.");
            }

            if (!string.Equals(price.Interval, expected.Value.Interval, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Canonical interval mismatch for {expected.Key}: expected {expected.Value.Interval}, found {price.Interval ?? "<missing>"}.");
            }

            if (!string.Equals(price.Currency, "usd", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Canonical currency mismatch for {expected.Key}: expected usd, found {price.Currency ?? "<missing>"}.");
            }

            if (!string.Equals(price.Pricing_Authority, CanonicalPricingAuthority, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Price entry is missing canonical authority for {expected.Key}.");
            }

            if (string.IsNullOrWhiteSpace(price.Price_Id) || string.IsNullOrWhiteSpace(price.Product_Id))
            {
                throw new InvalidOperationException(
                    $"Canonical Stripe identifiers missing for {expected.Key}.");
            }
        }
    }
}
