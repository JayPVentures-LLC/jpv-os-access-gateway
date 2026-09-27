# JPV-OS Access Gateway Deployment Guide

JPV-OS Access Gateway is a .NET 8 / Blazor application.

## Build
```bash
dotnet build JPVOS.sln -c Release
dotnet test JPVOS.sln -c Release --no-build
```

## Runtime authority
Production execution is JPV-native. The access gateway does not select Azure, Vercel, hosted workflow runners, Render, Railway, Fly.io, or another external compute provider as runtime authority.

Cloudflare is currently admitted only as bounded DNS/SSL/CDN/edge infrastructure. Provider availability never establishes JPV runtime or completion authority.

## Production configuration
Required server-side settings:
- STRIPE_MODE
- STRIPE_SECRET_KEY
- STRIPE_WEBHOOK_SECRET
- DISCORD_CLIENT_ID
- DISCORD_CLIENT_SECRET
- DISCORD_BOT_TOKEN
- DISCORD_GUILD_ID
- DISCORD_ROLE_CUSTOM
- DISCORD_REDIRECT_URI

Never commit secrets. JPV-native deployment and authoritative readback determine completion.

## Health
`GET /health` is the runtime health endpoint. Production completion requires JPV authoritative revision/readback, not provider status.
