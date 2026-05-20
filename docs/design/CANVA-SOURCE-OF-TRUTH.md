# Canva Source of Truth

The Canva design is the authoritative visual schematic for the public JPV-OS Access Gateway experience.

## Rule

No page, layout, route, section, header, footer, pricing block, ecosystem diagram, or visual component may be redesigned independently from the Canva design source.

## Required implementation behavior

- The Canva design must be treated as the visual source of truth.
- The app implementation must match the Canva layout, spacing, section order, iconography, and visual hierarchy.
- Placeholder dashboard cards, random metrics, generic trust blocks, and repeated route templates are not allowed.
- Design changes must reference the Canva source before implementation.
- Backend wiring may continue independently, but public UI must not drift from the approved Canva scheme.

## Current enforcement status

Discord role sync is deferred for launch.
Stripe, entitlement, and backend validation may continue.
Frontend visual parity is not complete until Canva design sections are implemented as the only public design system.
