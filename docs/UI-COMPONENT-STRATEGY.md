# UI Component Strategy

## Scope
This strategy applies to the active Blazor app at `src/JPVOS`.

## UI System Direction
- Keep the existing JPV-OS cinematic visual system and brand assets in `src/JPVOS/wwwroot/assets`.
- Maintain the dark premium interface with electric blue, royal purple, magenta accents, and silver-forward typography.
- Avoid Bootstrap-driven visual patterns.

## Component Model
- Use native Blazor components as the default implementation.
- Build pages from reusable primitives already in this project (`PageHero`, `CTASection`, `RouteTile`, `AccessCard`, `PartnerCategory`, `PricingCard`).
- Keep layout and routing shell in the existing component architecture (`AppShell`, `SiteHeader`, `SiteFooter`, `MainLayout`).

## Telerik Alignment
- Telerik UI for Blazor is optional and should be used only when package access is configured.
- Current repository configuration does not include Telerik package feeds or credentials.
- Until Telerik access is enabled, keep adapter-ready native Blazor components so the build remains stable and Windows/.NET friendly.

## Backup Folder Safety
- Backup trees matching `_design-backup-*` are excluded at project level to prevent duplicate C# definitions from entering compilation.
