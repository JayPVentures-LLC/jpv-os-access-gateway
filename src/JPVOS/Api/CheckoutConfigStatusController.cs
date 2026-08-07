using Microsoft.AspNetCore.Mvc;
using JPVOS.Infrastructure.Stripe;

namespace JPVOS.Api;

[ApiController]
[Route("api/checkout/status")]
public sealed class CheckoutConfigStatusController : ControllerBase
{
    private readonly StripePricingLoader _loader;

    public CheckoutConfigStatusController(StripePricingLoader loader)
    {
        _loader = loader;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            var map = _loader.Load();
            var secret = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
            var webhook = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

            return Ok(new
            {
                stripeConfigured = !string.IsNullOrWhiteSpace(secret),
                webhookConfigured = !string.IsNullOrWhiteSpace(webhook),
                pricingMapLoaded = true,
                pricingAuthority = map.Pricing_Authority,
                canonicalPricingAuthority = StripePricingLoader.CanonicalPricingAuthority,
                pricingAuthorityHealthy = string.Equals(
                    map.Pricing_Authority,
                    StripePricingLoader.CanonicalPricingAuthority,
                    StringComparison.Ordinal),
                mode = map.Mode,
                lookupKeys = map.Prices.Keys.OrderBy(key => key),
                environmentHealthy = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                environmentHealthy = false,
                pricingAuthorityHealthy = false,
                canonicalPricingAuthority = StripePricingLoader.CanonicalPricingAuthority,
                error = ex.Message
            });
        }
    }
}
