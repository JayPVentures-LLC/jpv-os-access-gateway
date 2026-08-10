# JPV Nexus Identity Design

## Decision
Rename the application-facing product identity from **JPV-OS Access Gateway** to **JPV Nexus**.

## Purpose
JPV Nexus is the authenticated application layer that sits between identity and JPV-OS services. Its operating sequence is:

`identity -> authentication -> entitlement resolution -> role/context routing -> authorized application experience`

## Scope
- Preserve existing routing, entitlement, role, audit, and deployment behavior.
- Replace user-facing and runtime product identity from `JPV-OS Access Gateway` / `Access Gateway` to `JPV Nexus` where those strings name the application itself.
- Preserve generic technical use of the word `gateway` only where it refers to a routing pattern rather than the product name.
- Rename the canonical authority contract from `access-gateway-authority.json` to `nexus-authority.json` and update validation to point to the new contract.
- Preserve repository history and deployment wiring; the GitHub repository slug remains unchanged until a repository-rename capability is available.

## Authority
JPV Nexus owns application entry, entitlement routing, role mapping, access-state transitions, and identity/access audit handoff. It does not own pricing authority, public marketing authority, legal conclusions, or case evidence.

## Success Criteria
1. User-facing homepage copy identifies the application as JPV Nexus.
2. Health endpoint reports `app = "JPV Nexus"`.
3. Canonical authority contract uses `system = "jpv-nexus"` and product name `JPV Nexus`.
4. Validation script checks the Nexus contract and emits Nexus identity in pass/fail output.
5. A deterministic PowerShell regression script fails when active product surfaces reintroduce `JPV-OS Access Gateway` or `Access Gateway` as the application name.
6. Existing routing and entitlement semantics remain unchanged.
