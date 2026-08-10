# JayPVentures LLC Repository Governance

Generated: 20260514-181432  
Organization: JayPVentures-LLC

## Current Approved Structure

### Active Repositories

| Repository | Purpose | Visibility |
|---|---|---|
| automation-core | Shared automation and reusable operational services. | INTERNAL |
| jayventures-labs | Research, prototypes, validation, and controlled experiments. | PUBLIC |
| jpv-os-access-gateway | JPV Nexus application repository for identity-aware entry, entitlement routing, role mapping, and governed application access. | PUBLIC |
| jpv-public-records | Private evidence, records, and governance material before approved publication. | PRIVATE |

### Archived Repositories

| Repository | Status |
|---|---|
| jpv-discussions | Archived. Do not reactivate unless there is a defined public-discussion strategy. |
| SOS | Archived hold. Review before deletion because of repository size and unknown retained material. |

## Repository Admission Rule

No new JayPVentures LLC repository may be created unless it meets one of these conditions:

1. It is a production application.
2. It is a reusable infrastructure/service component.
3. It is a legally or operationally isolated records/evidence repository.
4. It is a research/lab environment that cannot safely live inside jayventures-labs.

Default decision: use an existing repository and create a bounded folder/module.

## Required Pre-Create Checklist

Before creating any repo:

- Confirm the work cannot live inside an existing repo.
- Confirm the owner account is JayPVentures-LLC, not a personal profile.
- Define the repo purpose in one sentence.
- Define visibility: PUBLIC, PRIVATE, or INTERNAL.
- Define whether it is production, lab, records, or infrastructure.
- Confirm no duplicate repo already exists.
- Confirm governance files will be added before first push.

## Approved Active Repo Count

Target active repo count: 4

Allowed active repos:

- automation-core
- jayventures-labs
- jpv-os-access-gateway
- jpv-public-records

Any fifth active repo requires written justification.

## Personal Profile Rule

The personal jaypventures profile is not used for JayPVentures LLC operational repo management.

All organization work must be scoped to:

gh repo list JayPVentures-LLC

Do not use:

gh repo list jaypventures

## Current Repo Snapshot

Active count: 4  
Archived count: 2  
Total count: 6

### Active

- automation-core [INTERNAL] https://github.com/JayPVentures-LLC/automation-core
- jayventures-labs [PUBLIC] https://github.com/JayPVentures-LLC/jayventures-labs
- jpv-os-access-gateway [PUBLIC] https://github.com/JayPVentures-LLC/jpv-os-access-gateway
- jpv-public-records [PRIVATE] https://github.com/JayPVentures-LLC/jpv-public-records

### Archived

- jpv-discussions [PUBLIC] https://github.com/JayPVentures-LLC/jpv-discussions
- SOS [PRIVATE] https://github.com/JayPVentures-LLC/SOS
