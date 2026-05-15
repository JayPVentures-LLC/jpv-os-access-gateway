# Azure App Service Deployment Plan

## Target

Deploy JPVOS as an ASP.NET Core Blazor web app.

## Runtime

- Framework: .NET 8
- Project: src/JPVOS/JPVOS.csproj
- Solution: JPVOS.sln
- Release artifact: jpvos-release
- Hosting target: Azure App Service

## Required Azure Setup

- [ ] Azure subscription selected
- [ ] Resource group created (e.g., `rg-jpv-os-prod`)
- [ ] App Service Plan created (recommend B1 or higher)
- [ ] Web App created with .NET 8 runtime
- [ ] Runtime stack configured for .NET 8
- [ ] HTTPS enforced
- [ ] App settings configured:
  - `AllowedHosts` - Your domain(s), e.g., `yourdomain.azurewebsites.net;yourdomain.com`
- [ ] Deployment credentials or publish profile configured
- [ ] GitHub Actions secrets added:
  - `AZURE_WEBAPP_NAME` - Your Web App name
  - `AZURE_WEBAPP_PUBLISH_PROFILE` - Publish profile XML content
- [x] Deployment workflow added (`azure-deploy.yml`)
- [x] Health endpoint added (`/health`)
- [ ] Custom domain configured
- [ ] Privacy policy URL configured
- [ ] Terms URL configured
- [ ] Support contact configured

## Deployment Workflows

- **CI Build** (`ci.yml`) - Runs on every PR and push to main
- **Release Package** (`release-package.yml`) - Creates release artifacts
- **Deploy to Azure** (`azure-deploy.yml`) - Manual deployment to Azure App Service

## Release Gate

No production deployment is approved until build, release artifact, HTTPS, environment configuration, logging, privacy, terms, and rollback path are verified.
