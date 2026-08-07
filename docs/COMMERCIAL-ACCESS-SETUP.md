# JPV-OS Commercial Access Setup

## 1. Access Package Lookup Keys

The access gateway does not own final pricing authority.

Use governance-approved lookup keys and resolve payment processor price IDs server-side. Final prices come from the active JPV-OS canonical pricing authority. The gateway must fail closed when a generated processor map is stale, incomplete, or lacks the active authority marker.

| Package Key | Access Path | Billing Interval |
|---|---|---|
| member_access_monthly | Member Access | monthly |
| member_access_annual | Member Access | annual |
| creator_infrastructure_monthly | Creator Infrastructure | monthly |
| creator_infrastructure_annual | Creator Infrastructure | annual |
| partner_infrastructure_monthly | Partner Infrastructure | monthly |
| partner_infrastructure_annual | Partner Infrastructure | annual |
| enterprise_infrastructure_monthly | Enterprise Infrastructure | monthly |
| enterprise_infrastructure_annual | Enterprise Infrastructure | annual |

Legacy lookup keys including `vip_venture_*`, `creator_lane_*`, `operator_monthly`, and `enterprise_monthly` are prohibited for new checkout activity.

## 2. Stripe Products & Prices to Create

Do not wire frontend directly to Stripe `price_id` values. Use lookup-key based checkout and generated pricing maps.

Canonical lookup keys:

- member_access_monthly
- member_access_annual
- creator_infrastructure_monthly
- creator_infrastructure_annual
- partner_infrastructure_monthly
- partner_infrastructure_annual
- enterprise_infrastructure_monthly
- enterprise_infrastructure_annual

Canonical flow:

`Frontend -> lookup_key -> CheckoutController -> StripePricingLoader -> canonical authority validation -> infrastructure/stripe/generated/stripe-pricing.{mode}.json -> Stripe price_id`

`StripePricingLoader` validates the map against `JPV-OS-v2.1.0` before any checkout session can be created. A missing authority marker, legacy lookup key, wrong amount, wrong interval, wrong currency, missing product ID, or missing price ID rejects checkout rather than using stale pricing.

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

Discord role labels are entitlement identifiers and are independent from commercial pricing lookup keys.

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

## 6. Validation Checklist

- [ ] Generated processor pricing map carries `pricing_authority=JPV-OS-v2.1.0`.
- [ ] All eight canonical subscription lookup keys exist.
- [ ] Member Access resolves to $20 monthly / $200 annual.
- [ ] Creator Infrastructure resolves to $500 monthly / $5,000 annual.
- [ ] Partner Infrastructure resolves to $2,500 monthly / $25,000 annual.
- [ ] Enterprise Infrastructure resolves to $10,000 monthly / $100,000 annual.
- [ ] No legacy pricing lookup key is accepted by runtime checkout.
- [ ] Checkout rejects stale or incomplete maps.
- [ ] Webhook delivery and entitlement state transitions remain auditable.
- [ ] No secrets are present in appsettings files or source code.

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

## 10. Production Deployment Invariant

Production checkout must not start unless the processor map passes canonical pricing validation. Price provisioning may create or replace processor price objects, but it may not lower, invent, or silently reuse a divergent amount.

## 11. Revocation Rules

- On payment failure or subscription cancellation, paid access roles are removed and entitlement is revoked.
- On downgrade, roles are updated to match the new access path.
- All revocation actions must be logged for audit.

---

Never commit real secrets or live price IDs to source control. Keep secrets in environment variables and resolve price IDs server-side from governed lookup keys.
