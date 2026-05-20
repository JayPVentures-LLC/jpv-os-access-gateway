# JPV-OS Commercial Access Setup

## 1. Package Pricing

| Package Key                      | Description                       | Billing Interval | Example Price |
|----------------------------------|-----------------------------------|-----------------|--------------|
| member_access_monthly            | Member Access (Monthly)           | monthly         | $10/mo       |
| member_access_annual             | Member Access (Annual)            | annual          | $100/yr      |
| vip_venture_monthly              | VIP Venture (Monthly)             | monthly         | $25/mo       |
| vip_venture_annual               | VIP Venture (Annual)              | annual          | $250/yr      |
| creator_lane_monthly             | Creator Lane (Monthly)            | monthly         | $100/mo      |
| operator_monthly                 | Operator (Monthly)                | monthly         | $500/mo      |
| enterprise_monthly               | Enterprise (Monthly)              | monthly         | Custom       |

## 2. Stripe Products & Prices to Create

Do not wire frontend directly to Stripe `price_id` values.
Use lookup-key based checkout and generated pricing maps.

Canonical lookup keys:
- member_access_monthly
- member_access_annual
- vip_venture_monthly
- vip_venture_annual
- creator_lane_monthly
- operator_monthly
- enterprise_monthly

Canonical flow:
`Frontend -> lookup_key -> CheckoutController -> StripePricingLoader -> infrastructure/stripe/generated/stripe-pricing.{mode}.json -> Stripe price_id`

## 3. Required Environment Variables

Set these as environment variables in your deployment environment (do NOT store secrets in appsettings.json):

- STRIPE_MODE
- STRIPE_SECRET_KEY
- STRIPE_WEBHOOK_SECRET
- DISCORD_CLIENT_ID
- DISCORD_CLIENT_SECRET
- DISCORD_BOT_TOKEN
- DISCORD_GUILD_ID
// Deprecated: DISCORD_ROLE_MEMBER
// Deprecated: DISCORD_ROLE_CREATOR
// Deprecated: DISCORD_ROLE_PARTNER
// Deprecated: DISCORD_ROLE_ENTERPRISE
- DISCORD_ROLE_CUSTOM
- DISCORD_REDIRECT_URI

## 4. Discord Roles to Create

Create the following roles in your Discord server and copy their IDs:

- Member
- Creator
- Partner
- Enterprise
- Custom

Assign the role IDs to the corresponding environment variables above.

## 5. Local Test Checklist

- [ ] Set all required environment variables in your .env or launch profile.
- [ ] Run `dotnet build` and `dotnet run`.
- [ ] Test Stripe checkout flow (use test keys).
- [ ] Test Stripe webhook (use Stripe CLI to forward events).
- [ ] Test Discord OAuth connection and role assignment.
- [ ] Confirm entitlement state updates on payment, cancellation, and failure.
- [ ] Confirm no secrets are present in appsettings files or source code.

## 6. Persistence Requirement

- **In-memory entitlement storage is NOT production-ready.**
- For production, you must use a persistent repository (e.g., SQLite, SQL Server, or cloud DB) for entitlements.
- The SqliteEntitlementRepository is provided for local/dev and small-scale production. For scale or compliance, use a managed database.

## 7. Backup/Export Requirement

- You must regularly back up the entitlement database (e.g., entitlements.db for SQLite).
- Implement export routines for compliance and disaster recovery.

## 8. Revocation Audit Requirement

- All revocations (cancellation, payment failure, Discord role removal) must be logged and auditable.
- Review entitlement and Discord role state regularly for consistency.

## 6. Production Deployment Checklist

- [ ] Set all environment variables in your production environment (Azure, AWS, etc).
- [ ] Use live Stripe keys and set `STRIPE_MODE=live`.
- [ ] Ensure `infrastructure/stripe/generated/stripe-pricing.test.json` exists for test-mode validation.
- [ ] Use live Discord bot token and client secret.
- [ ] Confirm HTTPS is enabled.
- [ ] Confirm Stripe webhook endpoint is reachable and secret is set.
- [ ] Confirm Discord bot is in your server and has Manage Roles permission.
- [ ] Confirm no secrets are present in appsettings files or source code.


## 9. Revocation Rules

- On payment failure or subscription cancellation, paid Discord roles are removed and entitlement is revoked.
- On downgrade, roles are updated to match new package.
- All revocation actions should be logged for audit.

---

**Never commit real secrets or live price IDs to source control. Always keep secrets in environment variables and resolve price IDs server-side from lookup keys.**
