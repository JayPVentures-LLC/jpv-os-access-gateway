# JPV Proposal & Reform Execution Standard Design

Status: Approved architecture; written-spec confirmation required before implementation
Authority domain: JPV-OS / enterprise governance

## Purpose

Create one canonical JPV-OS execution layer that turns proposals, reforms, policy recommendations, institutional referrals, collaboration concepts, and grant initiatives into governed, auditable lifecycle records from evidence through verified outcome.

The standard SHALL eliminate proposal state reconstruction across decks, correspondence, repositories, and ad hoc notes. It SHALL provide deterministic status, authority, evidence, adoption, implementation, verification, outcome, and version-lineage records without overstating JPV authority or treating external submission as adoption.

The existing `jpv-os-access-gateway` remains the executable gateway. Existing claims/evidence infrastructure remains the evidence-case substrate. This subsystem SHALL reuse existing JPV governance, review, receipt, and evidence patterns rather than creating a parallel operating architecture.

## Architectural decision

Implement a logically isolated `ProposalExecution` domain within the existing JPV-OS application, backed by append-only lifecycle events and deterministic projections.

The canonical lifecycle is:

`Matter → Evidence → Problem → Principle → Proposed Standard → Authority → Adoption Route → Implementation → Verification → Outcome → Revision`

A proposal may stop at any valid stage, but the system SHALL preserve why it stopped, who or what authority owns the next decision, and what evidence is required to advance.

The standard SHALL distinguish:

- proposal existence from external submission;
- submission from acknowledgment;
- acknowledgment from review;
- review from adoption;
- adoption from implementation;
- implementation from verification; and
- verification from successful outcome.

No status may imply a later stage without explicit evidence for that transition.

## Core record model

Every governed proposal SHALL have one canonical `ProposalId` and one current projection derived from immutable lifecycle events.

The projection SHALL contain at minimum:

- canonical ID;
- title;
- proposal class;
- JPV lane: enterprise, creator, labs, or public-facing;
- current lifecycle stage;
- current status;
- owning JPV authority;
- external authority references where applicable;
- source artifacts;
- evidence references;
- adoption route;
- implementation obligations;
- verification requirements;
- outcome measures;
- revision state;
- version lineage;
- confidentiality/publication classification; and
- last verified timestamp.

## Proposal classes

The initial implementation SHALL support at least:

- `institutional-reform`;
- `public-policy`;
- `regulatory-referral`;
- `research-collaboration`;
- `grant-proposal`;
- `operational-standard`;
- `governance-standard`;
- `public-interest-framework`; and
- `commercial-or-strategic-collaboration`.

The class affects required authority and outcome fields but SHALL NOT create separate lifecycle engines.

## Canonical status model

The registry SHALL use a finite status vocabulary:

- `researching`
- `draft`
- `validated`
- `approved-internally`
- `submitted`
- `acknowledged`
- `under-review`
- `adopted`
- `implementation`
- `verification`
- `verified`
- `rejected`
- `superseded`
- `withdrawn`
- `closed`

Status transitions SHALL be validated. For example, `submitted` SHALL require a submission receipt or authorized submission record; `acknowledged` SHALL require external acknowledgment evidence; `adopted` SHALL require evidence from the competent adopting authority; and `verified` SHALL require completion evidence that satisfies the proposal's verification contract.

## Authority matrix

Every proposal SHALL include an authority matrix that distinguishes actual decision rights from influence, recommendation, referral, or implementation support.

The matrix SHALL support these roles:

- `proposal-owner` — JPV authority accountable for proposal integrity;
- `decision-authority` — actor legally or institutionally able to adopt or reject;
- `implementation-authority` — actor able to execute adopted obligations;
- `funding-authority` — actor able to authorize budget where relevant;
- `oversight-authority` — actor responsible for lawful or institutional oversight;
- `review-authority` — actor able to conduct independent challenge or review;
- `appeal-or-escalation-authority` — actor or route for disputed disposition; and
- `evidence-custodian` — system or actor responsible for retained evidence.

The system SHALL permit `unknown` or `not-applicable` where supported. It SHALL NOT invent authority to satisfy schema completeness.

## Evidence and provenance contract

Proposal evidence SHALL reuse or reference the existing claims/evidence engine where appropriate.

Every proposition used to justify a proposal SHALL be classifiable as one of:

- `assertion`;
- `source-record`;
- `verified-fact`;
- `inference`;
- `recommendation`;
- `unresolved-question`;
- `competent-authority-determination`; or
- `later-disposition`.

The proposal subsystem SHALL store opaque evidence references rather than duplicate protected evidence content when an existing evidence record already exists.

An evidentiary classification SHALL NOT be elevated automatically because a proposal was approved internally, submitted externally, or publicly discussed.

