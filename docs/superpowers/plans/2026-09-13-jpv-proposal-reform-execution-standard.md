# JPV Proposal & Reform Execution Standard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a governed, append-only proposal lifecycle registry that separates submission, acknowledgment, adoption, implementation, verification, outcome, and revision while preserving authority and evidence truth.

**Architecture:** Add a focused `ProposalExecution` domain under the existing ASP.NET JPV-OS application. Use immutable lifecycle events persisted in SQLite, deterministic projections, a validator/state machine, and a bootstrap manifest for known JPV proposal families. Reuse existing repository governance/evidence patterns; do not add a second gateway or new vendor dependency.

**Tech Stack:** .NET 8, C#, Microsoft.Data.Sqlite, xUnit, existing ASP.NET dependency injection.

**Spec:** `docs/superpowers/specs/2026-09-13-jpv-proposal-reform-execution-standard-design.md`

## Global Constraints

- No legal or external-authority findings may be fabricated.
- `submitted`, `acknowledged`, `adopted`, `implementation`, `verified`, and outcome-success states require distinct evidence.
- Enterprise, creator, labs, and public-facing lanes remain explicit.
- Protected records are private by default.
- Persistence is append-only and deterministic.
- Existing claims/evidence infrastructure remains authoritative for evidence records where referenced.
- Implementation lands via PR and exact-head verification.

---

### Task 1: Domain contracts and lifecycle validator

**Files:**
- Create: `src/JPVOS/Services/ProposalExecution/ProposalExecutionTypes.cs`
- Create: `src/JPVOS/Services/ProposalExecution/ProposalExecutionContracts.cs`
- Create: `src/JPVOS/Services/ProposalExecution/ProposalLifecycleValidator.cs`
- Test: `tests/JPVOS.Tests/ProposalLifecycleValidatorTests.cs`

**Interfaces:**
- Produces `ProposalStatus`, `ProposalLane`, `ProposalClass`, `ProposalEvidenceClass`, `ProposalEventType`, `ProposalLifecycleEvent`, `ProposalProjection`, `AuthorityAssignment`, `ImplementationObligation`, `OutcomeMeasurement`, `IProposalLifecycleValidator`.

- [ ] Write tests proving invalid stage skipping fails and evidence-backed submission/adoption/verification transitions pass.
- [ ] Run CI and verify the tests fail because production types do not exist.
- [ ] Implement minimal enums, records, transition rules, and evidence prerequisites.
- [ ] Run CI and verify green.

### Task 2: Deterministic projection and append-only SQLite store

**Files:**
- Create: `src/JPVOS/Services/ProposalExecution/ProposalProjector.cs`
- Create: `src/JPVOS/Services/ProposalExecution/SqliteProposalEventStore.cs`
- Test: `tests/JPVOS.Tests/ProposalExecutionStoreTests.cs`

**Interfaces:**
- Produces `IProposalEventStore.AppendAsync`, `IProposalEventStore.ReadStreamAsync`, `ProposalProjector.Project`.

- [ ] Write tests for monotonic sequencing, idempotency, replay, and supersession lineage.
- [ ] Verify red in CI.
- [ ] Implement SQLite schema, transactional append, idempotency table, stream read, and deterministic projection.
- [ ] Verify green in CI.

### Task 3: Registry service, bootstrap truthfulness, and inheritance

**Files:**
- Create: `src/JPVOS/Services/ProposalExecution/ProposalRegistryService.cs`
- Create: `governance/proposals/JPV-PROPOSAL-REGISTRY.bootstrap.json`
- Create: `JPV-PROPOSAL-EXECUTION-INHERITANCE.json`
- Test: `tests/JPVOS.Tests/ProposalRegistryServiceTests.cs`
- Modify: `src/JPVOS/Program.cs`

**Interfaces:**
- Produces `IProposalRegistryService.RegisterAsync`, `RecordAsync`, `GetAsync` and DI registrations.

- [ ] Write tests proving bootstrap records never overstate exploratory/submitted states and registry writes are validated.
- [ ] Verify red in CI.
- [ ] Implement service, bootstrap records, inheritance manifest, and dependency injection.
- [ ] Verify green plus existing test suite.

### Task 4: Machine-readable status boundary and operator documentation

**Files:**
- Create: `src/JPVOS/Api/ProposalRegistryController.cs`
- Create: `docs/governance/PROPOSAL-EXECUTION-OPERATIONS.md`
- Test: `tests/JPVOS.Tests/ProposalRegistryControllerTests.cs`

**Interfaces:**
- Produces bounded read endpoint `GET /api/proposals/{proposalId}/status` with publication filtering.

- [ ] Write tests proving private fields are not returned and unknown records return 404.
- [ ] Verify red in CI.
- [ ] Implement read-only controller and public projection DTO.
- [ ] Verify green and run full repository checks.

### Task 5: Review, merge, and exact-head receipt

- [ ] Update PR #206 to include implementation commits.
- [ ] Inspect changed files and review threads.
- [ ] Verify required checks and full test suite pass.
- [ ] Merge using expected head SHA.
- [ ] Verify `main` exact head equals the merge result and record the receipt in the PR discussion.
