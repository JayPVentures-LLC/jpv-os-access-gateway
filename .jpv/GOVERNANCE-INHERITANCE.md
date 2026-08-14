# JPV-OS Governance Inheritance

This repository inherits canonical enterprise governance from `JayPVentures-LLC/jpv-governance` and runtime authority from `jaypVLabs/JPV-OS`.

Canonical enterprise authority binds this repository from the canonical source. Local copies and receipts are implementation and audit evidence; they are not activation prerequisites and may not silently redefine or weaken inherited state.

## Canonical correction state

The merged canonical correction `JPV-CORRECTION-2026-08-12-PATCH-ACCUMULATION` is inherited transitively.

The governing lifecycle is:

`evidence → falsification/correction → canonical state mutation → dependency invalidation → reconciliation → execution`

The following pattern is not a valid correction path:

`failure → new local document → new local validator → new Gate layer → manual propagation`

If an inherited correction invalidates an assumption used by this repository, affected execution is stale until reconciled. Missing local write access does not suspend canonical authority. A new local mechanism requires genuinely distinct legal, evidentiary, or operational semantics; recurrence of a failure is not enough.

## Existing canonical sources

- `policies/PEOPLE-MISSION-TECHNOLOGY-TOOL.md`
- `governance/glossary/JPV-OS-CANONICAL-TERMS.md`
- `governance/schemas/people-first-decision-receipt.schema.json`
- `governance/policies/CANONICAL-DOCTRINE-INHERITANCE.md`
- `docs/superpowers/specs/2026-08-12-canonical-correction-state-design.md`

People are the mission. Systems are the method. Technology is the tool.

Belief is not governed. Conduct and verified impact are governed only when material harm or substantiated material risk activates JPV-OS review.

Conflicting local language is invalid. This repository may add implementation-specific requirements but may not weaken, reverse, or silently redefine canonical doctrine.
