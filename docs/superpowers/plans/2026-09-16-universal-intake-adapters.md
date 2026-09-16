# Universal Intake Adapter Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the merged universal intake core into an executable routing layer with a capability registry, concrete email/API/manual-handoff adapters, and deterministic execution receipts.

**Architecture:** Preserve `governance/universal-public-intake.mjs` as the transport-neutral core. Add an authority capability registry and a small adapter layer that consumes the selected transport, executes only explicitly authorized machine routes, emits normalized receipts, and generates a complete manual handoff when no executable route exists.

**Tech Stack:** Node.js ESM, `node:test`, built-in `fetch`, JSON registry.

**Spec:** PR #214 merged architecture plus `governance/universal-public-intake-registry.json`.

## Global Constraints

- One canonical request; agency-specific behavior is adapter metadata.
- `SUBMITTED` requires auditable receipt evidence.
- Manual handoff is allowed only when no executable transport is registered.
- No silent field loss or truncation.
- Preserve canonical request ID and provenance across every route.
- No direct mutation of `main`; PR review gate required.

---

### Task 1: Authority Capability Registry

**Files:**
- Create: `governance/universal-intake-authorities.json`
- Create: `governance/universal-intake-registry.mjs`
- Test: `tests/universal-intake-registry.test.mjs`

**Interfaces:**
- Produces: `getAuthority(registry, authorityId)` and `resolveAuthorityTransport(request, registry)`.

- [ ] Write failing tests for authority lookup, executable route preference, manual fallback, and invalid registry rejection.
- [ ] Run `node --test tests/universal-intake-registry.test.mjs` and confirm RED.
- [ ] Implement minimal registry loader/resolver.
- [ ] Run test and confirm GREEN.

### Task 2: Adapter Execution Contract

**Files:**
- Create: `governance/universal-intake-adapters.mjs`
- Test: `tests/universal-intake-adapters.test.mjs`

**Interfaces:**
- Produces: `executeIntake(request, authority, deps)` returning `{request, transport, receipt|handoff}`.

- [ ] Write failing tests for email execution, API execution, manual handoff generation, and receipt requirements.
- [ ] Confirm RED.
- [ ] Implement dependency-injected email/API execution and normalized receipts.
- [ ] Confirm GREEN.

### Task 3: Initial Authority Coverage

**Files:**
- Modify: `governance/universal-intake-authorities.json`
- Test: `tests/universal-intake-authorities.test.mjs`

**Coverage:** NIST FOIA, FTC FOIA, UK DSIT FOI as executable email routes; DOJ FOIA, California CDT PRA, and European Commission access-to-documents as non-executable portal routes until an authenticated browser/API adapter is available.

- [ ] Write tests asserting exact route classifications and endpoint metadata.
- [ ] Confirm RED.
- [ ] Add authority records with source URLs and review dates.
- [ ] Confirm GREEN.

### Task 4: End-to-End Routing

**Files:**
- Create: `tests/universal-intake-e2e.test.mjs`

- [ ] Test canonical request → authority registry → executable route → normalized receipt.
- [ ] Test canonical request → portal-only authority → complete manual handoff without `SUBMITTED` state.
- [ ] Run all universal-intake tests.
- [ ] Open PR with verification evidence and require independent review before merge.
