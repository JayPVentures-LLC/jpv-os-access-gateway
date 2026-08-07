using Stripe;
using Stripe.Checkout;

namespace JPVOS.Infrastructure.Stripe;

public sealed class StripeCheckoutService
{
    private readonly StripePricingLoader _pricingLoader;

    public StripeCheckoutService(StripePricingLoader pricingLoader)
    {
        _pricingLoader = pricingLoader;
    }

    public async Task<Session> CreateCheckoutSessionAsync(
        string lookupKey,
        HttpRequest request)
    {
        var configuredPrice = _pricingLoader.Resolve(lookupKey);
        var expected = _pricingLoader.ResolveExpected(lookupKey);

        if (string.IsNullOrWhiteSpace(configuredPrice.Price_Id))
        {
            throw new InvalidOperationException(
                $"Price ID missing for lookup key: {lookupKey}");
        }

        var priceService = new PriceService();
        var stripePrice = await priceService.GetAsync(configuredPrice.Price_Id);

        if (!stripePrice.Active)
        {
            throw new InvalidOperationException(
                $"Stripe price is inactive for canonical lookup key: {lookupKey}");
        }

        if (stripePrice.UnitAmount != expected.Amount)
        {
            throw new InvalidOperationException(
                $"Stripe amount mismatch for {lookupKey}: expected {expected.Amount}, found {stripePrice.UnitAmount?.ToString() ?? "<missing>"}.");
        }

        if (!string.Equals(stripePrice.Currency, expected.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Stripe currency mismatch for {lookupKey}: expected {expected.Currency}, found {stripePrice.Currency ?? "<missing>"}.");
        }

        if (stripePrice.Recurring is null ||
            !string.Equals(stripePrice.Recurring.Interval, expected.Interval, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stripe recurring interval mismatch for {lookupKey}: expected {expected.Interval}, found {stripePrice.Recurring?.Interval ?? "<missing>"}.");
        }

        if (!string.Equals(stripePrice.LookupKey, lookupKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stripe lookup-key mismatch: expected {lookupKey}, found {stripePrice.LookupKey ?? "<missing>"}.");
        }

        if (stripePrice.Metadata is null ||
            !stripePrice.Metadata.TryGetValue("pricing_authority", out var authority) ||
            !string.Equals(authority, StripePricingLoader.CanonicalPricingAuthority, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Stripe price is not stamped with {StripePricingLoader.CanonicalPricingAuthority}: {lookupKey}");
        }

        var baseUrl =
            $"{request.Scheme}://{request.Host}";

        var options = new SessionCreateOptions
        {
            Mode = "subscription",

            SuccessUrl =
                $"{baseUrl}/billing/success?session_id={{CHECKOUT_SESSION_ID}}",

            CancelUrl =
                $"{baseUrl}/billing/cancelled",

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = configuredPrice.Price_Id,
                    Quantity = 1
                }
            },

            AutomaticTax = new SessionAutomaticTaxOptions
            {
                Enabled = true
            },

            Metadata = new Dictionary<string, string>
            {
                ["ecosystem"] = "JPV-OS",
                ["lookup_key"] = lookupKey,
                ["pricing_authority"] = StripePricingLoader.CanonicalPricingAuthority,
                ["source"] = "access_gateway"
            },

            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["ecosystem"] = "JPV-OS",
                    ["lookup_key"] = lookupKey,
                    ["pricing_authority"] = StripePricingLoader.CanonicalPricingAuthority,
                    ["source"] = "access_gateway"
                }
            }
        };

        var service = new SessionService();

        return await service.CreateAsync(options);
    }
}
