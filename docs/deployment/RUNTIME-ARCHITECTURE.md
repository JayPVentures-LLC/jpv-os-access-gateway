# JPV-OS ACCESS GATEWAY
# RUNTIME DEPLOYMENT STANDARD

## Architecture

Cloudflare:
- DNS
- SSL
- CDN
- Edge protection

Azure App Service:
- Blazor/.NET runtime
- Stripe Checkout
- Stripe webhooks
- Discord OAuth
- Discord role assignment
- Admin routes
- Entitlement routing

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
- Cloudflare Pages is not the runtime host.
