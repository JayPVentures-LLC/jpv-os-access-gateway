# Discord Environment Automation

This validation script checks required Stripe and Discord runtime settings without printing secret values.

Run:

```powershell
.\scripts\validate-discord-env.ps1
```

The script writes a redacted report to:

```text
reports/discord-env-validation.md
```

It reports only `SET` or `MISSING`.

Required settings:

- STRIPE_SECRET_KEY
- STRIPE_WEBHOOK_SECRET
- DISCORD_GUILD_ID
- DISCORD_BOT_TOKEN
- DISCORD_CLIENT_ID
- DISCORD_CLIENT_SECRET
- DISCORD_REDIRECT_URI
- DISCORD_ROLE_MEMBER_ACCESS
- DISCORD_ROLE_VIP_VENTURE
- DISCORD_ROLE_CREATOR_LANE
- DISCORD_ROLE_OPERATOR
- DISCORD_ROLE_ENTERPRISE

Do not commit secrets, bot tokens, webhook secrets, client secrets, or Discord role IDs.
