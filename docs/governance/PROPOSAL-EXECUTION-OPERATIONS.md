# JPV Proposal Execution Operations

The proposal execution registry is the canonical machine-readable lifecycle for JPV proposals, collaboration concepts, grants, standards, referrals, and institutional review packets.

## Runtime

Production requires `JPV_PROPOSAL_DATA_DIR` to point to writable persistent storage. Development defaults to a temporary `jpv-os-proposals` directory. The SQLite database is `proposal-execution.db`.

## Lifecycle truth

The registry deliberately separates these states:

`researching → draft → validated → approved-internally → submitted → acknowledged → under-review → adopted → implementation → verification → verified`

Closure states (`rejected`, `withdrawn`, `superseded`, `closed`) are recorded independently. A later stage is not inferred from an earlier one.

Submission requires a submission record. Acknowledgment requires a distinct acknowledgment record. Adoption requires both supporting evidence and an authority reference. Verification requires implementation/verification evidence. Outcome measurements remain separate from verification.

## Proposal governance records

Each proposal can retain:

- authority assignments;
- evidence references;
- implementation obligations and completion-evidence requirements;
- outcome measurements;
- artifact/version lineage; and
- release-review state.

Lifecycle history is append-only. Current state is deterministically replayed from the event stream.

## Public boundary

`GET /api/proposals/{proposalId}/status` returns a bounded projection only after an explicit release review approves publication. The endpoint does not return evidence references, authority internals, implementation details, or private lineage.

## Bootstrap registry

`governance/proposals/JPV-PROPOSAL-REGISTRY.bootstrap.json` identifies known proposal families and conservative starting states. Bootstrap entries are descriptive migration inputs, not proof of external submission, acknowledgment, adoption, funding, partnership, or implementation.

## Verification

Repository implementation is complete only after tests pass, the PR is reviewed/merged, and the expected PR head is reconciled to the resulting `main` commit. External proposal outcomes require their own authoritative evidence and are never inferred from repository state.
