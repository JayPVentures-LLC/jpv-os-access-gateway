using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;

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

        var priceId = req.PackageKey?.ToLowerInvariant() switch
        {
            "community" => _config["STRIPE_PRICE_ID_COMMUNITY"],
            "vip" => _config["STRIPE_PRICE_ID_VIP"],
            _ => null
        };

        if (string.IsNullOrWhiteSpace(priceId))
        {
            return BadRequest("Invalid or unavailable package key. Only Community and VIP are enabled for checkout.");
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
                ["package_key"] = req.PackageKey ?? "",
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

    public sealed class CheckoutRequest
    {
        public string PackageKey { get; set; } = "";
        public string Interval { get; set; } = "monthly";
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
