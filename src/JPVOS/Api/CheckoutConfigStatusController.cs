using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/checkout/config-status")]
public class CheckoutConfigStatusController : ControllerBase
{
    private static readonly string[] RequiredVars = new[]
    {
        "STRIPE_SECRET_KEY",
        "STRIPE_WEBHOOK_SECRET",
        "STRIPE_PRICE_ID_COMMUNITY",
        "STRIPE_PRICE_ID_VIP"
    };

    private readonly IConfiguration _config;
    public CheckoutConfigStatusController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var missing = RequiredVars.Where(v => string.IsNullOrWhiteSpace(_config[v])).ToList();
        var stripeConfigured = missing.Count == 0 && !string.IsNullOrWhiteSpace(_config["STRIPE_SECRET_KEY"]);
        var priceIdsConfigured = RequiredVars.Count(v => !string.IsNullOrWhiteSpace(_config[v]));
        // No custom implementation for launch
        var secret = _config["STRIPE_SECRET_KEY"] ?? "";
        string checkoutMode = "unknown";
        if (!string.IsNullOrWhiteSpace(secret))
        {
            checkoutMode = secret.StartsWith("sk_test_") ? "test" : (secret.StartsWith("sk_live_") ? "live" : "unknown");
        }
        return Ok(new
        {
            stripeConfigured,
            missingVariables = missing,
            checkoutMode,
            priceIdsConfigured
        });
    }
}
