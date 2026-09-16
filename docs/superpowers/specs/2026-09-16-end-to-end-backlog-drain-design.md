# End-to-End JPV Backlog Drain Design

## Status
Approved in conversation on 2026-09-16 under the standing instruction: if valid for JPV, approve and implement.

## Objective
Recover existing prepared public-records, regulatory-complaint, and oversight packages from JPV records; normalize them into canonical UPIS requests; bind execution to JPV_NATIVE/JPV_DEPLOY only; execute all routes that are actually executable; persist auditable receipts/evidence; and return only genuine human-required gates or genuinely missing source material.

## Non-negotiable invariants

- JPV_NATIVE / JPV_DEPLOY is the only runtime/deployment authority for this workflow.
- External provider availability never confers runtime authority.
- No provider substitution when JPV-native execution is missing; report the smallest real JPV-native blocker.
- Do not reconstruct missing package content from memory when canonical source material exists or should exist.
- Do not mark any item SUBMITTED without tracking ID, timestamp, canonical request ID, package hash, and verifiable transport/portal evidence.
- Do not bypass CAPTCHA, MFA, identity confirmation, terms acceptance, or legally required attestations.
- Every multi-recipient package is split into independently auditable canonical requests.
- Duplicates are suppressed using canonical package hash, submission fingerprint, and existing receipts.
- Human burden is minimized to irreducible gates only.

## Architecture

### 1. Package recovery
A recovery layer queries JPV source records and repository artifacts for each seeded backlog item. Recovery output is a `RecoveredPackage` containing:

- source identity and canonical provenance,
- complete request body,
- recipient/authority metadata,
- attachments and attachment hashes,
- legal basis/profile,
- any pre-existing transmission or receipt evidence.

Recovery is evidence-driven. If exact source material cannot be recovered, the item remains `ACTION_REQUIRED:MISSING_CANONICAL_SOURCE` rather than being rewritten from recollection.

### 2. Recipient expansion
A recipient-expansion layer converts multi-recipient packages into one canonical request per destination. Shared substantive content retains the same source provenance while each derived request receives its own request ID, recipient authority, route, fingerprint, and lifecycle.

### 3. JPV-native runtime binding
All execution routes bind through a JPV-native worker adapter. The access gateway may describe semantic browser operations, but deployment and runtime selection are delegated only to JPV_DEPLOY / the JPV-native target router. No Vercel, Replit, Cloudflare, or other vendor-named runtime path may be selected as execution authority.

If no verified JPV-native target exists, execution fails closed with `JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE` and identifies the smallest required infrastructure repair.

### 4. Execution
The drain pipeline runs:

`recover -> normalize -> expand recipients -> deduplicate -> resolve authority -> execute -> capture evidence -> reconcile state`

Executable EMAIL/API routes execute directly through their registered JPV adapters. Portal routes execute through the JPV-native browser worker using scoped ephemeral sessions. Human gates return a resumable `ACTION_REQUIRED` object.

### 5. Evidence and reconciliation
Every successful submission appends a tamper-evident evidence record including:

- canonical request ID,
- recipient authority,
- package hash,
- attachment hashes,
- route and transport,
- external tracking ID,
- receipt timestamp,
- submission fingerprint,
- confirmation URL and/or screenshot hash,
- resulting lifecycle state.

The queue is reconciled after every run. Terminally submitted/resolved work is removed from the active manual queue but remains in the audit ledger.

## Seed backlog scope
The first production drain targets the currently seeded queue:

1. Frontier AI DOJ FOIA
2. Frontier AI California CDT PRA
3. Frontier AI European Commission access-to-documents request
4. Manhattan DA seized-domain/FOIL records request
5. FTC antitrust complaint package
6. Stars & Stripes accountability / records / oversight package
7. CLARITY 2.0 congressional package
8. IB Challenger / IBA / Umar Kremlev / Donald Trump Jr. oversight package
9. Remaining recipient copies from the four-FOIA / bipartisan oversight package

## Queue state model

- `RECOVERY_PENDING`
- `READY_TO_ROUTE`
- `ACTION_REQUIRED:MISSING_CANONICAL_SOURCE`
- `ACTION_REQUIRED:HUMAN_GATE`
- `ACTION_REQUIRED:JPV_NATIVE_RUNTIME_CAPACITY_UNAVAILABLE`
- `SUBMITTED`
- `RESOLVED`

## Security model

- Credentials, cookies, tokens, passwords, and MFA secrets never enter canonical request objects, repository configuration, receipts, or logs.
- Session providers return scoped ephemeral handles only.
- Browser workers receive semantic field operations and opaque session handles.
- Evidence storage is append-only and hash-linked.
- Attachments are verified by SHA-256 before and after transport.

## Failure behavior

The batch never aborts because one destination fails. Each item is isolated and reconciled independently. Unknown authorities, missing canonical sources, unavailable JPV-native runtime capacity, and human gates each produce precise non-terminal states with no fabricated progress.

## Acceptance criteria

The implementation is complete only when:

1. Recovery has been attempted against JPV records for every seeded item.
2. Multi-recipient packages are expanded into explicit destination manifests where source evidence supports them.
3. All execution paths are bound to JPV-native runtime authority.
4. The production drain has been invoked.
5. Every item ends in `SUBMITTED`, `RESOLVED`, or a precise `ACTION_REQUIRED:*` state.
6. Every submitted item has durable receipt/evidence records.
7. The active manual queue contains only irreducible human gates or missing canonical-source blockers.
8. Repository tests and JPV governance checks pass before merge.
