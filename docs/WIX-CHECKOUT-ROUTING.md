# Wix Checkout Routing

This document describes how to configure Wix checkout URLs for the JPV-OS Access Gateway pricing packages.

## Overview

The JPV-OS Access Gateway uses Wix as the commerce and checkout layer for public-facing package purchases. This replaces the previous Stripe checkout integration with a more flexible configuration-based approach.

## Configuration

Wix checkout URLs are configured via environment variables or application configuration (appsettings.json).

### Environment Variable Pattern

```
WIX_CHECKOUT_URL_{PACKAGE_KEY}_{INTERVAL}
```

Or for packages without interval variations:

```
WIX_CHECKOUT_URL_{PACKAGE_KEY}
```

### Package Keys

The following package keys are used in the system:

- `COMMUNITY` - Member Access package
- `VIP` - Creator Launch package  
- `PARTNER_PACKAGE` - Partner Package
- `ENTERPRISE_INFRASTRUCTURE` - Enterprise Infrastructure package
- `CUSTOM_IMPLEMENTATION` - Custom Implementation package

### Intervals

For subscription-based packages:

- `MONTHLY` - Monthly billing
- `ANNUAL` - Annual billing

## Example Configuration

### Environment Variables (.env or hosting platform)

```bash
# Member Access
WIX_CHECKOUT_URL_COMMUNITY_MONTHLY=https://yoursite.wixsite.com/checkout/member-monthly
WIX_CHECKOUT_URL_COMMUNITY_ANNUAL=https://yoursite.wixsite.com/checkout/member-annual

# Creator Launch
WIX_CHECKOUT_URL_VIP_MONTHLY=https://yoursite.wixsite.com/checkout/creator-monthly
WIX_CHECKOUT_URL_VIP_ANNUAL=https://yoursite.wixsite.com/checkout/creator-annual

# Partner Package (contact-based, placeholder)
WIX_CHECKOUT_URL_PARTNER_PACKAGE=/contact

# Enterprise Infrastructure (contact-based, placeholder)
WIX_CHECKOUT_URL_ENTERPRISE_INFRASTRUCTURE=/contact

# Custom Implementation (contact-based, placeholder)
WIX_CHECKOUT_URL_CUSTOM_IMPLEMENTATION=/contact
```

### appsettings.json

```json
{
  "WIX_CHECKOUT_URL_COMMUNITY_MONTHLY": "https://yoursite.wixsite.com/checkout/member-monthly",
  "WIX_CHECKOUT_URL_COMMUNITY_ANNUAL": "https://yoursite.wixsite.com/checkout/member-annual",
  "WIX_CHECKOUT_URL_VIP_MONTHLY": "https://yoursite.wixsite.com/checkout/creator-monthly",
  "WIX_CHECKOUT_URL_VIP_ANNUAL": "https://yoursite.wixsite.com/checkout/creator-annual",
  "WIX_CHECKOUT_URL_PARTNER_PACKAGE": "/contact",
  "WIX_CHECKOUT_URL_ENTERPRISE_INFRASTRUCTURE": "/contact",
  "WIX_CHECKOUT_URL_CUSTOM_IMPLEMENTATION": "/contact"
}
```

## Usage in Code

The `WixCheckoutConfig` service is injected and used to retrieve checkout URLs:

```csharp
// Inject the service
[Inject] private WixCheckoutConfig WixConfig { get; set; } = default!;

// Get checkout URL for a package
var checkoutUrl = WixConfig.GetCheckoutUrl("community", "monthly");

// Check if checkout is configured
if (WixConfig.IsConfigured("community", "monthly"))
{
    // Proceed with checkout
}
```

## Fallback Behavior

If a checkout URL is not configured, the system falls back to:

1. Generic package URL (without interval): `WIX_CHECKOUT_URL_{PACKAGE_KEY}`
2. Default placeholder: `/checkout-not-configured`

## Security Notes

- **Never commit Wix checkout URLs or secrets to source control**
- Use environment variables or secure configuration management
- Wix checkout pages should be configured on the Wix platform with appropriate access controls
- For production deployments, ensure checkout URLs point to production Wix pages

## Contact-Based Packages

Some packages (Partner, Enterprise, Custom) are not self-service checkouts. For these:

- Configure URLs to point to contact forms or intake pages
- Or disable the checkout button and display "Contact for access" messaging
- The system supports both approaches

## Testing

To test checkout routing locally:

1. Set environment variables for the packages you want to test
2. Run the application: `dotnet run --project src/JPVOS/JPVOS.csproj`
3. Navigate to `/pricing`
4. Verify checkout buttons route to the correct URLs

## Migration from Stripe

This configuration replaces the previous Stripe checkout integration. The Stripe API controllers remain for webhook processing (for existing subscriptions), but new checkouts route through Wix.

### Key Differences

- **Stripe**: Backend checkout session creation, redirect to Stripe
- **Wix**: Direct link to Wix checkout pages, no backend session creation

This simplified approach reduces server-side complexity and gives more control over the checkout experience through Wix's platform.
