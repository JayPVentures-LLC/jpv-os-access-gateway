namespace JPVOS.Infrastructure.Stripe;

public sealed class StripePricingMap
{
    public string? Mode { get; set; }

    public string? Pricing_Authority { get; set; }

    public Dictionary<string, StripePriceDefinition> Prices { get; set; } = new();
}

public sealed class StripePriceDefinition
{
    public string? Name { get; set; }

    public int Amount { get; set; }

    public string? Currency { get; set; }

    public string? Interval { get; set; }

    public string? Product_Id { get; set; }

    public string? Price_Id { get; set; }

    public string? Lookup_Key { get; set; }

    public string? Pricing_Authority { get; set; }
}