## Adoption-route contract

Every proposal that seeks external adoption SHALL define a deterministic adoption route.

The route SHALL contain:

- target institution or authority;
- submission mechanism;
- required artifact set;
- jurisdiction/eligibility basis where relevant;
- expected acknowledgment mechanism if one exists;
- known review sequence;
- escalation or alternate route;
- expiration or resubmission rules if applicable; and
- evidence required to record each stage.

When the external institution does not publish a deterministic process, the route SHALL record that uncertainty rather than fabricate one.

## Implementation contract

Once a proposal is adopted or internally authorized for execution, it SHALL have explicit implementation obligations.

Each obligation SHALL contain:

- obligation ID;
- responsible actor or authority;
- action required;
- prerequisite dependencies;
- target date or explicitly open-ended timing;
- artifact or system state produced;
- evidence required for completion;
- failure condition;
- remediation/escalation route; and
- completion state.

Implementation progress SHALL be recorded through lifecycle events. A narrative statement that work is complete SHALL NOT be sufficient evidence by itself.

## Verification contract

Every executable proposal SHALL define what proves implementation occurred.

Verification MAY include:

- exact repository commit or merge SHA;
- signed/verified commit state;
- workflow/check results;
- external acknowledgment or adoption record;
- deployed system observation;
- authoritative publication;
- signed agreement;
- audit result;
- measured operational state; or
- other proposal-specific evidence.

Where repository implementation is involved, the verification record SHALL support exact-head receipt semantics so that a proposal cannot claim implementation against a stale or different repository state.

Verification SHALL be independent from outcome measurement. A system can be correctly implemented and still fail to achieve the desired outcome.

## Outcome ledger

Every proposal intended to produce measurable effects SHALL define an outcome contract with:

- metric or observable condition;
- baseline where available;
- target or success threshold;
- measurement method;
- authoritative data source;
- observation window;
- adverse-effect indicator;
- review owner;
- confidence or data-quality notes; and
- revision trigger.

The outcome ledger SHALL support `not-yet-measurable`, `insufficient-data`, `mixed`, `met`, `not-met`, and `adverse-effect-detected` dispositions.

## Independent review

Proposal classes affecting public trust, public policy, regulatory claims, institutional accountability, or material external rights SHALL carry an explicit review requirement.

The record SHALL identify whether review is:

- required before submission;
- required before publication;
- required before adoption recommendation;
- required before closure; or
- not required, with a documented reason.

A reviewer SHALL not be represented as independent where the reviewer is the proposal owner or implementing authority.

The system SHALL allow an external reviewer to be unresolved until a suitable reviewer is identified; it SHALL not silently downgrade a required review to optional.

## Version lineage

Every proposal artifact SHALL be connected to a canonical proposal record using explicit lineage relationships:

- `initial-version`;
- `revises`;
- `supersedes`;
- `superseded-by`;
- `derives-from`;
- `external-submission-of`;
- `pilot-for`;
- `implements`;
- `public-release-of`; and
- `archive-copy-of`.

Multiple artifacts may represent one proposal, but exactly one proposal ID owns the lifecycle state.

A superseded artifact SHALL remain historically addressable and SHALL NOT continue to drive current implementation unless explicitly reactivated through a new revision event.

## Public/private boundary

The canonical registry is private-by-default for sensitive records.

A public view MAY expose:

- proposal ID;
- public title;
- public summary;
- non-sensitive lifecycle status;
- public source artifacts;
- public adoption/implementation records; and
- public outcome results.

It SHALL NOT expose protected evidence, privileged material, private identities, confidential correspondence, internal deliberation, security-sensitive implementation details, or non-public external records.

Public publication requires an explicit release classification or publication review event.

## Domain events

The domain SHALL support at least these event meanings:

- `ProposalRegistered`
- `ProposalRevised`
- `EvidenceLinked`
- `AuthorityMapped`
- `ValidationRecorded`
- `InternalApprovalRecorded`
- `SubmissionRecorded`
- `AcknowledgmentRecorded`
- `ReviewStarted`
- `ReviewDispositionRecorded`
- `AdoptionRecorded`
- `ImplementationObligationAdded`
- `ImplementationProgressRecorded`
- `ImplementationCompleted`
- `VerificationRecorded`
- `OutcomeMeasurementRecorded`
- `RevisionTriggered`
- `ProposalRejected`
- `ProposalSuperseded`
- `ProposalWithdrawn`
- `ProposalClosed`
- `ProposalReopened`
- `PublicationReviewed`

The event vocabulary MAY expand without changing prior event meaning.

## Persistence

The executable proposal history SHALL be append-only and transactionally durable.

