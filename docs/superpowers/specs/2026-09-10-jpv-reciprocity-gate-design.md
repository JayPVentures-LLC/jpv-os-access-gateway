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
The decision is evaluated synchronously at protected-access admission. No polling or watcher is introduced. The evaluator returns an access decision, reason code, evidence references, and whether the subject is exempt. `HEALTHY`, `IMBALANCED`, and `REMEDIATION` continue access; `RESTRICTED` denies value-increasing discretionary operations while preserving remediation/settlement paths; `REVOKED` denies discretionary JPV access.

## Auditability
Every enforcement decision is deterministic, reversible, and auditable. Records contain subject ID, requested resource, prior state, resulting state, reason code, evidence references, timestamp, and decision source. Unknown or materially uncertain evidence fails to `REMEDIATION`, not automatic revocation.

## Governance
Canonical policy lives in `JayPVentures-LLC/jpv-governance`; JPV-OS inherits and evaluates it; the access gateway enforces it. Runtime enforcement is autonomous after deployment. Production-impacting code still follows repository review and validation requirements.
