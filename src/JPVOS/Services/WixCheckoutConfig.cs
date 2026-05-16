namespace JPVOS.Services;

/// <summary>
/// Configuration for Wix checkout URLs.
/// Maps package keys to Wix checkout links.
/// </summary>
public class WixCheckoutConfig
{
    private readonly IConfiguration _config;

    public WixCheckoutConfig(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Gets the Wix checkout URL for a given package key.
    /// Falls back to a configuration placeholder if not configured.
    /// </summary>
    public string GetCheckoutUrl(string packageKey, string interval = "monthly")
    {
        if (string.IsNullOrWhiteSpace(packageKey))
        {
            return "/checkout-not-configured";
        }

        // Check for specific package configuration
        var configKey = $"WIX_CHECKOUT_URL_{packageKey.ToUpperInvariant()}_{interval.ToUpperInvariant()}";
        var url = _config[configKey];
        
        if (!string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        // Fallback: check for generic package key (without interval)
        configKey = $"WIX_CHECKOUT_URL_{packageKey.ToUpperInvariant()}";
        url = _config[configKey];
        
        if (!string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        // Default fallback
        return "/checkout-not-configured";
    }

    /// <summary>
    /// Checks if Wix checkout is configured for the given package.
    /// </summary>
    public bool IsConfigured(string packageKey, string interval = "monthly")
    {
        var url = GetCheckoutUrl(packageKey, interval);
        return url != "/checkout-not-configured";
    }
}
