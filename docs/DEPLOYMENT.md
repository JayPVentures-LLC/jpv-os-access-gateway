# JPV Nexus Deployment Guide

JPV Nexus is the application-facing entry layer of the JPV ecosystem. It is a .NET 8 / Blazor application.

## Build

```bash
dotnet build JPVOS.sln -c Release
dotnet test JPVOS.sln -c Release --no-build
```

## Authority boundary

JPV Nexus owns application entry, identity-aware access routing, entitlement routing, and governed handoff into JPV Runtime.

JPV Runtime is the persistent execution and completion authority. Nexus does not become general runtime authority merely because it initiates or routes an operation.

Production execution is JPV-native. Azure, Vercel, hosted workflow runners, Render, Railway, Fly.io, GitHub Actions, and other external compute providers are not JPV runtime authority.

External providers may be admitted only as bounded, replaceable actuators or infrastructure. Provider or route availability never establishes JPV state or completion.

## Production configuration

Required server-side settings currently used by Nexus surfaces:

- STRIPE_MODE
- STRIPE_SECRET_KEY
- STRIPE_WEBHOOK_SECRET
- DISCORD_CLIENT_ID
- DISCORD_CLIENT_SECRET
- DISCORD_BOT_TOKEN
- DISCORD_GUILD_ID
- DISCORD_ROLE_CUSTOM
- DISCORD_REDIRECT_URI

Never commit secrets.

## Completion and health

`GET /health` is an application health observation. It is not by itself authoritative proof of JPV Runtime state.

Production completion requires JPV-authoritative revision/readback. Route failure does not establish JPV failure; when authoritative JPV readback is absent, runtime state is `UNVERIFIED`.
