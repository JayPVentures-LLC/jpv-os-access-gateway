# JPV Nexus

JPV Nexus is the application-facing entry layer for the JPV ecosystem.

It provides identity-aware entry, entitlement routing, dashboard access, role-aware experience design, and governed application routing across JayPVentures LLC infrastructure.

## Operational purpose

- application interface
- identity-aware entry
- dashboard shell
- entitlement routing
- role-aware routing
- governance-aware application layer

## Canonical runtime

JPV Nexus production execution runs on **JPV Compute** under **JPV Deploy** authority.

External compute providers, GitHub Actions, Azure App Service, Render, Railway, Fly.io, DigitalOcean App Platform, Google Cloud Run, AWS App Runner, Vercel, and SentinelX are not canonical production runtime paths.

Cloudflare may be used only where separately admitted for bounded DNS/SSL/CDN/edge publication. Cloudflare never owns Nexus runtime, identity, entitlement state, deployment authority, or completion authority.

## Build

```bash
dotnet build JPVOS.sln -c Release
dotnet test JPVOS.sln -c Release --no-build
```

## Production contract

Production completion requires all of the following:

1. the exact approved source revision is packaged for JPV Compute;
2. JPV Deploy activates that exact revision on the admitted JPV Compute target;
3. `GET /health` returns healthy state;
4. Nexus authentication succeeds;
5. entitlement and role enforcement succeed;
6. Stripe payment/claim/entitlement flow succeeds where applicable;
7. core Nexus actions execute through JPV authority;
8. authoritative readback proves the exact deployed revision;
9. restart/recovery returns Nexus to service without workstation or hosted-provider substitution.

Repository state, container publication, provider acknowledgement, or source build success alone is not production completion.

See `docs/DEPLOYMENT.md` for the runtime contract.
