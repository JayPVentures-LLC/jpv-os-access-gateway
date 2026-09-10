# JPV Reciprocity Gate v1 — Design

## Purpose
Prevent persistent one-way extraction of discretionary JPV value. JPV-owned access remains available while reciprocal obligations are healthy and is progressively restricted when verified evidence shows persistent uncompensated extraction.

## Authority boundary
The gate may act only on JPV-owned or JPV-administered discretionary access. It may not interfere with third-party property or accounts, violate contracts or law, retaliate against people, or withdraw public/open resources that JPV has committed to keep public. Founder-peer rights, legal obligations, contractual obligations, public/open resources, and explicit exemptions are outside automatic revocation.

## Evidence model
Each subject has a value-exchange record containing JPV value delivered, reciprocal value returned, obligations, evidence references, observation count, and exemptions. Material reciprocal value includes money, contracted value, labor, infrastructure, credits, services, formal access, data, or another explicitly accepted contribution. Praise, attention, or vague support alone does not satisfy a material reciprocal obligation.

## State machine
`HEALTHY -> IMBALANCED -> REMEDIATION -> RESTRICTED -> REVOKED`.

A single adverse observation cannot produce `RESTRICTED` or `REVOKED`. Restriction requires persistent verified imbalance and an opportunity for remediation unless an existing contract or security rule independently authorizes immediate restriction. Revocation requires continued verified imbalance after remediation and restriction. Verified fulfillment restores `HEALTHY` automatically.

## Runtime
The decision is evaluated synchronously at a protected JPV resource grant. No polling or watcher is introduced. The evaluator returns an access decision and reason code from evidence-backed ledger state. `HEALTHY`, `IMBALANCED`, and `REMEDIATION` continue access; `RESTRICTED` denies value-increasing discretionary operations while preserving remediation/settlement paths; `REVOKED` denies discretionary JPV access. The first concrete enforcement integration is the JPV-managed Discord entitlement-role grant/removal path.

## Auditability
Every enforcement decision is deterministic, reversible, and auditable. Receipts contain subject ID, requested resource, resulting state, decision, reason code, evidence references, and timestamp. The persisted ledger preserves the evidence and progression fields needed to reevaluate and restore access. Unknown or materially uncertain evidence routes to `REMEDIATION`, not automatic revocation.

## Governance
Canonical policy lives in `JayPVentures-LLC/jpv-governance`; JPV-OS inherits and evaluates it; the access gateway enforces it. Runtime enforcement is autonomous after deployment. Production-impacting code still follows repository review and validation requirements.
