# Release Readiness Validation

Validates JPV-OS Access Gateway before deployment.

## Required checks

- Working tree is clean.
- No `.bak` files are present.
- `dotnet build src/JPVOS/JPVOS.csproj` succeeds.
- Stripe pricing map exists and is not empty.
- Stripe webhook secret is configured.
- Discord bot, guild, OAuth, and role settings are configured.
- Customer portal uses entitlement verification.
- Webhooks use idempotency.
- Discord role removal is awaited.
- Discord role sync has audit, retry, and Retry-After support.

## Stripe required settings

- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`

## Discord required settings

- `DISCORD_GUILD_ID`
- `DISCORD_BOT_TOKEN`
- `DISCORD_CLIENT_ID`
- `DISCORD_CLIENT_SECRET`
- `DISCORD_REDIRECT_URI`
- `DISCORD_ROLE_MEMBER_ACCESS`
- `DISCORD_ROLE_VIP_VENTURE`
- `DISCORD_ROLE_CREATOR_LANE`
- `DISCORD_ROLE_OPERATOR`
- `DISCORD_ROLE_ENTERPRISE`

## Deployment blockers

Do not deploy if build fails, backup files are present, Stripe pricing JSON is missing, webhook secret is missing, Discord settings are incomplete, or entitlement persistence is not production-ready.
