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

    using var reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
    var json = await reader.ReadToEndAsync();
    var signatureHeader = Request.Headers["Stripe-Signature"];
    var webhookSecret = _config["STRIPE_WEBHOOK_SECRET"];
    if (string.IsNullOrWhiteSpace(webhookSecret))
    {
      _logger.LogError("Stripe webhook secret is not configured.");
      return BadRequest("Webhook secret not configured.");
    }
    Event stripeEvent;
    try
    {
      stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
    }
    catch (Exception ex)
    {
      _logger.LogWarning("Stripe webhook signature verification failed: {Message}", ex.Message);
      return BadRequest("Invalid Stripe signature.");
    }

    // Handle events
    switch (stripeEvent.Type)
    {
      case "checkout.session.completed":
        {
          Stripe.Checkout.Session? session = null;
          try
          {
            session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session == null)
            {
              session = JsonSerializer.Deserialize<Stripe.Checkout.Session>(stripeEvent.Data.Object.ToString() ?? "{}");
            }
          }
          catch (JsonException ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe Checkout.Session");
          }
          catch (InvalidCastException ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe Checkout.Session");
          }
          catch (FormatException ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe Checkout.Session");
          }
          if (session == null)
          {
            _logger.LogWarning("Received checkout.session.completed with null session");
            return BadRequest("Invalid session payload.");
          }
          if (string.IsNullOrWhiteSpace(session.CustomerId))
          {
            _logger.LogWarning("Received checkout.session.completed with missing customer ID");
            return BadRequest("Missing customer id.");
          }
          var customerId = session.CustomerId;
          var subscriptionId = session.SubscriptionId;
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
          break;
        }
      case "invoice.paid":
        {
          Stripe.Invoice? invoice = null;
          try
          {
            invoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (invoice == null)
            {
              invoice = JsonSerializer.Deserialize<Stripe.Invoice>(stripeEvent.Data.Object.ToString() ?? "{}");
            }
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe.Invoice");
          }
          if (invoice == null)
          {
            _logger.LogWarning("Received invoice.paid with null invoice");
            return BadRequest("Invalid invoice payload.");
          }
          if (string.IsNullOrWhiteSpace(invoice.CustomerId))
          {
            _logger.LogWarning("Received invoice.paid with missing customer ID");
            return BadRequest("Missing customer id.");
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
          break;
        }
      case "invoice.payment_failed":
        {
          Stripe.Invoice? invoice = null;
          try
          {
            invoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (invoice == null)
            {
              invoice = JsonSerializer.Deserialize<Stripe.Invoice>(stripeEvent.Data.Object.ToString() ?? "{}");
            }
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe.Invoice");
          }
          if (invoice == null)
          {
            _logger.LogWarning("Received invoice.payment_failed with null invoice");
            return BadRequest("Invalid invoice payload.");
          }
          if (string.IsNullOrWhiteSpace(invoice.CustomerId))
          {
            _logger.LogWarning("Received invoice.payment_failed with missing customer ID");
            return BadRequest("Missing customer id.");
          }
          var customerId = invoice.CustomerId;
          var ent = _entitlementService.GetByStripeCustomerId(customerId);
          if (ent != null)
          {
            ent.Status = "past_due";
            _entitlementService.AddOrUpdate(ent);
            _logger.LogWarning("Payment failed for customer {CustomerId}", customerId);
          }
          break;
        }
      case "customer.subscription.updated":
        {
          Stripe.Subscription? sub = null;
          try
          {
            sub = stripeEvent.Data.Object as Stripe.Subscription;
            if (sub == null)
            {
              sub = JsonSerializer.Deserialize<Stripe.Subscription>(stripeEvent.Data.Object.ToString() ?? "{}");
            }
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe.Subscription");
          }
          if (sub == null)
          {
            _logger.LogWarning("Received customer.subscription.updated with null subscription");
            return BadRequest("Invalid subscription payload.");
          }
          if (string.IsNullOrWhiteSpace(sub.CustomerId))
          {
            _logger.LogWarning("Received customer.subscription.updated with missing customer ID");
            return BadRequest("Missing customer id.");
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
          break;
        }
      case "customer.subscription.deleted":
        {
          Stripe.Subscription? sub = null;
          try
          {
            sub = stripeEvent.Data.Object as Stripe.Subscription;
            if (sub == null)
            {
              sub = JsonSerializer.Deserialize<Stripe.Subscription>(stripeEvent.Data.Object.ToString() ?? "{}");
            }
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to deserialize Stripe.Subscription");
          }
          if (sub == null)
          {
            _logger.LogWarning("Received customer.subscription.deleted with null subscription");
            return BadRequest("Invalid subscription payload.");
          }
          if (string.IsNullOrWhiteSpace(sub.CustomerId))
          {
            _logger.LogWarning("Received customer.subscription.deleted with missing customer ID");
            return BadRequest("Missing customer id.");
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
