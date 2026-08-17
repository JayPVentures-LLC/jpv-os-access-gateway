# Completion Verification Invariant

Status: REQUIRED
Authority: JPV governance
Applicability: JayPVentures LLC, jaypventures, jaypVLabs, JPV-OS, JPV Institute, and every repository, runtime, service, workflow, artifact, deployment, and public or internal operating surface governed by JPV.

## Terminal rule

No claim of completion without scope enumeration + exact artifact/SHA + execution evidence where applicable + independent/provider readback + explicit PASS against the current authoritative state.

## Required evidence chain

A work item may advance to `DONE` only when all applicable evidence exists: scope enumeration; exact immutable artifact identity; execution evidence where operational behavior is affected; independent or authoritative provider readback; and explicit PASS against the current authoritative state.

## State semantics

Use only evidence-supported states: `PLANNED`, `IMPLEMENTED_NOT_MERGED`, `MERGED_NOT_EXECUTED`, `BLOCKED`, `FAILED`, `UNVERIFIED`, `PASS`, `DONE`.

`DONE` is reserved for work satisfying every applicable requirement in this invariant.

## Prohibited completion claims

Never represent partial scope, stale revisions, unexecuted merges, unverified mutations, or unavailable readback as complete. When verification cannot be completed, report the exact blocker and the last evidence-backed state.

## Precedence

This invariant is cross-entity and applies wherever a narrower repository or operating rule is less strict. A stricter domain-specific verification rule may add requirements but may not remove these requirements.
