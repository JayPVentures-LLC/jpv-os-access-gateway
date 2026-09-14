# JPV Proposal & Reform Execution Standard Implementation Plan

Status: Implementation complete on PR #207; final security/check gates and merge receipt pending.

**Goal:** Implement a governed, append-only proposal lifecycle registry that separates submission, acknowledgment, adoption, implementation, verification, outcome, and revision while preserving authority and evidence truth.

**Architecture:** Add a focused `ProposalExecution` domain under the existing ASP.NET JPV-OS application. Use immutable lifecycle events persisted in SQLite, deterministic projections, a validator/state machine, and a bootstrap manifest for known JPV proposal families. Reuse existing repository governance/evidence patterns; do not add a second gateway or new vendor dependency.

**Tech Stack:** .NET 8, C#, Microsoft.Data.Sqlite, xUnit, ASP.NET Core, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-13-jpv-proposal-reform-execution-standard-design.md`

## Global Constraints

- No legal or external-authority findings may be fabricated.
- `submitted`, `acknowledged`, `adopted`, `implementation`, `verified`, and outcome-success states require distinct evidence.
- Enterprise, creator, labs, and public-facing lanes remain explicit.
- Protected records are private by default.
- Persistence is append-only and deterministic.
- Existing claims/evidence infrastructure remains authoritative for evidence records where referenced.
- Implementation lands via PR and exact-head verification.

## Task 1: Domain contracts and lifecycle validator — complete

- [x] Add proposal status, lane, class, evidence classification, lifecycle events, authority, adoption-route, obligation, verification, review, outcome, and lineage contracts.
- [x] Add lifecycle tests that reject unsupported stage skipping and require evidence/authority for material transitions.
- [x] Implement the lifecycle validator and evidence prerequisites.

## Task 2: Deterministic projection and append-only SQLite store — complete

- [x] Add deterministic projection/replay tests.
- [x] Add monotonic sequencing and idempotency coverage.
- [x] Implement SQLite append-only event persistence and deterministic projection.
- [x] Correct the shared unsupported SQLite transaction API exposed by the new CI gate in both proposal and claims/evidence stores.

## Task 3: Registry service, bootstrap truthfulness, and inheritance — complete

- [x] Implement registry operations for evidence, authority, adoption route, obligations, verification requirements, independent review, outcomes, lineage, release review, and status.
- [x] Add conservative bootstrap records for known JPV proposal families without asserting unsupported external outcomes.
- [x] Add proposal-execution inheritance binding.
- [x] Register durable proposal execution services in the JPV runtime.

## Task 4: Machine-readable status boundary and operator documentation — complete

- [x] Add a read-only public-safe proposal status endpoint.
- [x] Require explicit release review before public status is exposed.
- [x] Add operator documentation.
- [x] Add a repository .NET build/test workflow and repair pre-existing build/test failures exposed by it.
- [x] Add CodeQL analysis and extend Dependabot coverage to NuGet and GitHub Actions.

## Task 5: Review, merge, and exact-head receipt — in progress

- [x] Open implementation PR #207 after design PR #206 was merged separately.
- [ ] Confirm final .NET CI and CodeQL checks on the final PR head.
- [ ] Inspect changed files/review threads and resolve any blocking review state.
- [ ] Merge using the expected final head SHA.
- [ ] Verify `main` exact head equals the merge result and record the receipt in PR #207.
