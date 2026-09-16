# Authenticated Portal Execution Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the universal intake gateway with a replaceable authenticated-browser execution boundary that can submit portal-backed requests without weakening receipt, provenance, or human-authorization guarantees.

**Architecture:** Keep the universal intake core transport-neutral. Add a PORTAL adapter that delegates browser interaction to an injected worker contract; portal profiles describe semantic fields and evidence requirements, while worker-specific selectors remain outside the governance core. A request reaches `SUBMITTED` only after the worker returns verifiable receipt evidence; CAPTCHA, MFA, identity confirmation, terms acceptance, or unknown portal states fail closed into `ACTION_REQUIRED` with a resumable handoff.

**Tech Stack:** Node.js ESM, `node:test`, JSON portal profiles, dependency-injected browser worker.

**Spec:** Approved portal-execution design in the 2026-09-16 JPV universal-intake workflow; builds on merged PRs #214 and #215.

## Global Constraints

- Preserve canonical request ID and provenance.
- No portal selectors in the transport-neutral core.
- No CAPTCHA/MFA bypass or fabricated user interaction.
- Human-required states must return `ACTION_REQUIRED`, never `SUBMITTED`.
- `SUBMITTED` requires external tracking ID, receipt timestamp, and portal evidence.
- Submission retries must carry an idempotency fingerprint.
- Every lossy field transformation must be surfaced before submission.
- No direct mutation of `main`; independent PR review/checks required.

---

### Task 1: Portal Worker Contract

**Files:**
- Create: `governance/universal-intake-portal.mjs`
- Test: `tests/universal-intake-portal.test.mjs`

**Interfaces:**
- Produces: `executePortal(request, authority, transport, deps)`.
- Worker input: canonical request, semantic portal profile, idempotency fingerprint.
- Worker output: submitted receipt/evidence or structured human-required state.

- [ ] Write failing tests for successful portal receipt, CAPTCHA/MFA handoff, unknown state, provenance preservation, and duplicate fingerprint generation.
- [ ] Confirm RED.
- [ ] Implement the minimal worker contract.
- [ ] Confirm GREEN.

### Task 2: Integrate PORTAL Transport

**Files:**
- Modify: `governance/universal-intake-adapters.mjs`
- Test: `tests/universal-intake-adapters.test.mjs`

- [ ] Write failing test proving an executable PORTAL delegates to the worker.
- [ ] Write failing test proving human-required worker output remains `ACTION_REQUIRED`.
- [ ] Implement PORTAL dispatch without changing EMAIL/API behavior.
- [ ] Confirm GREEN.

### Task 3: Semantic Portal Profiles

**Files:**
- Create: `governance/portal-profiles/ca-cdt-pra.json`
- Create: `governance/portal-profiles/eu-commission-1049.json`
- Modify: `governance/universal-intake-authorities.json`
- Test: `tests/universal-intake-portal-profiles.test.mjs`

- [ ] Define semantic field maps and receipt evidence requirements for California CDT and European Commission access-to-documents.
- [ ] Do not encode brittle DOM selectors in governance files.
- [ ] Mark PORTAL routes executable only through a registered authenticated worker.
- [ ] Confirm profiles validate and authority records point to them.

### Task 4: End-to-End Fail-Closed Execution

**Files:**
- Modify: `tests/universal-intake-e2e.test.mjs`

- [ ] Test canonical request → CDT portal profile → worker → verified receipt → `SUBMITTED`.
- [ ] Test canonical request → EU portal → MFA/CAPTCHA/identity requirement → resumable `ACTION_REQUIRED` handoff.
- [ ] Run all universal-intake tests and repository checks.
- [ ] Open PR with verification evidence and require independent review before merge.
