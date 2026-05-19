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
    // Supported package keys and their env var mapping (with legacy fallback)
    var packageMap = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase)
    {
      // Member Access
      ["member"] = _config["STRIPE_PRICE_MEMBER_MONTHLY"] ?? _config["STRIPE_PRICE_ID_COMMUNITY"],
      ["member_monthly"] = _config["STRIPE_PRICE_MEMBER_MONTHLY"] ?? _config["STRIPE_PRICE_ID_COMMUNITY"],
      ["member_annual"] = _config["STRIPE_PRICE_MEMBER_ANNUAL"],
      // VIP Venture
      ["vip"] = _config["STRIPE_PRICE_VIP_MONTHLY"] ?? _config["STRIPE_PRICE_ID_VIP"],
      ["vip_monthly"] = _config["STRIPE_PRICE_VIP_MONTHLY"] ?? _config["STRIPE_PRICE_ID_VIP"],
      ["vip_annual"] = _config["STRIPE_PRICE_VIP_ANNUAL"],
      // Creator Lane
      ["creator_lane"] = _config["STRIPE_PRICE_CREATOR_LANE_MONTHLY"],
      // Operator
      ["operator"] = _config["STRIPE_PRICE_OPERATOR_MONTHLY"] ?? _config["STRIPE_PRICE_CUSTOM_IMPLEMENTATION"],
      // Enterprise
      ["enterprise"] = _config["STRIPE_PRICE_ENTERPRISE_MONTHLY"] ?? _config["STRIPE_PRICE_ENTERPRISE_ANNUAL"],
      // Legacy/compatibility
      ["community"] = _config["STRIPE_PRICE_ID_COMMUNITY"],
      ["enterprise_infrastructure_annual"] = _config["STRIPE_PRICE_ENTERPRISE_ANNUAL"],
      ["custom_implementation_one_time"] = _config["STRIPE_PRICE_CUSTOM_IMPLEMENTATION"],
    };

    var normalizedPackageKey = req.PackageKey?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(normalizedPackageKey))
    {
      return BadRequest("Package key is required.");
    }

    if (normalizedPackageKey == "sovereign")
    {
      // Sovereign is contact-only, not Stripe checkout
      return BadRequest("Sovereign is a custom/contact-only package. Please contact support for this tier.");
    }

    if (!packageMap.TryGetValue(normalizedPackageKey, out var priceId) || string.IsNullOrWhiteSpace(priceId))
    {
      return BadRequest($"Invalid or unavailable package key: {normalizedPackageKey}. Please check configuration.");
    }

    // Only require the selected package's price ID and Stripe keys
    var requiredVars = new[] { "STRIPE_SECRET_KEY", "STRIPE_WEBHOOK_SECRET" };
    var missing = requiredVars.Where(v => string.IsNullOrWhiteSpace(_config[v])).ToList();
    if (string.IsNullOrWhiteSpace(priceId))
    {
      missing.Add($"Price ID for {normalizedPackageKey}");
    }
    if (missing.Count > 0)
    {
      return BadRequest($"Checkout is not configured yet. Missing: {string.Join(", ", missing)}");
    }

    // Determine mode (subscription for recurring, payment for one-time)
    var recurringKeys = new[] { "member", "member_monthly", "member_annual", "vip", "vip_monthly", "vip_annual", "creator_lane", "operator", "enterprise" };
    var mode = recurringKeys.Contains(normalizedPackageKey) ? "subscription" : "payment";

    // Use PUBLIC_APP_BASE_URL if set, else fallback to request origin
    var domain = _config["PUBLIC_APP_BASE_URL"] ?? ($"{Request.Scheme}://{Request.Host.Value}");
    var successUrl = string.IsNullOrWhiteSpace(req.SuccessUrl) ? $"{domain}/success" : req.SuccessUrl;
    var cancelUrl = string.IsNullOrWhiteSpace(req.CancelUrl) ? $"{domain}/pricing" : req.CancelUrl;

    var options = new SessionCreateOptions
    {
      PaymentMethodTypes = new List<string> { "card" },
      LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
      Mode = mode,
      SuccessUrl = successUrl,
      CancelUrl = cancelUrl,
      AllowPromotionCodes = true,
      BillingAddressCollection = "auto",
      Metadata = new Dictionary<string, string>
      {
        ["package_key"] = normalizedPackageKey,
        ["interval"] = req.Interval ?? "monthly"
      }
    };

    // Enable automatic tax if configured
    if (bool.TryParse(_config["STRIPE_AUTOMATIC_TAX_ENABLED"], out var autoTax) && autoTax)
    {
      options.AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true };
    }

    // Customer creation/handling is safe for subscriptions by default

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
