# JPV Nexus

JPV Nexus is the application-facing entry layer for the JPV ecosystem.

It provides the structured interface for identity-aware entry, entitlement routing, dashboard access, role-aware experience design, and governed application routing across the JayPVentures LLC infrastructure and creator-facing systems.

## Operational Purpose

- Application interface
- Identity-aware entry
- Dashboard shell
- Entitlement routing
- Role-aware routing
- Governance-aware app layer

## Repository Status

This repository replaces the temporary Visual Studio project name WebApplication1. The repository slug remains `jpv-os-access-gateway` for history and deployment continuity; the canonical product identity is **JPV Nexus**.

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

### Azure App Service

Azure App Service deployment is now provisioned using automated scripts. To get started:

**Windows (PowerShell):**
```powershell
.\scripts\provision-azure-appservice.ps1 -Path A -SubscriptionId "YOUR-SUBSCRIPTION-ID"
```

**Linux/macOS (Bash):**
```bash
./scripts/provision-azure-appservice.sh -Path A -SubscriptionId "YOUR-SUBSCRIPTION-ID"
```

See [docs/AZURE-PROVISIONING-GUIDE.md](docs/AZURE-PROVISIONING-GUIDE.md) for complete provisioning instructions and alternative deployment paths.
