# Azure App Service Deployment

## Target

Deploy JPVOS as an ASP.NET Core Blazor web app to Azure App Service.

## Runtime

- Framework: .NET 8
- Project: `src/JPVOS/JPVOS.csproj`
- Solution: `JPVOS.sln`
- Hosting target: Azure App Service
- Canonical production workflow: `.github/workflows/deploy-appservice.yml`
- Deployment authentication: GitHub Actions OpenID Connect (OIDC)

## Canonical deployment identity bootstrap

Production deployment does not use App Service publish profiles or long-lived Azure client secrets.

From an authenticated operator environment with Azure CLI and GitHub CLI installed, run:

```powershell
./ops/azure/bootstrap-github-oidc-deployment.ps1 -WebAppName '<production-app-service-name>'
```

The bootstrap script is idempotent and performs the executable setup end to end:

1. Verifies authenticated Azure and GitHub sessions.
2. Resolves the named production App Service in the active Azure subscription.
3. Creates or reuses the `jpv-os-access-gateway-github-actions` Microsoft Entra application.
4. Creates or reuses its service principal.
5. Creates or verifies an exact GitHub OIDC federated credential scoped to `JayPVentures-LLC/jpv-os-access-gateway` and `refs/heads/main`.
6. Assigns `Website Contributor` only at the target App Service scope.
7. Writes `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `AZURE_WEBAPP_NAME` into GitHub Actions secrets.
8. Dispatches `deploy-appservice.yml`, waits for the exact workflow run, and fails if deployment fails.
9. Verifies `/health` reports a healthy runtime with `productionAttentionAdmission.registered == true` and `productionAttentionAdmission.mode == "fail-closed"`.
10. Emits a JSON verification receipt.

If an existing federated credential with the canonical name has different issuer, subject, or audience values, the bootstrap fails closed rather than silently broadening trust.

## Required GitHub Actions secrets

The canonical workflow consumes:

- `AZURE_WEBAPP_NAME`
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

These values are populated by the bootstrap script. `AZURE_WEBAPP_PUBLISH_PROFILE` is not part of the canonical deployment path.

## Deployment workflow

`deploy-appservice.yml` runs on pushes to `main` and by explicit workflow dispatch. It requests only `contents: read` and `id-token: write`, authenticates to Azure through OIDC, builds and publishes the .NET application, deploys it to the configured App Service, then verifies the deployed health and production-attention admission state.

## Existing infrastructure provisioning

App Service infrastructure provisioning remains documented in [AZURE-PROVISIONING-GUIDE.md](./AZURE-PROVISIONING-GUIDE.md). Provisioning the hosting resource and authorizing GitHub to deploy to that resource are separate operations.

## Release gate

No production deployment is verified merely because repository code merged. Terminal deployment verification requires the production workflow to succeed and the deployed `/health` endpoint to confirm the fail-closed production-attention gate.
