using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Text;
using System.Text.Json;
using JPVOS.Infrastructure.Stripe;

[ApiController]
[Route("api/stripe/webhook")]
public class StripeWebhookController : ControllerBase
{
  private static readonly IReadOnlyDictionary<string, (string PackageKey, string Interval)> CanonicalEntitlements =
    new Dictionary<string, (string PackageKey, string Interval)>(StringComparer.Ordinal)
    {
      ["member_access_monthly"] = ("member_access", "monthly"),
      ["member_access_annual"] = ("member_access", "annual"),
      ["creator_infrastructure_monthly"] = ("creator_infrastructure", "monthly"),
      ["creator_infrastructure_annual"] = ("creator_infrastructure", "annual"),
      ["partner_infrastructure_monthly"] = ("partner_infrastructure", "monthly"),
      ["partner_infrastructure_annual"] = ("partner_infrastructure", "annual"),
      ["enterprise_infrastructure_monthly"] = ("enterprise_infrastructure", "monthly"),
      ["enterprise_infrastructure_annual"] = ("enterprise_infrastructure", "annual")
    };

  private readonly IConfiguration _config;
  private readonly IEntitlementService _entitlementService;
  private readonly DiscordService _discordService;
  private readonly ILogger<StripeWebhookController> _logger;
  private readonly StripeWebhookEventStore _eventStore;
  private readonly StripeSubscriptionAuditStore _auditStore;

