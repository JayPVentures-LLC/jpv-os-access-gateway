# Production Portal Workers and Backlog Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the merged portal boundary into production-grade worker orchestration with secure session isolation and a backlog importer that classifies and routes existing prepared packages.

**Architecture:** Keep credentials/session state out of canonical request objects. Add a driver-backed portal worker that consumes semantic portal profiles, a secure session-provider contract that returns ephemeral session handles, and a backlog importer that deduplicates prepared packages against receipts before routing. The worker must stop on human gates and return resumable state; it may mark `SUBMITTED` only with receipt evidence accepted by the merged #216 boundary.

**Tech Stack:** Node.js ESM, node:test, dependency-injected browser driver/session provider, JSON-compatible backlog records.

**Spec:** Builds on merged PRs #214, #215, and #216.

## Global Constraints
- Credentials and cookies never enter canonical request payloads, logs, receipts, or repository files.
- Session handles are scoped, ephemeral, and externally supplied.
- No CAPTCHA/MFA/identity/terms bypass.
- No selector data in governance core; driver mappings are worker-side configuration.
- Every submission must preserve request ID, provenance, fingerprint, receipt, and evidence.
- Backlog import must deduplicate against existing receipt fingerprints/tracking IDs.
- No direct main mutation; PR and repository checks required.

### Task 1: Secure session boundary
Create `governance/universal-intake-session.mjs` and tests covering ephemeral handle acquisition, scope enforcement, expiration, and secret stripping.

### Task 2: Driver-backed production portal worker
Create `governance/universal-intake-browser-worker.mjs` and tests for semantic field mapping, file upload delegation, pre-submit validation, human gates, submission evidence, and deterministic retry fingerprint propagation.

### Task 3: Backlog importer and classifier
Create `governance/universal-intake-backlog.mjs` and tests that normalize prepared items into canonical requests, deduplicate submitted/resolved work, and classify READY_TO_ROUTE / ACTION_REQUIRED / SUBMITTED / RESOLVED.

### Task 4: Integration
Extend end-to-end tests to exercise CDT/EU worker execution and backlog migration, then open a review-gated PR.