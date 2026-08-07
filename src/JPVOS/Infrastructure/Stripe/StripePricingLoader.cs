using System.Linq;
using System.Text.Json;

namespace JPVOS.Infrastructure.Stripe;

public sealed class StripePricingLoader
{
    public const string CanonicalPricingAuthority = "JPV-OS-v2.1.0";

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

        var environmentMap = TryLoadFromEnvironment();
        if (environmentMap is not null)
        {
            ValidateCanonicalPricing(environmentMap, "environment");
            _cache = environmentMap;
            _logger.LogInformation(
                "Stripe pricing loaded from environment under {PricingAuthority}",
                CanonicalPricingAuthority);
            return environmentMap;
        }

        var mode = Environment.GetEnvironmentVariable("STRIPE_MODE") ?? "test";
        var path = ResolvePricingMapPath(mode);
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
            "Stripe pricing map loaded from {Path} for mode {Mode} under {PricingAuthority}",
            path,
            mode,
            CanonicalPricingAuthority);

        return result;
    }

    public StripePriceDefinition Resolve(string lookupKey)
    {
        _ = ResolveExpected(lookupKey);
        var map = Load();

        if (!map.Prices.TryGetValue(lookupKey, out var result))
        {
            throw new KeyNotFoundException(
                $"Stripe lookup key not found: {lookupKey}");
        }

        return result;
    }

    public (int Amount, string Interval, string Currency) ResolveExpected(string lookupKey)
    {
        if (!CanonicalPrices.TryGetValue(lookupKey, out var expected))
        {
            throw new KeyNotFoundException(
                $"Checkout lookup key is not governed by {CanonicalPricingAuthority}: {lookupKey}");
        }

        return (expected.Amount, expected.Interval, "usd");
    }

    private StripePricingMap? TryLoadFromEnvironment()
    {
        var configured = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var lookupKey in CanonicalPrices.Keys)
        {
            var variable = "STRIPE_PRICE_" + lookupKey.ToUpperInvariant();
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value))
            {
                configured[lookupKey] = value.Trim();
            }
        }

        if (configured.Count == 0)
        {
            return null;
        }

        if (configured.Count != CanonicalPrices.Count)
        {
            var missing = CanonicalPrices.Keys.Where(key => !configured.ContainsKey(key));
            throw new InvalidOperationException(
                $"Partial canonical Stripe environment configuration is prohibited. Missing: {string.Join(", ", missing)}");
        }

        var authority = Environment.GetEnvironmentVariable("JPV_PRICING_AUTHORITY");
        if (!string.Equals(authority, CanonicalPricingAuthority, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Environment pricing authority mismatch. Expected {CanonicalPricingAuthority}; found {authority ?? "<missing>"}.");
        }

        var map = new StripePricingMap
        {
            Mode = Environment.GetEnvironmentVariable("STRIPE_MODE") ?? "live",
            Pricing_Authority = CanonicalPricingAuthority
        };

        foreach (var expected in CanonicalPrices)
        {
            var priceId = configured[expected.Key];
            if (!priceId.StartsWith("price_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Invalid Stripe price identifier configured for {expected.Key}.");
            }

            map.Prices[expected.Key] = new StripePriceDefinition
            {
                Name = expected.Key,
                Amount = expected.Value.Amount,
                Currency = "usd",
                Interval = expected.Value.Interval,
                Price_Id = priceId,
                Lookup_Key = expected.Key,
                Pricing_Authority = CanonicalPricingAuthority
            };
        }

        return map;
    }

    private string ResolvePricingMapPath(string mode)
    {
        var explicitPath = Environment.GetEnvironmentVariable("JPV_STRIPE_PRICING_MAP_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            if (!File.Exists(explicitPath))
            {
                throw new FileNotFoundException(
                    $"Explicit Stripe pricing map missing: {explicitPath}");
            }
            return explicitPath;
        }

        if (string.IsNullOrWhiteSpace(mode) ||
            mode.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            mode.Contains(Path.DirectorySeparatorChar) ||
            mode.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidOperationException("Stripe mode contains invalid path or filename characters.");
        }

        var fileName = $"stripe-pricing.{mode}.json";
        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Stripe pricing filename must not contain a directory component.");
        }

        var relative = Path.Combine(
            "infrastructure",
            "stripe",
            "generated",
            fileName);

        if (Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("Stripe pricing relative path must not be rooted.");
        }

        foreach (var root in new[] { _env.ContentRootPath, AppContext.BaseDirectory }
            .Select(static origin => new DirectoryInfo(origin)))
        {
            var current = root;
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, relative);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                current = current.Parent;
            }
        }

        throw new FileNotFoundException(
            $"Canonical Stripe pricing map not found for mode {mode}, and no complete governed STRIPE_PRICE_* environment configuration is present.");
    }

    private static void ValidateCanonicalPricing(StripePricingMap map, string source)
    {
        if (!string.Equals(map.Pricing_Authority, CanonicalPricingAuthority, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stripe pricing map is stale or unauthoritative: {source}. Expected pricing_authority={CanonicalPricingAuthority}; found {map.Pricing_Authority ?? "<missing>"}.");
        }

        foreach (var legacyKey in LegacyLookupKeys.Where(map.Prices.ContainsKey))
        {
            throw new InvalidOperationException(
                $"Legacy Stripe lookup key is prohibited under {CanonicalPricingAuthority}: {legacyKey}");
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

            if (string.IsNullOrWhiteSpace(price.Price_Id))
            {
                throw new InvalidOperationException(
                    $"Canonical Stripe price ID missing for {expected.Key}.");
            }
        }
    }
}
