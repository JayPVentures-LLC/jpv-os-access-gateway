# JPV-OS Access Gateway

JPV-OS Access Gateway is the application-facing gateway for the JPV ecosystem.

It provides the structured interface for access routing, dashboard entry, role-aware experience design, and future entitlement alignment across the JayPVentures LLC infrastructure and creator-facing systems.

## Operational Purpose

- Application interface
- Access gateway
- Dashboard shell
- Role-aware routing
- Governance-aware app layer
- Future entitlement integration

## Repository Status

This repository replaces the temporary Visual Studio project name WebApplication1.

## Governance Alignment

This repo must preserve:

- security policy enforcement
- role-based access architecture
- audit-friendly deployment history
- human-review protections
- interoperability freedom
- vendor-boundary separation
- JPV-OS people-protection standards

## Primary Stack

- .NET / Blazor
- GitHub Actions
- JayPVentures LLC governance standards

## Deployment

### Container Deployment (Current)

The application is containerized and published to GitHub Container Registry when changes to `src/JPVOS/**` are pushed to `main`:

```bash
# For local testing
docker pull ghcr.io/jaypventures-llc/jpv-os:latest
docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpv-os:latest

# For production (use immutable commit SHA tag)
docker pull ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
```

**Supported Platforms:**
- Render (render.yaml included)
- Railway
- Fly.io (fly.toml included)
- DigitalOcean App Platform
- Google Cloud Run
- AWS App Runner

See [docs/CONTAINER-DEPLOYMENT.md](docs/CONTAINER-DEPLOYMENT.md) for detailed deployment instructions.

### Azure App Service (Planned)

Azure App Service deployment is planned once Entra ID Conditional Access requirements are resolved. See [docs/AZURE-APP-SERVICE-DEPLOYMENT.md](docs/AZURE-APP-SERVICE-DEPLOYMENT.md).