The initial implementation SHALL follow the existing repository persistence pattern and use SQLite-backed storage behind interfaces unless the current codebase already provides a more appropriate durable event-store abstraction that can be reused directly.

Lifecycle event sequencing and idempotency SHALL be deterministic.

A mutable projection MAY be cached for efficient reads but SHALL be reconstructable from the event stream.

## API and service boundary

The initial implementation SHALL prioritize internal governed services and machine-readable registry access rather than creating a broad unauthenticated public mutation API.

A focused module under `src/JPVOS/Services/ProposalExecution` SHALL own proposal semantics.

The minimal internal contracts SHALL include:

- proposal registration;
- lifecycle event append;
- authority mapping;
- evidence linking;
- adoption-route updates;
- implementation-obligation management;
- verification recording;
- outcome recording;
- lineage management; and
- deterministic projection reads.

A bounded read-only machine interface MAY expose non-sensitive proposal status where existing authorization and publication rules permit it.

## Registry bootstrap

Implementation SHALL include an initial registry manifest for known JPV proposal families without asserting unsupported external outcomes.

At minimum, bootstrap entries SHALL be possible for:

- JPV × Purdue Food Resilience / Traceability initiative;
- JPV-OS Governance Grants / Industrial Policy proposal;
- presidential-capacity and institutional-accountability review packet;
- executive/public-integrity oversight referrals;
- public recovery / reinvestment reform concepts; and
- future proposal families added under the same contract.

Bootstrap records SHALL use only evidenced states. For example, an exploratory deck cannot be recorded as an adopted partnership; a sent packet cannot be recorded as acknowledged unless acknowledgment evidence exists.

## Governance inheritance

This subsystem SHALL inherit canonical JPV governance rather than create a competing policy authority.

The implementation SHALL add an inheritance manifest binding this repository to the canonical proposal/reform execution standard and related claim-truth, review-closure, evidence, and institutional-integrity standards.

Where a canonical governance rule and local implementation differ, the local implementation SHALL fail validation until reconciled or explicitly version-pinned under an approved migration record.

## Validation and fail-closed behavior

A validator SHALL reject or flag records that:

- skip required lifecycle stages without evidence;
- assert adoption without competent-authority evidence;
- assert implementation without completion evidence;
- assert verification without satisfying the verification contract;
- assert outcome success without a recorded measurement;
- contain contradictory active lineage;
- assign impossible or circular independent-review roles;
- cite missing evidence references;
- lack an owning JPV lane;
- use unknown status values; or
- publish fields above their release classification.

Validation failures SHALL be visible and auditable. The system SHALL not silently coerce invalid state into a valid-looking projection.

## Testing strategy

Implementation SHALL use test-driven development and add coverage for:

1. lifecycle transition validity;
2. submission versus acknowledgment separation;
3. adoption authority enforcement;
4. implementation-obligation completion evidence;
5. verification versus outcome separation;
6. authority-matrix role constraints;
7. evidence classification preservation;
8. independent-review conflict detection;
9. version-lineage supersession behavior;
10. deterministic event replay/projection;
11. idempotent lifecycle writes;
12. fail-closed publication filtering;
13. bootstrap registry truthfulness; and
14. exact-head repository verification where applicable.

Existing claims/evidence and governance tests SHALL remain passing.

## Delivery artifacts

The implementation phase SHALL produce at minimum:

- domain types and lifecycle state machine;
- append-only proposal event store;
- deterministic proposal projection;
- authority, adoption, implementation, verification, outcome, and lineage contracts;
- proposal registry service;
- validation service;
- inheritance manifest;
- bootstrap registry for known proposal families;
- machine-readable schemas or DTO contracts;
- tests;
- operator documentation;
- CI-validating checks for registry/schema integrity; and
- PR/merge verification receipt.

## Explicit non-goals

This phase SHALL NOT:

- create legal findings JPV lacks authority to make;
- fabricate external acknowledgments, adoption, partnership, funding, or implementation;
- replace the existing claims/evidence engine;
- expose protected proposal/evidence data publicly by default;
- create a new infrastructure vendor dependency without necessity;
- build a second public gateway; or
- automatically submit external proposals without an independently authorized submission path.

## Definition of done

The subsystem is complete only when:

1. the lifecycle standard is represented as executable domain contracts;
2. proposal records can be registered and replayed deterministically;
3. authority, evidence, adoption, implementation, verification, outcome, and lineage are machine-readable;
4. invalid or unsupported state fails visibly;
5. known proposal families can be bootstrapped without overstating their current status;
6. tests and repository checks pass;
7. the inheritance binding is present;
8. the implementation lands through the repository's PR/review process; and
9. the merged exact-head state is verified against the expected implementation receipt.
