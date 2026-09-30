# JPV RUNTIME DEPLOYMENT STANDARD

## Canonical authority chain

JPV Nexus:
- application entry
- identity, entitlement, and role routing
- governed handoff into JPV Runtime
- access-state audit handoff

JPV Runtime / JPV-native:
- persistent application and execution authority
- governed state transitions
- local execution through admitted JPV capabilities, including JPV PowerShell
- deployment selection
- authoritative terminal readback

Replaceable routes, adapters, and actuators:
- provide transport or bounded external actuation only
- do not define JPV identity, runtime state, authority, or completion
- may disappear or be replaced without redefining JPV Runtime state
- route failure is not JPV failure

Currently admitted bounded infrastructure may include DNS, TLS, CDN, payment, identity, repository, or external API services when a governed operation requires them. Admission never transfers JPV authority to the provider.

## Execution physics

```text
request
  -> JPV Nexus / canonical ingress
  -> governance and authority gate
  -> JPV Runtime
  -> admitted local capability or replaceable actuator
  -> authoritative readback
```

A route is derived execution state, not canonical JPV state.

External reachability is not proof that JPV is online.
External unreachability is not proof that JPV is offline.
Without JPV-authoritative readback, state is UNVERIFIED.

## Production Secrets

Production secrets remain server-side and under JPV authority. Provider-specific credentials may be admitted only for the bounded capability they authorize.

Examples currently used by application surfaces include:

```text
STRIPE_SECRET_KEY
STRIPE_WEBHOOK_SECRET
DISCORD_CLIENT_ID
DISCORD_CLIENT_SECRET
DISCORD_BOT_TOKEN
DISCORD_GUILD_ID
DISCORD_ROLE_MEMBER
DISCORD_ROLE_VIP_VENTURE
```

## Rules

- Never expose secrets client-side.
- Never commit secrets into Git.
- JPV Nexus owns access and entitlement routing; it does not become general runtime authority.
- JPV Runtime owns execution state and completion.
- Providers, routes, adapters, and actuators remain subordinate and replaceable.
- Provider availability never confers runtime or completion authority.
- Route failure never mutates canonical JPV state.
- JPV-authoritative readback is required for completion.
