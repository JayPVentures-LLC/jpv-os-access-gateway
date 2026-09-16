# End-to-End JPV Backlog Drain Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover, normalize, split, route, execute, and reconcile the seeded JPV public-intake backlog through JPV-native execution only, leaving only irreducible human gates or genuinely missing canonical source material.

**Architecture:** Add a recovery/expansion layer in the access gateway, bind execution through a JPV-native runtime adapter, then feed recovered canonical requests into the existing `drainBacklog()` pipeline and append evidence to the existing ledger. Recovery is evidence-driven and fail-closed; multi-recipient packages are expanded deterministically; successful submissions require durable receipt evidence.

**Tech Stack:** Node.js ESM, `node:test`, existing UPIS/intake modules, GitHub-backed JPV records, JPV-native execution contracts.

**Spec:** `docs/superpowers/specs/2026-09-16-end-to-end-backlog-drain-design.md`

## Global Constraints

- JPV_NATIVE / JPV_DEPLOY is the only runtime/deployment authority for this workflow.
- External provider availability never confers runtime authority.
- No provider substitution when JPV-native execution is missing; report the smallest real JPV-native blocker.
- Do not reconstruct missing package content from memory when canonical source material exists or should exist.
- Do not mark any item SUBMITTED without tracking ID, timestamp, canonical request ID, package hash, and verifiable transport/portal evidence.
- Do not bypass CAPTCHA, MFA, identity confirmation, terms acceptance, or legally required attestations.
- Every multi-recipient package is split into independently auditable canonical requests.
- Duplicates are suppressed using canonical package hash, submission fingerprint, and existing receipts.
- Human burden is minimized to irreducible gates only.

---

### Task 1: Canonical Package Recovery

**Files:**
- Create: `governance/universal-intake-recovery.mjs`
- Test: `tests/universal-intake-recovery.test.mjs`

**Interfaces:**
- Produces: `recoverBacklogPackages(seedItems, sourceRecords)` -> array of `{ source_id, status, package?, blocker? }`.
- `sourceRecords` entries use `{ source_id, request, attachments?, recipients?, receipts? }`.

- [ ] **Step 1: Write failing tests**

Cover exact-source recovery, pre-existing receipt preservation, missing-source fail-closed behavior, and attachment hash preservation.

```js
const result = recoverBacklogPackages(seed,[record]);
assert.equal(result[0].status,'RECOVERED');
assert.equal(result[0].package.request.subject,record.request.subject);
assert.deepEqual(result[0].package.attachments,record.attachments);
```

Missing source must yield:

```js
assert.deepEqual(result[0].blocker,{code:'MISSING_CANONICAL_SOURCE',source_id:'x'});
```

- [ ] **Step 2: Run the recovery test and verify RED**

Run: `node --test tests/universal-intake-recovery.test.mjs`
Expected: module-not-found / missing export before implementation.

- [ ] **Step 3: Implement minimal recovery logic**

The implementation must index records by `source_id`, clone exact canonical source data, preserve attachments/receipts, and never synthesize substantive request text for missing records.

- [ ] **Step 4: Run test and verify GREEN**

Run the same command; expected PASS.

- [ ] **Step 5: Commit**

Commit message: `feat: add canonical backlog package recovery`

### Task 2: Recipient Expansion

**Files:**
- Create: `governance/universal-intake-recipient-expansion.mjs`
- Test: `tests/universal-intake-recipient-expansion.test.mjs`

**Interfaces:**
- Consumes recovered package shape from Task 1.
- Produces: `expandRecipients(recoveredPackage)` -> one canonical package per recipient with stable derived request IDs.

- [ ] **Step 1: Write failing tests**

Test single-recipient passthrough and multi-recipient deterministic expansion.

```js
const expanded = expandRecipients({
  source_id:'oversight-1',
  request:baseRequest,
  recipients:[
    {authority_id:'HOUSE_JUDICIARY',authority_name:'House Judiciary Committee'},
    {authority_id:'SENATE_JUDICIARY',authority_name:'Senate Judiciary Committee'}
  ]
});
assert.equal(expanded.length,2);
assert.notEqual(expanded[0].request.request_id,expanded[1].request.request_id);
assert.deepEqual(expanded[0].request.provenance,baseRequest.provenance);
```

- [ ] **Step 2: Verify RED**

Run: `node --test tests/universal-intake-recipient-expansion.test.mjs`

- [ ] **Step 3: Implement deterministic expansion**

Derived IDs must hash `{source request id/source id, authority id}` and preserve substantive request body/provenance unchanged apart from recipient-specific routing fields.

- [ ] **Step 4: Verify GREEN**

- [ ] **Step 5: Commit**

Commit message: `feat: split multi-recipient intake packages`

### Task 3: JPV-Native Runtime Binding

**Files:**
- Create: `governance/universal-intake-jpv-runtime.mjs`
- Test: `tests/universal-intake-jpv-runtime.test.mjs`
- Modify: `governance/universal-intake-browser-worker.mjs`

**Interfaces:**
- Produces: `createJpvNativeDriverFactory(deps)`.
- Consumes a `jpvDeploy` dependency that returns `{ target, revision, endpoint, receipt }` for an admitted JPV-native runtime target.

- [ ] **Step 1: Write failing tests**

Require JPV-native target admission, reject external/vendor-named targets, and fail closed with `JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE` when no verified target exists.

```js
await assert.rejects(
  () => factory({authority_id:'CA_CDT_PRA',session_handle:'s',fingerprint:'f'}),
  /JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE/
);
```

A valid dependency response must produce a driver adapter without exposing credentials.

- [ ] **Step 2: Verify RED**

Run: `node --test tests/universal-intake-jpv-runtime.test.mjs`

- [ ] **Step 3: Implement JPV-native binding**

