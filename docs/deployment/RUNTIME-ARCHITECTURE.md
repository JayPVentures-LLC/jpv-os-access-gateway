# JPV-OS ACCESS GATEWAY
# RUNTIME DEPLOYMENT STANDARD

## Architecture

JPV-OS / JPV-native:
- application runtime authority
- Stripe Checkout and webhooks
- Discord OAuth and role assignment
- admin and entitlement routing
- deployment selection and terminal verification

Cloudflare (currently admitted bounded infrastructure):
- DNS
- SSL
- CDN
- edge protection

Azure App Service is not an admitted production runtime.

## Production Secrets

STRIPE_SECRET_KEY
STRIPE_WEBHOOK_SECRET
DISCORD_CLIENT_ID
DISCORD_CLIENT_SECRET
DISCORD_BOT_TOKEN
DISCORD_GUILD_ID
DISCORD_ROLE_MEMBER
DISCORD_ROLE_VIP_VENTURE

## Rules

- Never expose secrets client-side.
- Never commit secrets into Git.
- Stripe + Discord stay server-side only.
- Provider availability never confers runtime or completion authority.
- JPV-native authoritative readback is required for completion.
