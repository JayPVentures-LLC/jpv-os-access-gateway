using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;
using JPVOS.Models;

namespace JPVOS.Api
{
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
                "STRIPE_PRICE_ENTERPRISE_ANNUAL",
                "STRIPE_PRICE_CUSTOM_IMPLEMENTATION"
            };
            var missing = requiredVars.Where(v => string.IsNullOrWhiteSpace(_config[v])).ToList();
            if (missing.Count > 0)
            {
                return BadRequest($"Checkout is not configured yet. Missing server environment variable: {string.Join(", ", missing)}");
            }

            string priceId = req.PackageKey switch
            {
                "enterprise_infrastructure_annual" => _config["STRIPE_PRICE_ENTERPRISE_ANNUAL"],
                "custom_implementation_one_time" => _config["STRIPE_PRICE_CUSTOM_IMPLEMENTATION"],
                _ => null
            };
            if (string.IsNullOrEmpty(priceId))
            {
                return BadRequest("Invalid or unavailable package key. Only enterprise and custom implementation are enabled for checkout.");
            }

            var domain = Request.Scheme + "://" + Request.Host.Value;
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
                Mode = "subscription",
                SuccessUrl = string.IsNullOrEmpty(req.SuccessUrl) ? domain + "/success" : req.SuccessUrl,
                CancelUrl = string.IsNullOrEmpty(req.CancelUrl) ? domain + "/pricing" : req.CancelUrl,
            };
            var service = new SessionService();
            var session = service.Create(options);
            if (string.IsNullOrEmpty(session.Url))
            {
                return StatusCode(500, "Stripe session creation failed: No URL returned.");
            }
            return Ok(new { url = session.Url });
        }
    }
}
