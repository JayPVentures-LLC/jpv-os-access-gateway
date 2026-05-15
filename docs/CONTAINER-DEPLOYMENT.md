# Container Deployment Guide

This guide provides instructions for deploying JPV-OS using container platforms as an alternative to Azure App Service.

## Container Image

The container image is automatically built and pushed to GitHub Container Registry (ghcr.io) when changes are pushed to the `main` branch in `src/JPVOS/**` or the workflow file. Pull requests also build the image (without pushing) to validate changes.

### Image Location

```
ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
```

### Available Tags

- `<sha>` - Git commit SHA for specific versions (recommended for production)
- `latest` - Most recent build from main branch (for local testing only)
- `YYYYMMDD-HHmmss` - Timestamp-based tags

> **Production Recommendation**: Always use the immutable commit SHA tag for production deployments to ensure auditability and reliable rollbacks.

## Quick Start

### Pull and Run Locally

```bash
# For local testing, latest is acceptable
docker pull ghcr.io/jaypventures-llc/jpv-os:latest
docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpv-os:latest

# For production, use a specific commit SHA
docker pull ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
```

Visit http://localhost:8080 to access the application.

## Deployment Platforms

### Render

1. Create a new **Web Service** on [Render](https://render.com)
2. Select **Deploy an existing image from a registry**
3. Enter the image URL: `ghcr.io/jaypventures-llc/jpv-os:<commit-sha>`
4. Configure environment variables if needed
5. Deploy

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `PORT`: Render will set this automatically

> **Note**: The included `render.yaml` uses the free tier for evaluation. For production, change `plan: free` to `plan: starter` or higher.

### Railway

1. Create a new project on [Railway](https://railway.app)
2. Add a new service and select **Docker Image**
3. Enter: `ghcr.io/jaypventures-llc/jpv-os:<commit-sha>`
4. Railway will automatically detect the exposed port
5. Generate a domain or connect your custom domain

**Alternative - GitHub Integration:**
1. Connect your GitHub repository to Railway
2. Set the **Root Directory** to `src/JPVOS` so Railway finds the Dockerfile
3. Railway will build and deploy automatically

### Fly.io

1. Install the Fly CLI: `brew install flyctl` or see [Fly.io docs](https://fly.io/docs/hands-on/install-flyctl/)

2. Use the included `fly.toml` (uses pre-built GHCR image):

```bash
fly auth login
fly launch --no-deploy
# For production, edit fly.toml to use a specific commit SHA tag
fly deploy
```

> **Note**: The included `fly.toml` sets `min_machines_running = 0` for cost savings. For production, edit `fly.toml` and set `min_machines_running = 1` to avoid cold starts.

**Alternative - Build from Dockerfile:**

To build from source instead of using the pre-built image, run from the `src/JPVOS` directory:

```bash
cd src/JPVOS
fly auth login
fly launch --dockerfile Dockerfile --no-deploy
fly deploy
```

**Example fly.toml for production:**

```toml
app = "jpv-os"
primary_region = "iad"

[build]
  image = "ghcr.io/jaypventures-llc/jpv-os:<commit-sha>"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = "stop"
  auto_start_machines = true
  # Set to 1 for production
  min_machines_running = 1

[[http_service.checks]]
  grace_period = "10s"
  interval = "30s"
  method = "GET"
  timeout = "5s"
  path = "/health"
```

Then deploy:

```bash
fly auth login
fly launch --no-deploy
fly deploy
```

### DigitalOcean App Platform

1. Go to [DigitalOcean App Platform](https://cloud.digitalocean.com/apps)
2. Create new app → Select **Container Registry**
3. Enter: `ghcr.io/jaypventures-llc/jpv-os:<commit-sha>`
4. Configure resources and deploy

### Google Cloud Run

```bash
# Pull from GHCR and push to Artifact Registry (gcr.io is being deprecated)
docker pull ghcr.io/jaypventures-llc/jpv-os:<commit-sha>
docker tag ghcr.io/jaypventures-llc/jpv-os:<commit-sha> REGION-docker.pkg.dev/YOUR_PROJECT/YOUR_REPO/jpv-os:<commit-sha>
docker push REGION-docker.pkg.dev/YOUR_PROJECT/YOUR_REPO/jpv-os:<commit-sha>

# Deploy to Cloud Run
gcloud run deploy jpv-os \
  --image REGION-docker.pkg.dev/YOUR_PROJECT/YOUR_REPO/jpv-os:<commit-sha> \
  --platform managed \
  --port 8080 \
  --allow-unauthenticated
```

### AWS App Runner

1. Push the image to Amazon ECR (or use a public ECR)
2. Create an App Runner service pointing to the image
3. Configure port 8080

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |
| `PORT` | `8080` | Listening port (honored by Dockerfile) |

## Health Check

The application exposes a health endpoint at `/health` that returns:

```json
{
  "status": "healthy",
  "timestamp": "<ISO 8601 timestamp>"
}
```

Example response: `{"status":"healthy","timestamp":"2026-05-15T14:30:00.000Z"}`

Most container platforms will automatically use this for health monitoring.

## Building Locally

If you need to build the container image locally:

```bash
cd src/JPVOS
docker build -t jpv-os:local .
docker run -p 8080:8080 jpv-os:local
```

## Manual Deployment from Release Artifact

If you prefer to deploy without containers:

1. Download the `jpvos-release` artifact from GitHub Actions
2. Extract to your server
3. Run with:

```bash
export ASPNETCORE_URLS=http://+:8080
export ASPNETCORE_ENVIRONMENT=Production
dotnet JPVOS.dll
```

Or with a process manager like `systemd` or `pm2`.

## Future: Azure App Service

Azure App Service deployment is planned once Entra ID device compliance and Conditional Access requirements are resolved. The existing `azure-deploy.yml` workflow will be used when:

- [ ] Azure subscription access is restored
- [ ] Resource group and App Service are provisioned
- [ ] Publish profile is configured as a GitHub secret

See [AZURE-APP-SERVICE-DEPLOYMENT.md](./AZURE-APP-SERVICE-DEPLOYMENT.md) for the full Azure deployment plan.
