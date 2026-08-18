# GitHub Organization Mutation Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a least-privilege GitHub App-backed service that reconciles canonical JPV GitHub team topology into observed organization state and emits durable receipts.

**Architecture:** `jaypVLabs/JPV-OS` remains canonical governance/desired-state authority. `JayPVentures-LLC/jpv-os-access-gateway` authenticates as a GitHub App installation, reads canonical topology, computes a deterministic delta, mutates only authorized organization-team state, reads back provider state, and records a receipt. Provider access never creates authority.

**Tech Stack:** .NET 8, ASP.NET Core dependency injection/BackgroundService, System.Net.Http, System.Text.Json, System.Security.Cryptography, xUnit, GitHub REST API version 2026-03-10.

**Spec:** `jaypVLabs/JPV-OS/docs/superpowers/specs/2026-08-18-github-organization-mutation-service-design.md`

## Global Constraints

- `CONNECTOR_VISIBILITY != REPOSITORY_AUTHORITY`.
- GitHub App installation authentication only in production; no founder PAT fallback.
- Secrets/tokens are never committed or written to receipts.
- Target organizations are allowlisted: `jaypVLabs` and `JayPVentures-LLC`.
- Parent-only teams receive no direct repository permissions.
- Owner promotion is outside unattended reconciliation.
- `VERIFIED` requires post-write provider readback equal to canonical desired state.
- Fail closed on missing canonical topology, auth, organization scope, or unsupported provider capability.

---

### Task 1: Contracts, canonical topology parsing, and deterministic planning

**Files:**
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubOrgMutationContracts.cs`
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubOrgMutationPlanner.cs`
- Test: `tests/JPVOS.Tests/GitHubOrgMutationPlannerTests.cs`

**Interfaces:**
- Produces `GitHubTopology`, `GitHubOrganizationTopology`, `GitHubObservedTeam`, `GitHubTeamMutation`, `GitHubReconciliationPlan`, and `GitHubOrgMutationPlanner.Plan(...)`.
- Planner must create missing parent teams before child teams, update wrong parent assignments, produce no mutations for an already matching topology, and reject organizations outside the canonical topology.

- [ ] Write planner tests first.
- [ ] Verify CI fails because planner/contracts do not yet exist.
- [ ] Implement the minimal contracts and deterministic planner.
- [ ] Verify planner tests pass.

### Task 2: GitHub App authentication and provider client

**Files:**
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubAppAuthentication.cs`
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubOrganizationClient.cs`
- Test: `tests/JPVOS.Tests/GitHubAppAuthenticationTests.cs`

**Interfaces:**
- `GitHubAppTokenProvider.GetInstallationTokenAsync(long installationId, CancellationToken)` returns a short-lived installation token.
- `IGitHubOrganizationClient` reads teams, creates teams, and updates parent relationships using `/orgs/{org}/teams` routes.
- Requests use `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2026-03-10`, and bearer installation authentication.

- [ ] Write JWT/config boundary tests first.
- [ ] Verify CI fails for missing authentication implementation.
- [ ] Implement RS256 GitHub App JWT generation and installation-token exchange without external JWT packages.
- [ ] Implement typed provider client with non-secret error reporting.
- [ ] Verify tests pass.

### Task 3: Canonical-state loader, reconciler, receipt store, and runtime wiring

**Files:**
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubCanonicalTopologyLoader.cs`
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubOrganizationReconciler.cs`
- Create: `src/JPVOS/Services/GitHubOrgMutation/GitHubOrgMutationRuntime.cs`
- Modify: `src/JPVOS/Program.cs`
- Test: `tests/JPVOS.Tests/GitHubOrganizationReconcilerTests.cs`

**Interfaces:**
- Loader retrieves `governance/platform/github-team-topology.v1.json` from `jaypVLabs/JPV-OS` using the jaypVLabs installation token and validates `authority == "jaypVLabs/JPV-OS"` and `status == "CANONICAL_DESIRED_STATE"`.
- Reconciler reads observed teams, plans, applies team create/parent mutations, rereads, and emits `VERIFIED` only on equality.
- Runtime records append-only JSONL receipts under `audit/github-org-mutation-receipts.jsonl` and exposes non-secret health state.

- [ ] Write reconciliation/readback tests first.
- [ ] Verify CI fails for missing runtime implementation.
- [ ] Implement loader, reconciler, receipt store, and hosted service.
- [ ] Register services in `Program.cs` and extend `/health` with GitHub mutation readiness without exposing secrets.
- [ ] Verify all tests/build pass.

### Task 4: Provider capability and operational verification

**Files:**
- No secret material committed.
- Runtime configuration keys: `JPV_GITHUB_APP_ID`, `JPV_GITHUB_APP_PRIVATE_KEY_PEM`, `JPV_GITHUB_INSTALLATION_JPVLABS`, `JPV_GITHUB_INSTALLATION_ENTERPRISE`, `JPV_GITHUB_ORG_RECONCILIATION_MINUTES`.

- [ ] Open PR from `feat/github-org-mutation-service` to `main`.
- [ ] Require repository CI/build/test and governance inheritance checks.
- [ ] Confirm the GitHub App has Organization Members write plus repository Metadata read / Administration write only if repository-permission reconciliation is enabled.
- [ ] Confirm installation IDs bind to `jaypVLabs` and `JayPVentures-LLC`.
- [ ] Configure secrets through the governed runtime secret plane, never through repository files.
- [ ] Run live reconciliation.
- [ ] Read back both organizations and retain a `VERIFIED` receipt before marking the service operational.
