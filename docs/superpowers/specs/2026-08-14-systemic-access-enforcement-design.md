# Systemic Access Enforcement Design

## Goal

Make the existing `.jpv/governance/systemic-access-hygiene.json` policy an enforced runtime invariant rather than passive metadata.

## Architecture

The gateway will add a focused `SystemicAccess` service boundary with four responsibilities: load and validate the policy at boot, classify access inventory deterministically, reconcile invalid state on a bounded schedule, and emit append-only audit receipts. Runtime code will depend on interfaces so provider-specific revocation can be added without weakening the core decision engine.

## Components

- `SystemicAccessPolicyLoader`: reads `.jpv/governance/systemic-access-hygiene.json`, validates required identifiers/actions/safeguards, and blocks boot on malformed or weakened policy.
- `SystemicAccessClassifier`: maps access records to `VALID`, `QUARANTINE`, `REVOKE`, `ROTATE`, `DEDUPLICATE`, `EXPIRE`, or `REVIEW` using normalized state.
- `ISystemicAccessInventorySource`: supplies access records. The first source is the existing entitlement repository; additional identity/session/integration providers can plug in behind the same interface.
- `ISystemicAccessActionProvider`: executes reversible, authorized actions. The initial entitlement provider removes verified expired/revoked entitlements; uncertain/high-impact records remain quarantined/review-only.
- `SystemicAccessAuditStore`: appends JSONL receipts under the application data directory with prior state, evidence, action, timestamp, and resulting state.
- `SystemicAccessReconciliationService`: hosted background service that runs once at startup and then on a configurable interval, defaulting to 15 minutes.
- `/health`: reports whether the systemic-access policy loaded successfully and the most recent reconciliation summary without exposing sensitive identifiers.

## Data Flow

policy load → startup validation → inventory discovery → normalization/deduplication → classification → action provider → audit receipt → regression summary.

## Safety and Authority

No person-target interpretation is permitted. Unknown or materially ambiguous records default to `QUARANTINE`/`REVIEW`; destructive actions require verified invalid state. The reconciler never modifies founder/break-glass state through generic inventory providers. Provider actions are idempotent and bounded to their own resource type.

## Error Handling

Missing or malformed policy blocks startup. Individual provider failures do not silently pass: the cycle records a failed audit receipt and reports degraded reconciliation state. A failed cycle does not retry faster than the configured interval.

## Testing

Use xUnit tests for policy validation and classification. Add a reconciliation test using in-memory fakes to verify that verified invalid access is acted on, uncertain state is quarantined/reviewed, receipts are written, and valid state is untouched. CI runs `dotnet test` for the gateway test project.

## Completion Criteria

The gap is closed when startup fails on missing/weakened policy, reconciliation executes automatically, invalid entitlement state is removed through the provider boundary, uncertain state is preserved for review, audit receipts are produced, health exposes reconciliation status, and CI verifies the behavior.
