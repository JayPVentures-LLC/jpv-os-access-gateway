using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/checkout/config-status")]
public class CheckoutConfigStatusController : ControllerBase
{
  private static readonly string[] StripeConfigVars = new[]
  {
        // Current pricing model
        "STRIPE_SECRET_KEY",
        "STRIPE_WEBHOOK_SECRET",
        "STRIPE_PRICE_MEMBER_MONTHLY",
        "STRIPE_PRICE_MEMBER_ANNUAL",
        "STRIPE_PRICE_VIP_MONTHLY",
        "STRIPE_PRICE_VIP_ANNUAL",
        "STRIPE_PRICE_CREATOR_LANE_MONTHLY",
        "STRIPE_PRICE_OPERATOR_MONTHLY",
        "STRIPE_PRICE_ENTERPRISE_MONTHLY",
        // Compatibility/legacy
        "STRIPE_PRICE_ID_COMMUNITY",
        "STRIPE_PRICE_ID_VIP",
        "STRIPE_PRICE_ENTERPRISE_ANNUAL",
        "STRIPE_PRICE_CUSTOM_IMPLEMENTATION"
    };

  private readonly IConfiguration _config;
  public CheckoutConfigStatusController(IConfiguration config)
  {
    _config = config;
  }

  [HttpGet]
  public IActionResult Get()
  {
    var configStatus = StripeConfigVars.ToDictionary(
        v => v,
        v => !string.IsNullOrWhiteSpace(_config[v])
    );
    var secret = _config["STRIPE_SECRET_KEY"] ?? string.Empty;
    string checkoutMode = "unknown";
    if (!string.IsNullOrWhiteSpace(secret))
    {
      checkoutMode = secret.StartsWith("sk_test_") ? "test" : (secret.StartsWith("sk_live_") ? "live" : "unknown");
    }
    return Ok(new
    {
      stripeConfigured = configStatus["STRIPE_SECRET_KEY"] && configStatus["STRIPE_WEBHOOK_SECRET"],
      checkoutMode,
      config = configStatus
    });
  }
}