  public StripeWebhookController(
      IConfiguration config,
      IEntitlementService entitlementService,
      DiscordService discordService,
      ILogger<StripeWebhookController> logger,
      StripeWebhookEventStore eventStore,
      StripeSubscriptionAuditStore auditStore)
  {
    _config = config;
    _entitlementService = entitlementService;
    _discordService = discordService;
    _logger = logger;
    _eventStore = eventStore;
    _auditStore = auditStore;
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

    if (_eventStore.HasProcessed(stripeEvent.Id))
    {
      _logger.LogInformation("Duplicate Stripe webhook event ignored: {EventId} ({EventType})", stripeEvent.Id, stripeEvent.Type);
      return Ok(new
      {
        received = true,
        duplicate = true,
        eventId = stripeEvent.Id,
        eventType = stripeEvent.Type
      });
    }

    string? auditCustomerId = null;
    string? auditSubscriptionId = null;
    string? auditStatus = null;
    bool handledSuccessfully = false;

    switch (stripeEvent.Type)
    {
      case "checkout.session.completed":
        {
          var session = DeserializeStripeObject<Stripe.Checkout.Session>(stripeEvent, "Stripe Checkout.Session");
          if (session == null)
          {
            return BadRequest("Invalid session payload.");
          }
          if (string.IsNullOrWhiteSpace(session.CustomerId))
          {
            _logger.LogWarning("Received checkout.session.completed with missing customer ID");
            return BadRequest("Missing customer id.");
          }

          if (!TryValidateCanonicalCheckoutMetadata(session.Metadata, out var lookupKey, out var packageKey, out var interval, out var metadataError))
          {
            _logger.LogError("Rejected checkout entitlement because canonical metadata validation failed: {Reason}", metadataError);
            return BadRequest("Canonical checkout metadata validation failed.");
          }

          var customerId = session.CustomerId;
          var subscriptionId = session.SubscriptionId;
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
          _logger.LogInformation(
            "Checkout session completed for customer {CustomerId} under canonical lookup key {LookupKey}",
            customerId,
            lookupKey);

          auditCustomerId = customerId;
          auditSubscriptionId = subscriptionId;
          auditStatus = "active";
          handledSuccessfully = true;
          break;
        }

      case "invoice.paid":
        {
          var invoice = DeserializeStripeObject<Stripe.Invoice>(stripeEvent, "Stripe.Invoice");
          if (invoice == null)
          {
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

          auditCustomerId = customerId;
          auditSubscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
          auditStatus = ent?.Status ?? "active";
          handledSuccessfully = true;
          break;
        }

      case "invoice.payment_failed":
        {
          var invoice = DeserializeStripeObject<Stripe.Invoice>(stripeEvent, "Stripe.Invoice");
          if (invoice == null)
          {
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

          auditCustomerId = customerId;
          auditSubscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
          auditStatus = ent?.Status ?? "past_due";
          handledSuccessfully = true;
          break;
        }

      case "customer.subscription.updated":
        {
          var sub = DeserializeStripeObject<Stripe.Subscription>(stripeEvent, "Stripe.Subscription");
          if (sub == null)
          {
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

          auditCustomerId = sub.CustomerId;
          auditSubscriptionId = sub.Id;
          auditStatus = sub.Status;
          handledSuccessfully = true;
          break;
        }

      case "customer.subscription.deleted":
        {
          var sub = DeserializeStripeObject<Stripe.Subscription>(stripeEvent, "Stripe.Subscription");
          if (sub == null)
          {
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
            if (!string.IsNullOrEmpty(ent.DiscordUserId) && !string.IsNullOrEmpty(ent.DiscordRole))
            {
              try
              {
                await _discordService.RemoveRoleAsync(ent.DiscordUserId, ent.DiscordRole);
                _logger.LogInformation("Discord role {DiscordRole} revoked for user {DiscordUserId}", ent.DiscordRole, ent.DiscordUserId);
              }
              catch (HttpRequestException ex)
              {
                _logger.LogError(ex, "Failed to revoke Discord role {DiscordRole} for user {DiscordUserId}", ent.DiscordRole, ent.DiscordUserId);
                return StatusCode(StatusCodes.Status502BadGateway, "Failed to revoke Discord role.");
              }
              catch (TaskCanceledException ex)
              {
                _logger.LogError(ex, "Failed to revoke Discord role {DiscordRole} for user {DiscordUserId}", ent.DiscordRole, ent.DiscordUserId);
                return StatusCode(StatusCodes.Status502BadGateway, "Failed to revoke Discord role.");
              }
            }

            _entitlementService.RemoveByStripeCustomerId(customerId);
            _logger.LogWarning("Subscription deleted for customer {CustomerId}, entitlement revoked", customerId);
          }

          auditCustomerId = sub.CustomerId;
          auditSubscriptionId = sub.Id;
          auditStatus = "canceled";
          handledSuccessfully = true;
          break;
        }

      case "customer.subscription.created":
        {
          var sub = DeserializeStripeObject<Stripe.Subscription>(stripeEvent, "Stripe.Subscription");
          auditCustomerId = sub?.CustomerId;
          auditSubscriptionId = sub?.Id;
          auditStatus = sub?.Status;
          handledSuccessfully = true;
          break;
        }

      default:
        _logger.LogInformation("Stripe webhook event acknowledged without state mutation: {EventType}", stripeEvent.Type);
        handledSuccessfully = true;
        break;
    }

    _auditStore.Append(new StripeSubscriptionState
    {
      EventId = stripeEvent.Id,
      EventType = stripeEvent.Type,
      CustomerId = auditCustomerId,
      SubscriptionId = auditSubscriptionId,
      Status = auditStatus,
      EntitlementActive = auditStatus is "active" or "trialing",
      UpdatedAt = DateTimeOffset.UtcNow
    });

    if (handledSuccessfully)
    {
      _eventStore.MarkProcessed(stripeEvent.Id, stripeEvent.Type);
    }

    return Ok(new
    {
      received = true,
      duplicate = false,
      eventId = stripeEvent.Id,
      eventType = stripeEvent.Type
    });
  }

  private T? DeserializeStripeObject<T>(Event stripeEvent, string label) where T : class
  {
    try
    {
      if (stripeEvent.Data.Object is T typed)
      {
        return typed;
      }

      return JsonSerializer.Deserialize<T>(stripeEvent.Data.Object.ToString() ?? "{}");
    }
    catch (JsonException ex)
    {
      _logger.LogWarning(ex, "Failed to deserialize {StripeObjectLabel}", label);
    }
    catch (InvalidCastException ex)
    {
      _logger.LogWarning(ex, "Failed to deserialize {StripeObjectLabel}", label);
    }
    catch (FormatException ex)
    {
      _logger.LogWarning(ex, "Failed to deserialize {StripeObjectLabel}", label);
    }
    catch (NotSupportedException ex)
    {
      _logger.LogWarning(ex, "Failed to deserialize {StripeObjectLabel}", label);
    }

    return null;
  }

  private static bool TryValidateCanonicalCheckoutMetadata(
    IDictionary<string, string>? metadata,
    out string lookupKey,
    out string packageKey,
    out string interval,
    out string error)
  {
    lookupKey = "";
    packageKey = "";
    interval = "";
    error = "";

    if (metadata is null)
    {
      error = "metadata_missing";
      return false;
    }

    if (!metadata.TryGetValue("pricing_authority", out var authority) ||
        !string.Equals(authority, StripePricingLoader.CanonicalPricingAuthority, StringComparison.Ordinal))
    {
      error = "pricing_authority_mismatch";
      return false;
    }

    if (!metadata.TryGetValue("lookup_key", out lookupKey) ||
        !CanonicalEntitlements.TryGetValue(lookupKey, out var expected))
    {
      error = "lookup_key_invalid";
      return false;
    }

    if (!metadata.TryGetValue("package_key", out packageKey) ||
        !string.Equals(packageKey, expected.PackageKey, StringComparison.Ordinal))
    {
      error = "package_key_mismatch";
      return false;
    }

    if (!metadata.TryGetValue("interval", out interval) ||
        !string.Equals(interval, expected.Interval, StringComparison.Ordinal))
    {
      error = "billing_interval_mismatch";
      return false;
    }

    return true;
  }

  private DateTime? GetCurrentPeriodEnd(Subscription sub)
  {
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
