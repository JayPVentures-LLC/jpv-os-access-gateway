# JPV Nexus

JPV Nexus is the application-facing entry layer for the JPV ecosystem.

It provides identity-aware entry, entitlement routing, dashboard access, role-aware experience design, and governed application routing across JayPVentures LLC infrastructure and creator-facing systems.

## Canonical runtime

Production compute: **JPV Compute**  
Deployment authority: **JPV_DEPLOY**  
Canonical execution boundary: **JPV_COMPUTE_FABRIC**

The application is packaged by `src/JPVOS/Dockerfile` for JPV Compute. Fly.io, Render, Railway, Azure, Vercel, hosted workflow runners, GitHub Actions, and other external compute providers are not production runtime or deployment authority.

Cloudflare may be used only where separately admitted as bounded DNS/SSL/CDN/edge infrastructure. It does not provide Nexus compute, entitlement authority, canonical state, or completion authority.

## Build

```bash
dotnet build JPVOS.sln -c Release
dotnet test JPVOS.sln -c Release --no-build
```

## Production completion

Nexus is complete only when JPV Compute runs the exact approved revision and authoritative readback verifies:

- `GET /health` is healthy;
- identity and authentication work;
- checkout → Stripe webhook → entitlement → claim → access completes;
- revocation is enforced;
- persistent proposal/state storage survives restart;
- the exact deployed revision/artifact is returned by JPV readback.

Repository commits or container creation alone do not establish production completion.
