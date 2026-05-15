# JPV-OS Access Gateway Deployment Guide

## Overview

JPV-OS Access Gateway is a .NET 8.0 / Blazor application that provides application-facing routing, dashboard entry, and role-aware experience design for the JPV ecosystem. This document describes how to build, run, and deploy the application.

## Build Command

```bash
dotnet build JPVOS.sln -c Release
```

**Requirements:**
- .NET 8.0 SDK or higher
- Operating system: Windows, macOS, or Linux

**Output:** Compiled assemblies in `src/JPVOS/bin/Release/net8.0/`

## Run Command (Local Development)

### Development Environment

```bash
cd src/JPVOS
dotnet run
```

The application will start on `https://localhost:5001` and `http://localhost:5000` in development mode.

**Development Configuration:**
- Uses in-memory entitlements service by default
- HTTPS redirection disabled
- HSTS disabled
- Verbose logging enabled (Information level)

### Production Environment

```bash
cd src/JPVOS
dotnet run --configuration Release --no-build --environment Production
```

**Production Configuration:**
- Uses SQLite entitlements database (`entitlements.db`)
- HTTPS redirection enabled
- HSTS enabled (30-day default)
- Warning-level logging only
- Health check endpoint available at `/health`

## Publish Command

### Docker Build and Push

```bash
# Build Docker image (from repository root)
docker build -f src/JPVOS/Dockerfile -t jpv-os:latest .

# Tag for GitHub Container Registry
docker tag jpv-os:latest ghcr.io/jaypventures-llc/jpv-os:latest

# Push to GitHub Container Registry (requires authentication)
docker push ghcr.io/jaypventures-llc/jpv-os:latest

# For production, use immutable commit SHA tag
docker tag jpv-os:latest ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
docker push ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
```

### dotnet publish (Self-Contained)

```bash
dotnet publish src/JPVOS/JPVOS.csproj -c Release -o ./publish
```

This produces a self-contained application ready for deployment. The output directory contains:
- `JPVOS.dll` - Main application assembly
- `appsettings.Production.json` - Production configuration template
- All dependencies and runtime files

## Environment Assumptions

### Required Environment Variables

**Stripe Configuration:**
```
STRIPE_SECRET_KEY=sk_live_...                          # Stripe secret key
STRIPE_WEBHOOK_SECRET=whsec_...                        # Webhook endpoint secret
STRIPE_PRICE_ENTERPRISE_ANNUAL=price_...               # Enterprise annual subscription price ID
STRIPE_PRICE_CUSTOM_IMPLEMENTATION=price_...           # Custom implementation one-time price ID
```

**Discord Configuration:**
```
DISCORD_CLIENT_ID=...                                  # Discord OAuth app client ID
DISCORD_CLIENT_SECRET=...                              # Discord OAuth app client secret
DISCORD_BOT_TOKEN=...                                  # Discord bot token for role assignments
DISCORD_GUILD_ID=...                                   # Discord server guild ID
DISCORD_ROLE_CUSTOM=...                                # Discord role ID for custom access
DISCORD_REDIRECT_URI=https://yourdomain.com/api/discord/oauth/callback
```

**ASP.NET Core:**
```
ASPNETCORE_ENVIRONMENT=Production                      # Set to Production for production deployments
ASPNETCORE_HTTP_PORTS=8080                             # HTTP port (used in containers)
```

### Database

**Development:** In-memory entitlements service (no persistence required)

**Production:** SQLite database (`entitlements.db`)
- Automatically created on first run
- Located in the application working directory
- Permissions: Ensure write access to the directory containing the database file
- Backup: Implement regular backups of this file

### Security Considerations

- All secret keys and tokens should be stored securely (environment variables, Key Vault, or secret management service)
- Never commit secrets to version control
- Use HTTPS in production (enabled by default in non-development environments)
- HSTS is enabled by default (30-day max-age)

## Cloudflare / Static Hosting Notes

### Container Deployment (Primary Path)

The application is containerized and designed for cloud platforms:

**Supported Platforms:**
- Fly.io
- Render
- Railway
- Azure Container Instances
- AWS ECS
- Google Cloud Run
- Any Docker-compatible container registry

**Container Configuration:**
- Base image: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Listening port: `8080`
- Non-root user: `app` (UID 1654)
- Health check: `GET /health`

### Fly.io Deployment

See `fly.toml` for production configuration:

```bash
fly auth login
fly launch --copy-config  # Or use existing fly.toml
fly deploy
```

**Configuration Highlights:**
- Primary region: IAD (adjust as needed)
- Min machines: 0 (development), change to 1 for production to avoid cold starts
- Health checks: Configured at `/health` endpoint
- HTTPS: Forced with `force_https = true`

### Render Deployment

See `render.yaml` for production configuration:

```bash
# Deploy via Render dashboard using render.yaml
# Or deploy via CLI:
render deploy
```

