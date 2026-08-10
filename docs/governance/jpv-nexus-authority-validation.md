# JPV Nexus Authority Validation

Status: Active validator  
Owner: JayPVentures LLC / JPV-OS governance authority  
Repository: `JayPVentures-LLC/jpv-os-access-gateway`

## Purpose

JPV Nexus owns application entry, access routing, entitlement state, role mapping, and audit handoff. It must not become the pricing authority, public marketing authority, legal review authority, or evidence-record authority.

This validator keeps JPV Nexus inside its lane.

## Authority contract

Machine-readable contract:

```text
authority/nexus-authority.json
```

Validator:

```bash
node scripts/validate-jpv-nexus-authority.mjs
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

JPV Nexus may reference lookup keys and route checkout behavior.

JPV Nexus must not define final prices. Final pricing authority belongs to governance and the public-site pricing standard.

## Drift blocked by this validator

The validator blocks:

- missing access states
- missing entitlement roles
- missing audit events
- Nexus-owned pricing language
- stale price examples in commercial access setup
- missing statement that JPV Nexus must not define final prices

## Operating rule

Access changes must produce auditable state transitions.

JPV Nexus records should answer:

1. Who requested access?
2. Which access path was requested?
3. What payment or review event occurred?
4. Which entitlement state changed?
5. Which role was granted, changed, or revoked?
6. Which audit event records the action?
