# Systemic Access Enforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce the existing systemic-access-hygiene policy at runtime with deterministic classification, scheduled reconciliation, auditable actions, health reporting, and CI tests.

**Architecture:** Add a small `SystemicAccess` service boundary inside the ASP.NET Core gateway. Boot validation is fail-closed; runtime reconciliation is provider-based and defaults uncertainty to non-destructive quarantine/review.

**Tech Stack:** .NET 8, ASP.NET Core hosted services, System.Text.Json, xUnit, existing SQLite entitlement repository.

## Global Constraints

- No person-target interpretation.
- Uncertain/high-impact state defaults to `QUARANTINE` or `REVIEW`.
- Destructive actions require verified invalid state.
- Founder/break-glass access is never modified through generic providers.
- Every transition emits an audit receipt.
- Policy weakening or absence blocks startup.

---

### Task 1: Tests and policy loader

**Files:**
- Create: `tests/JPVOS.Tests/JPVOS.Tests.csproj`
- Create: `tests/JPVOS.Tests/SystemicAccessPolicyTests.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessPolicy.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessPolicyLoader.cs`
- Modify: `src/JPVOS/JPVOS.csproj`

**Interfaces:**
- Produces: `SystemicAccessPolicyLoader.LoadAndValidate(string path) -> SystemicAccessPolicy`.

- [ ] Write tests that reject missing files, malformed JSON, wrong policy ID, missing required actions, and `person_targeting=true`.
- [ ] Run `dotnet test tests/JPVOS.Tests/JPVOS.Tests.csproj` and verify RED.
- [ ] Implement the loader and policy model.
- [ ] Run the same tests and verify GREEN.

### Task 2: Deterministic classification

**Files:**
- Create: `tests/JPVOS.Tests/SystemicAccessClassifierTests.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessRecord.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessDecision.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessClassifier.cs`

**Interfaces:**
- Produces: `SystemicAccessClassifier.Classify(SystemicAccessRecord record) -> SystemicAccessDecision`.

- [ ] Test valid, expired, revoked, duplicate, compromised, stale, orphaned/unowned, and uncertain records.
- [ ] Verify RED.
- [ ] Implement minimal deterministic classifier using normalized state and explicit precedence: compromised→ROTATE/REVOKE, revoked→REVOKE, expired→EXPIRE, duplicate→DEDUPLICATE, stale/orphaned/unowned→QUARANTINE, uncertainty→REVIEW, otherwise VALID.
- [ ] Verify GREEN.

### Task 3: Inventory/action provider and audit receipts

**Files:**
- Create: `tests/JPVOS.Tests/SystemicAccessReconcilerTests.cs`
- Create: `src/JPVOS/Services/SystemicAccess/ISystemicAccessInventorySource.cs`
- Create: `src/JPVOS/Services/SystemicAccess/ISystemicAccessActionProvider.cs`
- Create: `src/JPVOS/Services/SystemicAccess/EntitlementAccessProvider.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessAuditStore.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessReconciler.cs`

**Interfaces:**
- `ISystemicAccessInventorySource.GetRecordsAsync(CancellationToken) -> IReadOnlyCollection<SystemicAccessRecord>`.
- `ISystemicAccessActionProvider.CanHandle(SystemicAccessRecord) -> bool`.
- `ISystemicAccessActionProvider.ApplyAsync(SystemicAccessRecord, SystemicAccessDecision, CancellationToken) -> SystemicAccessActionResult`.
- `SystemicAccessReconciler.RunOnceAsync(CancellationToken) -> SystemicAccessReconciliationSummary`.

- [ ] Test that valid records are untouched, verified expired/revoked entitlement records are removed, review/quarantine records are preserved, and every decision is audited.
- [ ] Verify RED.
- [ ] Implement provider, reconciler, and JSONL audit store.
- [ ] Verify GREEN.

### Task 4: Startup/runtime wiring and health

**Files:**
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessReconciliationService.cs`
- Create: `src/JPVOS/Services/SystemicAccess/SystemicAccessRuntimeState.cs`
- Modify: `src/JPVOS/Program.cs`
- Modify: `src/JPVOS/JPVOS.csproj`

**Interfaces:**
- Hosted service runs immediately then every `JPV_SYSTEMIC_ACCESS_RECONCILIATION_MINUTES` minutes, default 15.
- `/health` includes non-sensitive `systemicAccess` status.

- [ ] Add integration-oriented tests for startup policy validation and runtime-state updates.
- [ ] Verify RED.
- [ ] Register loader, classifier, provider, reconciler, audit store, runtime state, and hosted service.
- [ ] Copy `.jpv/governance/systemic-access-hygiene.json` to build/publish output and fail boot if unavailable.
- [ ] Verify GREEN.

### Task 5: CI gate and verification

**Files:**
- Create: `.github/workflows/systemic-access-enforcement.yml`

- [ ] Configure PR/push workflow to restore, build with warnings as errors for the test project, and run `dotnet test tests/JPVOS.Tests/JPVOS.Tests.csproj --configuration Release`.
- [ ] Open PR and verify GitHub Actions completes successfully.
- [ ] Merge only after successful checks or report the exact failing job/log evidence.
