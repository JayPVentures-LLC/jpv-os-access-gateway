using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<StripeWebhookController> _logger;
    
    public StripeWebhookController(
        IConfiguration config, 
        IEntitlementService entitlementService, 
        DiscordService discordService,
        ILogger<StripeWebhookController> logger)
    {
        _config = config;
        _entitlementService = entitlementService;
        _discordService = discordService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogWarning("Stripe webhook signature verification failed: {Message}", ex.Message);
            return BadRequest();
        }

        // Handle events
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            {
                try
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session ?? JsonSerializer.Deserialize<Stripe.Checkout.Session>(stripeEvent.Data.Object.ToString() ?? "{}");
                    if (session == null || string.IsNullOrWhiteSpace(session.CustomerId))
                    {
                        _logger.LogWarning("Received checkout.session.completed with missing session or customer ID");
                        break;
                    }
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
                    _logger.LogInformation("Checkout session completed for customer {CustomerId}", customerId);
                    // Discord role assignment deferred until Discord user is linked
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing checkout.session.completed event");
                }
                break;
            }
            case "invoice.paid":
            {
                try
                {
                    var invoice = stripeEvent.Data.Object as Stripe.Invoice ?? JsonSerializer.Deserialize<Stripe.Invoice>(stripeEvent.Data.Object.ToString() ?? "{}");
                    if (invoice == null || string.IsNullOrWhiteSpace(invoice.CustomerId))
                    {
                        _logger.LogWarning("Received invoice.paid with missing invoice or customer ID");
                        break;
                    }
                    var customerId = invoice.CustomerId;
                    var ent = _entitlementService.GetByStripeCustomerId(customerId);
                    if (ent != null)
                    {
                        ent.Status = "active";
                        ent.AccessExpiration = null;
                        _entitlementService.AddOrUpdate(ent);
                        _logger.LogInformation("Invoice paid for customer {CustomerId}", customerId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing invoice.paid event");
                }
                break;
            }
            case "invoice.payment_failed":
            {
                try
                {
                    var invoice = stripeEvent.Data.Object as Stripe.Invoice ?? JsonSerializer.Deserialize<Stripe.Invoice>(stripeEvent.Data.Object.ToString() ?? "{}");
                    if (invoice == null || string.IsNullOrWhiteSpace(invoice.CustomerId))
                    {
                        _logger.LogWarning("Received invoice.payment_failed with missing invoice or customer ID");
                        break;
                    }
                    var customerId = invoice.CustomerId;
                    var ent = _entitlementService.GetByStripeCustomerId(customerId);
                    if (ent != null)
                    {
                        ent.Status = "past_due";
                        _entitlementService.AddOrUpdate(ent);
                        _logger.LogWarning("Payment failed for customer {CustomerId}", customerId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing invoice.payment_failed event");
                }
                break;
            }
            case "customer.subscription.updated":
            {
                try
                {
                    var sub = stripeEvent.Data.Object as Stripe.Subscription ?? JsonSerializer.Deserialize<Stripe.Subscription>(stripeEvent.Data.Object.ToString() ?? "{}");
                    if (sub == null || string.IsNullOrWhiteSpace(sub.CustomerId))
                    {
                        _logger.LogWarning("Received customer.subscription.updated with missing subscription or customer ID");
                        break;
                    }

                    var ent = _entitlementService.GetByStripeCustomerId(sub.CustomerId);
                    if (ent != null)
                    {
                        ent.StripeSubscriptionId = sub.Id;
                        ent.Status = sub.Status;
                        ent.AccessExpiration = GetCurrentPeriodEnd(sub);
                        _entitlementService.AddOrUpdate(ent);
                        _logger.LogInformation("Subscription updated for customer {CustomerId}, status: {Status}", sub.CustomerId, sub.Status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing customer.subscription.updated event");
                }
                break;
            }
            case "customer.subscription.deleted":
            {
                try
                {
                    var sub = stripeEvent.Data.Object as Stripe.Subscription ?? JsonSerializer.Deserialize<Stripe.Subscription>(stripeEvent.Data.Object.ToString() ?? "{}");
                    if (sub == null || string.IsNullOrWhiteSpace(sub.CustomerId))
                    {
                        _logger.LogWarning("Received customer.subscription.deleted with missing subscription or customer ID");
                        break;
                    }
                    var customerId = sub.CustomerId;
                    var ent = _entitlementService.GetByStripeCustomerId(customerId);
                    if (ent != null)
                    {
                        // Remove Discord role if linked
                        if (!string.IsNullOrEmpty(ent.DiscordUserId) && !string.IsNullOrEmpty(ent.DiscordRole))
                        {
                            _ = _discordService.RemoveRoleAsync(ent.DiscordUserId, ent.DiscordRole);
                            _logger.LogInformation("Discord role {DiscordRole} revoked for user {DiscordUserId}", ent.DiscordRole, ent.DiscordUserId);
                        }
                        _entitlementService.RemoveByStripeCustomerId(customerId);
                        _logger.LogWarning("Subscription deleted for customer {CustomerId}, entitlement revoked", customerId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing customer.subscription.deleted event");
                }
                break;
            }
        }
        return Ok();
    }

    private DateTime? GetCurrentPeriodEnd(Subscription sub)
    {
        // Handle version compatibility for Stripe.net API
        // CurrentPeriodEnd property may have different names/types across versions
        var prop = sub.GetType().GetProperty("CurrentPeriodEnd");
        if (prop != null && prop.GetValue(sub) is DateTime dt)
        {
            return dt;
        }

        prop = sub.GetType().GetProperty("CurrentPeriodEndUnix");
        if (prop != null && prop.GetValue(sub) is long unix)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        }

        return null;
    }
}
