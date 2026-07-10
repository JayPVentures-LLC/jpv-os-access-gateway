# JPV-OS Commercial Access Setup

## 1. Access Package Lookup Keys

The access gateway does not own final pricing authority.

Use governance-approved lookup keys and resolve payment processor price IDs server-side. Final prices must come from the active governance/public-site pricing standard, not this gateway document.

| Package Key | Access Path | Billing Interval |
|---|---|---|
| member_access_monthly | Member Access | monthly |
| member_access_annual | Member Access | annual |
| vip_venture_monthly | VIP Venture | monthly |
| vip_venture_annual | VIP Venture | annual |
| creator_lane_monthly | Creator Lane | monthly |
| operator_monthly | Operator Access | monthly |
| enterprise_monthly | Enterprise | monthly |
| sovereign_review | Sovereign Review | review |

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
- sovereign_review

Canonical flow:

`Frontend -> lookup_key -> CheckoutController -> StripePricingLoader -> infrastructure/stripe/generated/stripe-pricing.{mode}.json -> Stripe price_id`

## 3. Required Environment Variables

Set these as environment variables in your deployment environment. Do not store secrets in appsettings files or source code.

- STRIPE_MODE
- STRIPE_SECRET_KEY
- STRIPE_WEBHOOK_SECRET
- DISCORD_CLIENT_ID
- DISCORD_CLIENT_SECRET
- DISCORD_BOT_TOKEN
- DISCORD_GUILD_ID
- DISCORD_ROLE_FREE_ACCESS
- DISCORD_ROLE_MEMBER_ACCESS
- DISCORD_ROLE_VIP_VENTURE
- DISCORD_ROLE_CREATOR_LANE
- DISCORD_ROLE_OPERATOR
- DISCORD_ROLE_ENTERPRISE
- DISCORD_ROLE_SOVEREIGN_REVIEW
- DISCORD_REDIRECT_URI

Deprecated role variables:

- DISCORD_ROLE_MEMBER
- DISCORD_ROLE_CREATOR
- DISCORD_ROLE_PARTNER
- DISCORD_ROLE_CUSTOM

## 4. Discord Roles to Create

Create the following roles in your Discord server and copy their IDs:

- Free Access
- Member Access
- VIP Venture
- Creator Lane
- Operator Access
- Enterprise
- Sovereign Review

Assign the role IDs to the corresponding environment variables above.

## 5. Entitlement States

The access gateway recognizes these entitlement states:

- requested
- checkout_started
- payment_confirmed
- active
- past_due
- cancelled
- revoked
- manual_review

## 6. Local Test Checklist

- [ ] Set all required environment variables in your `.env` or launch profile.
- [ ] Run `dotnet build` and `dotnet run`.
- [ ] Test checkout flow with test keys.
- [ ] Test webhook delivery with a local forwarding tool.
- [ ] Test OAuth connection and role assignment.
- [ ] Confirm entitlement state updates on payment, cancellation, and failure.
- [ ] Confirm no secrets are present in appsettings files or source code.

## 7. Persistence Requirement

- In-memory entitlement storage is not production-ready.
- For production, use a persistent repository such as SQLite, SQL Server, or a managed database.
- The SqliteEntitlementRepository may be used for local/dev and small-scale production. For scale or compliance, use a managed database.

## 8. Backup / Export Requirement

- Back up the entitlement database regularly.
- Implement export routines for compliance and disaster recovery.

## 9. Audit Requirement

All access state changes and revocations must be logged and auditable.

Required audit events:

- access_requested
- checkout_started
- payment_confirmed
- entitlement_granted
- entitlement_changed
- payment_failed
- subscription_cancelled
- entitlement_revoked
- manual_review_required

Review entitlement and role state regularly for consistency.

## 10. Production Deployment Checklist

- [ ] Set all environment variables in the production environment.
- [ ] Use live payment keys only in production secrets.
- [ ] Ensure generated pricing maps exist for the intended mode.
- [ ] Use live OAuth/client secrets only in production secrets.
- [ ] Confirm HTTPS is enabled.
- [ ] Confirm webhook endpoint is reachable and webhook secret is set.
- [ ] Confirm bot permissions can manage roles.
- [ ] Confirm no secrets are present in appsettings files or source code.

## 11. Revocation Rules

- On payment failure or subscription cancellation, paid access roles are removed and entitlement is revoked.
- On downgrade, roles are updated to match the new access path.
- All revocation actions must be logged for audit.

---

Never commit real secrets or live price IDs to source control. Keep secrets in environment variables and resolve price IDs server-side from lookup keys.
