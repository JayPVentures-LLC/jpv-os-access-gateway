using Microsoft.AspNetCore.Mvc;
using JPVOS.Infrastructure.Stripe;
using Stripe.BillingPortal;

namespace JPVOS.Api;

[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly StripeCheckoutService _checkout;

    public CheckoutController(
        StripeCheckoutService checkout)
    {
        _checkout = checkout;
    }

    public sealed class CheckoutRequest
    {
        public string LookupKey { get; set; } = "";
    }

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession(
        [FromBody] CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LookupKey))
        {
            return BadRequest(new
            {
                error = "lookup_key_required"
            });
        }

        var session =
            await _checkout.CreateCheckoutSessionAsync(
                request.LookupKey,
                Request);

        return Ok(new
        {
            url = session.Url,
            sessionId = session.Id
        });
    }

    public sealed class PortalRequest
    {
        public string CustomerId { get; set; } = "";
    }

    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortalSession(
        [FromBody] PortalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            return BadRequest(new
            {
                error = "customer_id_required"
            });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var options = new SessionCreateOptions
        {
            Customer = request.CustomerId,
            ReturnUrl = $"{baseUrl}/account/billing"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Ok(new
        {
            url = session.Url
        });
    }
}

