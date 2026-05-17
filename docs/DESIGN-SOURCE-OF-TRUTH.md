# JPV-OS / init — Visual Source of Truth

The Canva Implementation Master is the authoritative visual reference for the Blazor implementation.

## Reference Files

- docs/design-reference/implementation-master-01.png
- docs/design-reference/implementation-master-02.png
- docs/design-reference/implementation-master-03.png
- docs/design-reference/implementation-master-04.png
- docs/design-reference/implementation-master-05.png

## Implementation Rules

- No duplicate navigation.
- Skip link hidden until keyboard focus.
- Header, footer, homepage, pricing, access, login, and admin must follow the approved visual system.
- No placeholder imagery.
- No founder imagery outside Founder/About.
- No generic SaaS layout.
- No empty PRs.
- Changed files must be greater than zero.

## Active Files

- src/JPVOS/Components/SiteHeader.razor
- src/JPVOS/Components/SiteFooter.razor
- src/JPVOS/Components/Pages/Home.razor
- src/JPVOS/Components/Pages/Pricing.razor
- src/JPVOS/Components/Pages/AccessRouting.razor
- src/JPVOS/Components/Pages/Login.razor
- src/JPVOS/Components/Pages/Admin.razor
- src/JPVOS/Components/Pages/Partners.razor
- src/JPVOS/wwwroot/css/jpv-os.tokens.css

## Validation

dotnet build .\JPVOS.csproj
powershell -ExecutionPolicy Bypass -File .\scripts\verify-ui.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\final-launch-curation.ps1

## Approval Standard

Localhost must visually match the Canva Implementation Master before merge.