Only targets with `platform === 'JPV_NATIVE'` and `verified === true` are accepted. Any provider/vendor-named runtime is rejected even if technically reachable.

- [ ] **Step 4: Wire browser worker dependency injection**

`createBrowserPortalWorker()` must be able to receive the JPV-native driver factory without changing portal semantics.

- [ ] **Step 5: Verify GREEN**

- [ ] **Step 6: Commit**

Commit message: `feat: bind intake portal execution to JPV native runtime`

### Task 4: Production Drain Orchestrator

**Files:**
- Create: `governance/universal-intake-production-drain.mjs`
- Test: `tests/universal-intake-production-drain.test.mjs`

**Interfaces:**
- Consumes: recovery, recipient expansion, `drainBacklog()`, `appendEvidence()`.
- Produces: `runProductionDrain({seedItems,sourceRecords,receipts,registry,deps})` -> `{items,summary,evidence}`.

- [ ] **Step 1: Write failing end-to-end tests**

Cases:
1. recovered email route -> `SUBMITTED` + evidence ledger append;
2. recovered portal route -> JPV-native worker -> `SUBMITTED` + evidence;
3. missing source -> `ACTION_REQUIRED:MISSING_CANONICAL_SOURCE`;
4. missing native capacity -> `ACTION_REQUIRED:JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE`;
5. MFA/CAPTCHA -> `ACTION_REQUIRED:HUMAN_GATE` with resumable handoff;
6. existing receipt -> duplicate suppression and no re-send.

- [ ] **Step 2: Verify RED**

Run: `node --test tests/universal-intake-production-drain.test.mjs`

- [ ] **Step 3: Implement orchestration**

Pipeline:

```text
recover -> expand -> import/dedupe -> authority resolve -> execute -> append evidence -> reconcile
```

Batch isolation is mandatory: one item failure must not abort later items.

- [ ] **Step 4: Verify GREEN**

- [ ] **Step 5: Commit**

Commit message: `feat: add production end-to-end intake drain`

### Task 5: Seeded Queue Recovery Manifest

**Files:**
- Create: `governance/backlog/2026-09-16-recovered-source-manifest.json`
- Modify: `governance/backlog/2026-09-16-manual-queue.json`
- Test: `tests/universal-intake-seeded-queue.test.mjs`

**Interfaces:**
- Manifest entries point to evidence-backed source records and recipient manifests recovered from JPV records.

- [ ] **Step 1: Retrieve exact JPV source records**

Search repository/library/conversation records for all nine seeded source IDs and their package names. Do not infer missing text from conversational recollection.

- [ ] **Step 2: Write failing manifest test**

Each seeded item must resolve to one of:
- `RECOVERED_SOURCE`
- `MISSING_CANONICAL_SOURCE`
- `RECIPIENT_MANIFEST_RECOVERED`
- `HUMAN_GATE_REQUIRED`

No ambiguous `PREPARED_PACKAGE_REQUIRED` placeholders remain after recovery attempt.

- [ ] **Step 3: Create recovered-source manifest from evidence**

Store references/metadata, not secrets. Include source path/ref, content hash when available, recipient manifest status, and prior receipt evidence if recovered.

- [ ] **Step 4: Verify GREEN**

Run: `node --test tests/universal-intake-seeded-queue.test.mjs`

- [ ] **Step 5: Commit**

Commit message: `data: reconcile seeded intake backlog sources`

### Task 6: First Production Drain Command and Receipt

**Files:**
- Create: `scripts/run-universal-intake-production-drain.mjs`
- Create: `docs/receipts/2026-09-16-universal-intake-production-drain.md`
- Test: `tests/universal-intake-production-command.test.mjs`

**Interfaces:**
- Command reads seeded/recovered manifests and invokes `runProductionDrain()` through JPV-native dependencies.
- Emits machine-readable JSON summary and non-secret receipt references.

- [ ] **Step 1: Write failing command test**

Require deterministic exit behavior:
- exit 0 if all items are `SUBMITTED`, `RESOLVED`, or precise `ACTION_REQUIRED:*` states;
- non-zero on schema/integrity failure or attempted external-provider runtime substitution.

- [ ] **Step 2: Verify RED**

Run: `node --test tests/universal-intake-production-command.test.mjs`

- [ ] **Step 3: Implement command**

The script must never claim a submission without evidence and must emit a run receipt containing counts by final state.

- [ ] **Step 4: Run targeted suite**

Run:

```bash
node --test \
  tests/universal-intake-recovery.test.mjs \
  tests/universal-intake-recipient-expansion.test.mjs \
  tests/universal-intake-jpv-runtime.test.mjs \
  tests/universal-intake-production-drain.test.mjs \
  tests/universal-intake-seeded-queue.test.mjs \
  tests/universal-intake-production-command.test.mjs
```

Expected: all PASS.

- [ ] **Step 5: Run existing universal-intake regression suite**

Run all `tests/universal-intake-*.test.mjs`.

- [ ] **Step 6: Execute first production drain through JPV-native runtime**

Persist resulting non-secret run receipt. Do not substitute any external provider if native runtime capacity is unavailable.

- [ ] **Step 7: Commit**

Commit message: `feat: execute first JPV native intake backlog drain`

### Task 7: Review Gate and Merge Admission

**Files:**
- No new production files unless review fixes are required.

- [ ] **Step 1: Open PR**

Summarize recovered sources, exact final queue states, submitted receipt counts, irreducible human gates, and any missing canonical-source blockers.

- [ ] **Step 2: Run repository CI/checks**

Treat repository checks and independent review as authoritative merge evidence.

- [ ] **Step 3: Do not claim completion before verification**

Completion requires merged code plus a verified production-drain receipt; an open PR alone is not end-to-end completion.
