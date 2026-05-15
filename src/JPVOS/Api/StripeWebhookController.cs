using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/stripe/webhook")]

public class StripeWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IEntitlementService _entitlementService;
    private readonly DiscordService _discordService;
    public StripeWebhookController(IConfiguration config, IEntitlementService entitlementService, DiscordService discordService)
    {
        _config = config;
        _entitlementService = entitlementService;
        _discordService = discordService;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signatureHeader = Request.Headers["Stripe-Signature"];
        var webhookSecret = _config["STRIPE_WEBHOOK_SECRET"];
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
        }
        catch (Exception)
        {
            return BadRequest();
        }

        // Handle events
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session ?? JsonSerializer.Deserialize<Stripe.Checkout.Session>(stripeEvent.Data.Object.ToString());
                var customerId = session.CustomerId;
                var subscriptionId = session.SubscriptionId;
                var priceId = session.LineItems?.FirstOrDefault()?.Price?.Id ?? session.Metadata?["price_id"] ?? "";
                var interval = session.Metadata?["interval"] ?? "";
                var packageKey = session.Metadata?["package_key"] ?? "";
                var ent = new JPVOS.Models.Entitlement
                {
                    StripeCustomerId = customerId,
                    StripeSubscriptionId = subscriptionId,
                    PackageKey = packageKey,
                    BillingInterval = interval,
                    Status = "active",
                    AccessExpiration = null
                };
                _entitlementService.AddOrUpdate(ent);
                // Discord role assignment deferred until Discord user is linked
                break;
            }
            case "invoice.paid":
            {
                var invoice = stripeEvent.Data.Object as Stripe.Invoice ?? JsonSerializer.Deserialize<Stripe.Invoice>(stripeEvent.Data.Object.ToString());
                var customerId = invoice.CustomerId;
                var ent = _entitlementService.GetByStripeCustomerId(customerId);
                if (ent != null)
                {
                    ent.Status = "active";
                    ent.AccessExpiration = null;
                    _entitlementService.AddOrUpdate(ent);
                }
                break;
            }
            case "invoice.payment_failed":
            {
                var invoice = stripeEvent.Data.Object as Stripe.Invoice ?? JsonSerializer.Deserialize<Stripe.Invoice>(stripeEvent.Data.Object.ToString());
                var customerId = invoice.CustomerId;
                var ent = _entitlementService.GetByStripeCustomerId(customerId);
                if (ent != null)
                {
                    ent.Status = "past_due";
                    _entitlementService.AddOrUpdate(ent);
                }
                break;
            }
            case "customer.subscription.updated":
            {
                var sub = stripeEvent.Data.Object as Stripe.Subscription ?? JsonSerializer.Deserialize<Stripe.Subscription>(stripeEvent.Data.Object.ToString());
                var customerId = sub.CustomerId;
                var ent = _entitlementService.GetByStripeCustomerId(customerId);
                if (ent != null)
                {
                    ent.StripeSubscriptionId = sub.Id;
                    ent.Status = sub.Status;
                    ent.AccessExpiration = sub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
                    _entitlementService.AddOrUpdate(ent);
                }
                break;
            }
            case "customer.subscription.deleted":
            {
                var sub = stripeEvent.Data.Object as Stripe.Subscription ?? JsonSerializer.Deserialize<Stripe.Subscription>(stripeEvent.Data.Object.ToString());
                var customerId = sub.CustomerId;
                var ent = _entitlementService.GetByStripeCustomerId(customerId);
                if (ent != null)
                {
                    // Remove Discord role if linked
                    if (!string.IsNullOrEmpty(ent.DiscordUserId) && !string.IsNullOrEmpty(ent.DiscordRole))
                    {
                        _ = _discordService.RemoveRoleAsync(ent.DiscordUserId, ent.DiscordRole);
                    }
                    _entitlementService.RemoveByStripeCustomerId(customerId);
                }
                break;
            }
        }
        return Ok();
    }
}
