using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;
using JPVOS.Models;

namespace JPVOS.Api;

[ApiController]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly IConfiguration _config;

    public CheckoutController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("create")]
    public IActionResult Create([FromBody] CheckoutRequest req)
    {
        var requiredVars = new[]
        {
            "STRIPE_SECRET_KEY",
            "STRIPE_WEBHOOK_SECRET",
            "STRIPE_PRICE_ID_COMMUNITY",
            "STRIPE_PRICE_ID_VIP"
        };

        var missing = requiredVars.Where(v => string.IsNullOrWhiteSpace(_config[v])).ToList();
        if (missing.Count > 0)
        {
            return BadRequest($"Checkout is not configured yet. Missing server environment variable: {string.Join(", ", missing)}");
        }

        var normalizedPackageKey = req.PackageKey?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedPackageKey))
        {
            return BadRequest("Package key is required.");
        }

        var priceId = normalizedPackageKey switch
        {
            "community" => _config["STRIPE_PRICE_ID_COMMUNITY"],
            "vip" => _config["STRIPE_PRICE_ID_VIP"],
            "enterprise_infrastructure_annual" => _config["STRIPE_PRICE_ENTERPRISE_ANNUAL"],
            "custom_implementation_one_time" => _config["STRIPE_PRICE_CUSTOM_IMPLEMENTATION"],
            _ => null
        };

        if (string.IsNullOrWhiteSpace(priceId))
        {
            return BadRequest("Invalid or unavailable package key.");
        }

        var domain = $"{Request.Scheme}://{Request.Host.Value}";
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            ],
            Mode = "subscription",
            SuccessUrl = string.IsNullOrWhiteSpace(req.SuccessUrl) ? $"{domain}/success" : req.SuccessUrl,
            CancelUrl = string.IsNullOrWhiteSpace(req.CancelUrl) ? $"{domain}/pricing" : req.CancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["package_key"] = normalizedPackageKey,
                ["interval"] = req.Interval ?? "monthly"
            }
        };

        var service = new SessionService();
        var session = service.Create(options);
        if (string.IsNullOrWhiteSpace(session.Url))
        {
            return StatusCode(500, "Stripe session creation failed: No URL returned.");
        }

        return Ok(new { url = session.Url });
    }
}

public class CheckoutRequest
{
    public string PackageKey { get; set; } = string.Empty;
    public string Interval { get; set; } = "monthly";
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}
