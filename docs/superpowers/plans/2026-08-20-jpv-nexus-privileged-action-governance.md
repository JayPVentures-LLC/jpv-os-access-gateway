# JPV Nexus Privileged-Action Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend JPV Nexus with deterministic privileged-action risk classification, phishing-resistant step-up enforcement, bounded break-glass authorization, provider readback, and durable receipts without creating a parallel security subsystem.

**Architecture:** Add a focused `PrivilegedActions` boundary that consumes current Nexus identity/entitlement state and produces an authorization decision before provider execution. Reuse systemic-access audit/reconciliation semantics, fail closed on ambiguity, and keep provider execution behind interfaces so readback gates terminal success.

**Tech Stack:** .NET/C#, ASP.NET Core, existing JPV Nexus services, xUnit, JSON policy files, PowerShell/CI regression checks.

**Spec:** `docs/superpowers/specs/2026-08-20-jpv-nexus-privileged-action-governance-design.md`

## Global Constraints

- Do not create a second identity or entitlement authority.
- Unknown/stale/contradictory state must never elevate authority.
- `PRIVILEGED` and `BREAK_GLASS` require phishing-resistant cryptographic step-up.
- Voice verification cannot satisfy privileged authorization by itself.
- Break-glass TTL is at most 30 minutes.
- Provider readback is required before terminal `PASS`.
- Existing systemic-access reconciliation and founder/break-glass protections remain intact.
- Sensitive credential material must never enter audit receipts.

---

### Task 1: Privileged action policy and domain types

**Files:**
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionTypes.cs`
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionPolicy.cs`
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionPolicyLoader.cs`
- Create: `.jpv/governance/privileged-action-governance.json`
- Test: `tests/JPVOS.Tests/PrivilegedActionPolicyTests.cs`

**Interfaces:**
- Produces `PrivilegedRiskClass`, `PrivilegedActionRequest`, `AuthenticationEvidence`, `PrivilegedActionDecision`, `PrivilegedActionPolicy`, and `IPrivilegedActionPolicyLoader`.

- [ ] Write failing tests proving malformed/weakened policy is rejected, privileged classes require phishing-resistant step-up, break-glass TTL cannot exceed 30 minutes, and voice-only assurance is insufficient.
- [ ] Run the focused policy tests and confirm failure before implementation.
- [ ] Implement the domain records/enums and strict JSON policy loader.
- [ ] Run focused tests and confirm PASS.
- [ ] Commit the task.

### Task 2: Risk classifier and authorization engine

**Files:**
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionClassifier.cs`
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionAuthorizer.cs`
- Test: `tests/JPVOS.Tests/PrivilegedActionAuthorizerTests.cs`

**Interfaces:**
- Consumes `PrivilegedActionRequest`, `AuthenticationEvidence`, current entitlement state, and policy.
- Produces a deterministic `PrivilegedActionDecision` with `ALLOW`, `DENY`, `REVIEW`, or `BREAK_GLASS_REQUIRED` and a reason code.

- [ ] Write failing tests for routine allow, missing entitlement denial, privileged action without phishing-resistant step-up denial, voice-only denial, expired step-up denial, ambiguous entitlement review, and valid privileged allow.
- [ ] Run focused tests and confirm failure.
- [ ] Implement the classifier and authorizer with no implicit privilege escalation.
- [ ] Run focused tests and confirm PASS.
- [ ] Commit the task.

### Task 3: Break-glass bounded authorization

**Files:**
- Create: `src/JPVOS/Services/PrivilegedActions/BreakGlassAuthorizationService.cs`
- Test: `tests/JPVOS.Tests/BreakGlassAuthorizationTests.cs`

**Interfaces:**
- Produces short-lived scoped `BreakGlassGrant` records containing reason, scope, issuer, issued-at, expiry, and post-event-review requirement.

- [ ] Write failing tests proving explicit reason/scope are required, TTL over 30 minutes is rejected, grants expire deterministically, and expired grants cannot authorize execution.
- [ ] Run focused tests and confirm failure.
- [ ] Implement bounded grant issuance/validation without mutating generic founder identity/recovery state.
- [ ] Run focused tests and confirm PASS.
- [ ] Commit the task.

### Task 4: Provider execution, readback, and durable receipts

**Files:**
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionExecutionContracts.cs`
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionExecutionService.cs`
- Create: `src/JPVOS/Services/PrivilegedActions/PrivilegedActionAuditStore.cs`
- Test: `tests/JPVOS.Tests/PrivilegedActionExecutionTests.cs`

**Interfaces:**
- `IPrivilegedActionProvider.ExecuteAsync(request, cancellationToken)` returns provider execution evidence.
- `IPrivilegedActionProvider.ReadBackAsync(request, cancellationToken)` returns observed state.
- `PrivilegedActionExecutionService.ExecuteAsync(...)` writes a terminal receipt only after authorization, provider execution, readback, and reconciliation comparison.

- [ ] Write failing tests for denied requests never reaching providers, provider-write failure => `FAILED`, stale/mismatched readback => `DEGRADED`, matching readback => `PASS`, and secret values excluded from receipt serialization.
- [ ] Run focused tests and confirm failure.
- [ ] Implement provider contracts, execution sequencing, readback comparison, and append-only JSONL receipts with tamper-evident previous-receipt hash linkage where feasible.
- [ ] Run focused tests and confirm PASS.
- [ ] Commit the task.

### Task 5: Nexus registration and systemic-access integration

**Files:**
- Modify: `src/JPVOS/Program.cs`
- Modify: `src/JPVOS/Services/SystemicAccess/SystemicAccessRuntime.cs` or the narrowest existing integration point after inspection.
- Test: `tests/JPVOS.Tests/SystemicAccessReconcilerTests.cs`
- Test: `tests/JPVOS.Tests/PrivilegedActionIntegrationTests.cs`

**Interfaces:**
- Registers one privileged-action authorization/execution path in DI.
- Preserves systemic-access reconciliation as post-action access hygiene.

- [ ] Write failing integration tests proving the service graph boots with valid policy, fails closed on invalid policy, privileged receipts coexist with systemic-access receipts, and generic reconciliation cannot destructively mutate founder/break-glass state.
- [ ] Run focused integration tests and confirm failure.
- [ ] Register services and integrate through the existing Nexus/SystemicAccess boundaries without creating duplicate identity or entitlement stores.
- [ ] Run focused integration tests and confirm PASS.
- [ ] Commit the task.

### Task 6: Canonical governance alignment and regression verification

**Files:**
- Modify in canonical JPV-OS authority: `governance/policies/systemic-access-hygiene.v1.json`
- Create in canonical JPV-OS authority: `governance/policies/privileged-action-governance.v1.json`
- Modify gateway inherited policy copy as required.
- Add or modify deterministic validation script under existing `scripts/` conventions.

**Interfaces:**
- Canonical policy declares privileged-action resource class, phishing-resistant step-up invariant, provider-readback requirement, break-glass TTL, and voice-only prohibition.

- [ ] Update canonical governance first, then inherited gateway policy references.
- [ ] Add regression validation that rejects weakened privileged-action safeguards.
- [ ] Run `dotnet test` for the full gateway test project.
- [ ] Run the repository's governance/PowerShell validation path on Windows/PWSH where available.
- [ ] Inspect provider/runtime readback and CI evidence; do not claim completion if unavailable or failing.
- [ ] Open governed pull requests for canonical policy and gateway implementation; merge only when required checks/review rules pass.
