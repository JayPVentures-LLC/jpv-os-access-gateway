# JPV Nexus Privileged-Action Governance Design

## Decision

Extend the existing JPV Nexus identity/entitlement fabric and systemic-access enforcement into one canonical privileged-action governance contract. Do not create a parallel security subsystem.

## Purpose

Make sensitive actions deterministic, phishing-resistant, least-privilege, auditable, and fail-closed while keeping routine work friction-light.

Canonical sequence:

`verified identity -> entitlement -> action-risk classification -> step-up requirement -> authorization decision -> execution -> provider readback -> durable receipt`

## Scope

Applies to JPV Nexus privileged operations across production infrastructure, identity/recovery, credentials/secrets, entitlements, finance authority, destructive data operations, governance-policy changes, and break-glass invocation.

Routine read/write operations that remain within existing entitlements do not require privileged step-up unless a provider or policy explicitly raises the action class.

Federal classified work is out of scope for this fabric. If JPV later receives a sponsored classified requirement, it must use a dedicated enclave and government-mandated clearance/handling requirements.

## Risk Classes

- `ROUTINE`: existing authenticated session and entitlement are sufficient.
- `ELEVATED`: reauthentication/step-up required when policy or provider sensitivity warrants it.
- `PRIVILEGED`: phishing-resistant step-up required before authorization.
- `BREAK_GLASS`: explicit emergency invocation, narrow scope, short TTL, immutable receipt, and mandatory post-event review.

## Privileged Action Classes

The following are always at least `PRIVILEGED`:

- production deployment or execution-authority changes;
- secret, credential, signing-key, token, or recovery-method changes;
- identity-provider or account-recovery changes;
- entitlement grants, privilege elevation, or access-boundary weakening;
- financial authority, payout, bank-routing, or high-impact payment configuration changes;
- destructive or irreversible data operations;
- governance-policy weakening, bypass, or enforcement disablement;
- break-glass activation.

## Authentication

Preferred privileged step-up is a phishing-resistant cryptographic factor: passkey/WebAuthn platform authenticator or hardware security key. Biometrics may be used only as the local user-verification mechanism bound to the cryptographic credential.

Voice verification must never satisfy privileged or break-glass authorization by itself. It may be recorded as an auxiliary emergency signal only when the primary cryptographic requirement has already been satisfied or when a separately governed recovery process explicitly permits it.

## Authorization

Authorization requires all of:

1. verified current identity;
2. valid current entitlement for the action/resource;
3. risk classification at or below the authenticated assurance level;
4. fresh step-up evidence when required;
5. no conflicting quarantine/review state;
6. explicit break-glass invocation when emergency authority is used.

Unknown, stale, contradictory, or materially ambiguous state defaults to `BLOCK` or `REVIEW`; the system never guesses upward into authority.

## Break-Glass

Break-glass is not standing privilege. Each invocation must include a reason, exact requested scope, issuer identity, issued-at timestamp, expiration timestamp, and resulting provider actions.

Requirements:

- explicit invocation only;
- narrowest feasible scope;
- maximum TTL of 30 minutes unless a stricter provider limit applies;
- no generic mutation of founder identity or recovery state;
- immutable audit receipt;
- provider readback before terminal success;
- mandatory post-event review state;
- automatic expiry and session invalidation at TTL end;
- credential/session rotation when compromise is suspected or the provider action requires it.

## Execution and Provider Readback

An authorization decision is not completion. Privileged actions must produce:

`desired action -> provider execution -> observed provider state -> reconciliation -> terminal receipt`

Provider execution failure, stale readback, or mismatched observed state produces `DEGRADED`/`FAILED`, never `PASS`.

## Audit Receipt

Every privileged and break-glass action must append a durable receipt containing at minimum:

- receipt ID;
- actor identity subject;
- action and resource;
- risk class;
- entitlement decision;
- authentication assurance and step-up method;
- decision timestamp;
- desired state;
- provider execution result;
- observed state/readback;
- terminal status;
- previous-receipt hash or equivalent tamper-evident linkage where supported.

Sensitive credential material must never be written into receipts.

## Systemic-Access Integration

The existing systemic-access reconciler remains authoritative for post-action hygiene. Expired/revoked access is removed, uncertain state is quarantined, provider divergence is surfaced, and founder/break-glass state is excluded from generic destructive reconciliation.

## Failure Semantics

- malformed or weakened policy: startup/enforcement `BLOCK`;
- missing step-up for privileged action: `DENY`;
- expired step-up: `DENY`;
- ambiguous entitlement: `REVIEW`/`QUARANTINE`;
- provider write failure: `FAILED`;
- provider readback mismatch: `DEGRADED`;
- break-glass expiry: revoke emergency authority and invalidate associated session scope.

## Testing

Tests must cover risk classification, privileged step-up enforcement, voice-only denial, entitlement denial, break-glass TTL/scope, provider-readback failure, audit-receipt emission, and regression compatibility with existing systemic-access reconciliation.

## Completion Criteria

The design is complete when JPV Nexus has one deterministic authorization path for routine/elevated/privileged/break-glass actions; privileged actions cannot execute without phishing-resistant step-up; break-glass is narrow and expiring; provider readback gates terminal success; durable receipts are emitted; systemic reconciliation remains intact; and CI verifies the behavior.