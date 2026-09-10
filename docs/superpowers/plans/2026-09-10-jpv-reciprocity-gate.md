# JPV Reciprocity Gate v1 — Implementation Plan

**Goal:** Make discretionary JPV access contingent on evidence-based reciprocity without retaliation, unlawful interference, founder execution fallback, or polling.

**Architecture:** Canonical reciprocity policy is inherited from governance. A synchronous evaluator classifies a subject into HEALTHY, IMBALANCED, REMEDIATION, RESTRICTED, or REVOKED and converts that state into a protected-access decision. The gateway enforces only JPV-owned discretionary access and emits deterministic audit data.

**Tech Stack:** .NET/C#, JSON governance policy, existing JPV SystemicAccess architecture.

**Spec:** `docs/superpowers/specs/2026-09-10-jpv-reciprocity-gate-design.md`

## Tasks
1. Add failing unit coverage for state transitions, exemptions, uncertainty, restoration, and access decisions.
2. Add reciprocity contracts and evaluator with deterministic transition rules.
3. Add synchronous admission gate and JSONL audit receipt store.
4. Register the gate in the application runtime without adding a hosted watcher.
5. Add inherited gateway policy and canonical governance policy.
6. Add JPV-OS inheritance/evaluator contract.
7. Validate JSON, source invariants, diff scope, and available repository checks.
8. Open reviewable PRs; do not bypass required independent review or production gates.

## Global Constraints
- No GitHub Actions dependency.
- No automatic action against third-party property/accounts.
- No contractual or legal bypass.
- No person-targeted retaliation.
- No restriction from a single adverse observation.
- Material uncertainty routes to REMEDIATION, never automatic revocation.
- Restoration is automatic when verified obligations are satisfied.
- Founder override is exceptional; normal runtime remains autonomous.
