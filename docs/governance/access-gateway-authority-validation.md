# Access Gateway Authority Validation

Status: Active validator  
Owner: JayPVentures LLC / JPV-OS governance authority  
Repository: `JayPVentures-LLC/jpv-os-access-gateway`

## Purpose

The access gateway owns access routing, entitlement state, role mapping, and audit handoff. It must not become the pricing authority, public marketing authority, legal review authority, or evidence-record authority.

This validator keeps the gateway inside its lane.

## Authority contract

Machine-readable contract:

```text
authority/access-gateway-authority.json
```

Validator:

```bash
node scripts/validate-access-gateway-authority.mjs
```

## Required access states

- requested
- checkout_started
- payment_confirmed
- active
- past_due
- cancelled
- revoked
- manual_review

## Required entitlement roles

- Free Access
- Member Access
- VIP Venture
- Creator Lane
- Operator Access
- Enterprise
- Sovereign Review

## Required audit events

- access_requested
- checkout_started
- payment_confirmed
- entitlement_granted
- entitlement_changed
- payment_failed
- subscription_cancelled
- entitlement_revoked
- manual_review_required

## Pricing authority rule

The gateway may reference lookup keys and route checkout behavior.

The gateway must not define final prices. Final pricing authority belongs to governance and the public-site pricing standard.

## Drift blocked by this validator

The validator blocks:

- missing access states
- missing entitlement roles
- missing audit events
- gateway-owned pricing language
- stale price examples in commercial access setup
- missing statement that gateway must not define final prices

## Operating rule

Access changes must produce auditable state transitions.

Gateway records should answer:

1. Who requested access?
2. Which access path was requested?
3. What payment or review event occurred?
4. Which entitlement state changed?
5. Which role was granted, changed, or revoked?
6. Which audit event records the action?
