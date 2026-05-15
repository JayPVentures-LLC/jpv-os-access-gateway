# Container Deployment Guide

This guide provides instructions for deploying JPVOS using container platforms as an alternative to Azure App Service.

## Container Image

The container image is automatically built and pushed to GitHub Container Registry (ghcr.io) on every push to the `main` branch.

### Image Location

```
ghcr.io/jaypventures-llc/jpvos:latest
```

### Available Tags

- `latest` - Most recent build from main branch
- `<sha>` - Git commit SHA for specific versions
- `YYYYMMDD-HHmmss` - Timestamp-based tags

## Quick Start

### Pull and Run Locally

```bash
docker pull ghcr.io/jaypventures-llc/jpvos:latest
docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpvos:latest
```

Visit http://localhost:8080 to access the application.

## Deployment Platforms

### Render

1. Create a new **Web Service** on [Render](https://render.com)
2. Select **Deploy an existing image from a registry**
3. Enter the image URL: `ghcr.io/jaypventures-llc/jpvos:latest`
4. Configure environment variables if needed
5. Deploy

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `PORT`: Render will set this automatically

### Railway

1. Create a new project on [Railway](https://railway.app)
2. Add a new service and select **Docker Image**
3. Enter: `ghcr.io/jaypventures-llc/jpvos:latest`
4. Railway will automatically detect the exposed port
5. Generate a domain or connect your custom domain

**Alternative - GitHub Integration:**
1. Connect your GitHub repository to Railway
2. Railway will automatically detect the Dockerfile and build

### Fly.io

1. Install the Fly CLI: `brew install flyctl` or see [Fly.io docs](https://fly.io/docs/hands-on/install-flyctl/)

2. Create a `fly.toml` in the repository root:

```toml
app = "jpvos"
primary_region = "iad"

[build]
  image = "ghcr.io/jaypventures-llc/jpvos:latest"

[http_service]
  internal_port = 8080
  force_https = true
  auto_stop_machines = true
  auto_start_machines = true
  min_machines_running = 0

[checks]
  [checks.health]
    port = 8080
    type = "http"
    interval = "30s"
    timeout = "5s"
    path = "/health"
```

3. Deploy:

```bash
fly auth login
fly launch --no-deploy
fly deploy
```

### DigitalOcean App Platform

1. Go to [DigitalOcean App Platform](https://cloud.digitalocean.com/apps)
2. Create new app → Select **Container Registry**
3. Enter: `ghcr.io/jaypventures-llc/jpvos`
4. Configure resources and deploy

### Google Cloud Run

```bash
# Pull from GHCR and push to Google Container Registry
docker pull ghcr.io/jaypventures-llc/jpvos:latest
docker tag ghcr.io/jaypventures-llc/jpvos:latest gcr.io/YOUR_PROJECT/jpvos:latest
docker push gcr.io/YOUR_PROJECT/jpvos:latest

# Deploy to Cloud Run
gcloud run deploy jpvos \
  --image gcr.io/YOUR_PROJECT/jpvos:latest \
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
| `ASPNETCORE_URLS` | `http://+:8080` | Listening URL (set in Dockerfile) |
| `PORT` | - | Some platforms set this; the app uses 8080 by default |

## Health Check

The application exposes a health endpoint at `/health` that returns:

```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00.000Z"
}
```

Most container platforms will automatically use this for health monitoring.

## Building Locally

If you need to build the container image locally:

```bash
cd src/JPVOS
docker build -t jpvos:local .
docker run -p 8080:8080 jpvos:local
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
