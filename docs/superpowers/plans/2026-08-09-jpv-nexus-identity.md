# JPV Nexus Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Access Gateway product identity with JPV Nexus without changing routing, entitlement, or deployment behavior.

**Architecture:** Keep the existing Blazor/.NET application and authority model. Rename only the canonical product identity, authority contract, validator references, health identity, and public-facing application copy; preserve repository history and route semantics.

**Tech Stack:** .NET / Blazor, JSON authority contract, Node validation script, PowerShell regression validation, GitHub PR workflow.

## Global Constraints
- Product identity: `JPV Nexus`.
- Preserve routing, entitlement, role, audit, and deployment behavior.
- Do not create a duplicate application or parallel gateway implementation.
- Repository slug may remain `jpv-os-access-gateway` until an explicit repository-rename capability exists.
- PowerShell is the deterministic regression surface.

---

### Task 1: Add identity regression gate

**Files:**
- Create: `tests/Test-JPVNexusIdentity.ps1`

**Interfaces:**
- Consumes: active product files and authority contract.
- Produces: fail-closed identity validation.

- [ ] **Step 1:** Add a test that requires `JPV Nexus` in README, homepage, health endpoint, validator, and authority contract.
- [ ] **Step 2:** Add a test that rejects `JPV-OS Access Gateway` and `Access Gateway` as the active application name on those surfaces.
- [ ] **Step 3:** Verify the pre-change repository state would fail because current active surfaces still contain the old identity.

### Task 2: Replace canonical authority identity

**Files:**
- Create: `authority/nexus-authority.json`
- Modify: `scripts/validate-access-gateway-authority.mjs`
- Delete after migration: `authority/access-gateway-authority.json`

**Interfaces:**
- Consumes: existing access states, entitlement roles, and audit events.
- Produces: canonical Nexus authority contract with unchanged semantics.

- [ ] **Step 1:** Copy the existing authority semantics into `nexus-authority.json` with `system = "jpv-nexus"` and `product = "JPV Nexus"`.
- [ ] **Step 2:** Update the validator to read `authority/nexus-authority.json` and emit Nexus-named validation messages.
- [ ] **Step 3:** Retire the old authority contract after the validator points to the Nexus contract.

### Task 3: Replace runtime and public identity

**Files:**
- Modify: `README.md`
- Modify: `src/JPVOS/Components/Pages/Home.razor`
- Modify: `src/JPVOS/Pages/AccessRouting.razor`
- Modify: `src/JPVOS/Api/HealthController.cs`

**Interfaces:**
- Produces: JPV Nexus public/application identity.

- [ ] **Step 1:** Rename the README product heading and operational description to JPV Nexus.
- [ ] **Step 2:** Replace homepage product-name references with JPV Nexus while preserving route and entitlement copy.
- [ ] **Step 3:** Replace the disabled AccessRouting product label/copy with JPV Nexus terminology.
- [ ] **Step 4:** Change health endpoint `app` from `JPV-OS Access Gateway` to `JPV Nexus`.

### Task 4: Verify branch integrity

**Files:**
- Test: `tests/Test-JPVNexusIdentity.ps1`

- [ ] **Step 1:** Verify all intended files differ from `main` and no unrelated source behavior changed.
- [ ] **Step 2:** Verify active product surfaces use JPV Nexus and old product-name strings are absent from those surfaces.
- [ ] **Step 3:** Open a ready-for-review PR and merge only after the diff is clean and repository protections allow it.