**Configuration Highlights:**
- Environment: Docker
- Health check path: `/health`
- Plan: Free tier (development/staging), change to starter or higher for production
- Region: Oregon

### GitHub Pages / Static Hosting

The application is **not designed for static hosting**. It is a dynamic ASP.NET Core Blazor Server application with server-side state management and database access. It requires:
- ASP.NET Core runtime
- HTTPS support
- WebSocket support (for Blazor Server)

If you need static deployment, you would need to:
1. Convert to Blazor WebAssembly (client-side)
2. Implement a separate backend API
3. Deploy the compiled Wasm app to static hosting

This is outside the scope of the current architecture.

## Health Check Endpoint

The application includes a health check endpoint for monitoring and container orchestration:

**Endpoint:** `GET /health`

**Response (Healthy):**
```json
{
  "status": "healthy",
  "timestamp": "2026-05-15T22:29:02.579Z"
}
```

**Usage:**
- Container platforms use this for liveness/readiness probes
- Monitoring systems can periodically check this endpoint
- No authentication required

## Rollback Expectations

### Container Deployments

**Rollback Strategy:** Immutable Container Tags

1. **Always deploy using commit SHAs:**
   ```bash
   docker pull ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
   docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
   ```

2. **Maintain a `latest` tag for reference only:**
   ```bash
   docker pull ghcr.io/jaypventures-llc/jpv-os:latest
   ```

3. **In case of issues, immediately revert to previous commit SHA:**
   - Fly.io: Update `fly.toml` image and redeploy
   - Render: Redeploy using previous commit SHA via dashboard
   - Manual: Update your container orchestration to reference previous SHA

4. **Database:** SQLite deployments are isolated per instance
   - If using managed database, maintain backups before each deployment
   - Test database migrations on staging environment first

### Deployment Safeguards

1. **Pre-deployment Validation:**
   ```bash
   dotnet build JPVOS.sln -c Release   # Verify build succeeds
   dotnet test JPVOS.sln -c Release --no-build  # Run tests
   ```

2. **Canary Deployments:** Deploy to staging first
   - Test all Stripe payment flows
   - Verify Discord OAuth integration
   - Confirm health check responds correctly

3. **Database Schema Changes:**
   - Test migrations on a copy of production database
   - Keep backward-compatible schemas
   - Have rollback plan (previous app version can read old schema)

### Data Persistence

**Entitlements Database:**
- The SQLite database (`entitlements.db`) contains user entitlements and access records
- **Backup before deployments** - use container volume mounts or managed storage
- If database corruption occurs, rollback to previous database backup and redeploy app

**Stripe Data:**
- Stripe maintains webhook history and subscription records independently
- Application status can be recovered from Stripe API if database is lost
- Use `customer.subscription.*` webhook events to rebuild state

**Discord Data:**
- Role assignments stored in Discord (not replicated in app database)
- Can be rebuilt by replaying webhook events or manual role reassignment

## Deployment Checklist

Before deploying to production:

- [ ] All environment secrets configured securely (not in code)
- [ ] Stripe API keys and webhook secret validated
- [ ] Discord bot token and guild configuration verified
- [ ] Database backup strategy in place
- [ ] Health check endpoint accessible (`/health`)
- [ ] HTTPS certificate valid and auto-renewal configured
- [ ] Container image built and tested locally
- [ ] Deployment platform (Fly.io, Render, etc.) configured with rollback plan
- [ ] Monitoring/logging configured for the platform
- [ ] Team access to deployment platform and secrets management
- [ ] Incident response plan documented

## Support and Troubleshooting

### Common Issues

**Health check fails:**
```bash
curl -I http://localhost:8080/health
```
If the endpoint returns 5xx errors, check application logs.

**Database file not found:**
Ensure the application has write permissions in its working directory. Create `entitlements.db` manually if needed:
```bash
touch entitlements.db
chmod 644 entitlements.db
```

**Stripe webhook not processing:**
- Verify webhook endpoint is publicly accessible
- Check STRIPE_WEBHOOK_SECRET environment variable
- Review application logs for deserialization errors

**Discord OAuth failing:**
- Verify DISCORD_CLIENT_ID and DISCORD_CLIENT_SECRET
- Confirm DISCORD_REDIRECT_URI matches registered redirect URI in Discord app settings

### Logs

**Development:**
```bash
dotnet run
# Logs output to console
```

**Production (Docker):**
```bash
docker logs <container-id>
```

**Production (Fly.io):**
```bash
fly logs
```

**Production (Render):**
Logs available in Render dashboard under deployment logs.

## Architecture Notes

- **Framework:** ASP.NET Core 8.0 with Blazor Server
- **Database:** SQLite (development/small deployment), can be replaced with PostgreSQL/SQL Server for larger deployments
- **Authentication:** Stripe + Discord OAuth
- **State Management:** Server-side Blazor component state + SQLite persistence
- **API:** RESTful endpoints for webhooks and checkout

For architectural questions, refer to the README.md and codebase comments.
