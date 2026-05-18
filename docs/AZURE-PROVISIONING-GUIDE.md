JPV-OS Access Gateway - Azure Provisioning

ℹ Checking Azure CLI prerequisites...
✓ Azure CLI version: 2.50.0
✓ GitHub CLI available

ℹ Validating Azure login status...
✓ Logged in as: user@jaypventures-llc
✓ Current tenant: <tenant-id>

ℹ Fetching available subscriptions...
Found 1 subscription(s):
  - JayPVentures LLC Production [<subscription-id>]
✓ Selected subscription: JayPVentures LLC Production [<subscription-id>]

... (more steps)

✓ =========================================
✓ PROVISIONING COMPLETE
✓ =========================================
```

## Deployment Paths

### Path A: JayPVentures LLC Tenant (Recommended)

**Conditions:**
- Using JayPVentures LLC Azure tenant
- Subscription is assigned to your account
- Have Owner or Contributor role

**Process:**
```bash
# Login to JayPVentures LLC tenant
az login --tenant <jpv-tenant-id>

# Run provisioning
./provision-azure-appservice.ps1
```

**Post-Provisioning:**
- Web App: `https://jpv-os-access-gateway.azurewebsites.net`
- Cloudflare DNS points to Azure endpoint
- Custom domain configured in App Service

### Path B: Existing Subscription with Quota

**Conditions:**
- You have an existing Azure subscription
- Regional quota is available (≥ 1 VM core)
- Have appropriate permissions

**Process:**
```bash
# Login to your subscription
az login

# Select your subscription if multiple exist
az account set --subscription "<your-subscription-id>"

# Run provisioning with custom parameters
./provision-azure-appservice.ps1 `
  -ResourceGroupName "rg-jpv-custom" `
  -Regions "eastus,centralus,westus"
```

**Quota Verification:**
```bash
# Check current quota in a region
az compute vm list-usage --location eastus

# Request quota increase if needed
# Via Portal: https://portal.azure.com > Subscriptions > Usage + quotas
```

### Path C: Alternate Runtime Hosts

**If Azure provisioning is blocked**, use container deployment:

- **Render** - See `docs/CONTAINER-DEPLOYMENT.md`
- **Railway, Fly.io, DigitalOcean App Platform**
- **Google Cloud Run, AWS App Runner**

Container image is automatically published to GitHub Container Registry:
```bash
docker pull ghcr.io/jaypventures-llc/jpv-os:latest
docker run -p 8080:8080 ghcr.io/jaypventures-llc/jpv-os:latest
```

## Validation Checks

### Tenant Access Validation

```
✓ Azure CLI installed
✓ Logged into Azure
✓ Current account identified
✓ Subscriptions available
✓ Subscription selected
```

**Failure Scenarios:**

| Scenario | Remediation |
|----------|-------------|
| Azure CLI not installed | Install from https://aka.ms/azure-cli |
| Not logged in | Run `az login` |
| No subscriptions | Contact Azure administrator for subscription assignment |
| Wrong tenant | Run `az login --tenant <tenant-id>` |

### Subscription Authority Validation

```
✓ Required roles confirmed
✓ Microsoft.Web provider registered
✓ RBAC permissions validated
```

**Role Requirements:**

| Role | Permissions | Notes |
|------|-------------|-------|
| Owner | Full access | Recommended, can manage all resources |
| Contributor | Most operations | Can create App Service resources |
| Website Contributor | App Service specific | Limited to App Service operations |

**Provider Registration:**

```bash
# Check status
az provider show --namespace Microsoft.Web

# Register if needed
az provider register --namespace Microsoft.Web

# Verify registration (may take 1-2 minutes)
az provider show --namespace Microsoft.Web --query "registrationState"
```

### Regional Quota Validation

The script checks these regions by priority:
1. **East US** - Primary region
2. **Central US** - Fallback region
3. **East US 2** - Backup region
4. **West US 2** - West Coast option

**Quota Requirements:**

- Minimum 1 VM core for specified SKU
- For B1 SKU: 1 core minimum

**Quota Verification:**

```bash
# Check current quota
az compute vm list-usage --location eastus --query "[].{name:name.value, currentValue:currentValue, limit:limit}"

# Example output:
# Name                    CurrentValue    Limit
# ----                    ------------    -----
# Total VM Cores          2               10
# Standard B Family Cores 1               5
```

**Quota Increase Process:**

1. Navigate to Azure Portal: https://portal.azure.com
2. Go to: **Subscriptions** > **Usage + quotas**
3. Select region where quota is 0
4. Find "Total VM cores" quota
5. Click **"Request quota increase"**
6. Select new quota (e.g., 2 or 4 cores)
7. Submit request (usually approved within 30 minutes)
8. Re-run provisioning script

### Resource Provisioning Validation

```
✓ Resource group created
✓ App Service plan created (B1 or specified SKU)
✓ Web App created (.NET 8 runtime)
✓ HTTPS enforcement enabled
✓ App settings configured
```

**Blockers:**

- ✗ App Service plan creation fails → **STOP** (quota issue or RBAC)
- ✗ Web App creation fails → **STOP** (plan failed or name conflict)
- ✗ HTTPS configuration fails → Warning only, continues

## Publish Profile Configuration

### Automatic Configuration (with GitHub CLI)

```bash
# Script automatically:
# 1. Generates publish profile
# 2. Reads file content
# 3. Sets GitHub secret
# 4. Triggers workflow

# Verification:
gh secret list --repo <owner>/<repo>
# Should show: AZURE_WEBAPP_PUBLISH_PROFILE
```

### Manual Configuration (without GitHub CLI)

```bash
# 1. Get publish profile
az webapp deployment list-publishing-profiles \
  --name jpv-os-access-gateway \
  --resource-group rg-jpv-os-prod > profile.xml

# 2. Copy content
cat profile.xml

# 3. Set secret in GitHub
# - Go to: https://github.com/<owner>/<repo>/settings/secrets/actions
# - Click "New repository secret"
# - Name: AZURE_WEBAPP_PUBLISH_PROFILE
# - Value: (paste profile.xml content)
# - Click "Add secret"

# 4. Verify
# - Workflow: .github/workflows/deploy-appservice.yml
# - Check for usage of AZURE_WEBAPP_PUBLISH_PROFILE
```

## Deployment Workflow

### Automatic Deployment

The script automatically:

1. Checks for GitHub CLI (`gh` command)
2. Reads publish profile from file
3. Sets GitHub secret: `AZURE_WEBAPP_PUBLISH_PROFILE`
4. Triggers workflow: `deploy-appservice.yml`
5. Waits for workflow to complete
6. Tests health endpoint

### Manual Deployment

If automatic deployment fails:

```bash
# 1. Ensure publish profile secret is set
gh secret list

# 2. Manually trigger workflow
gh workflow run deploy-appservice.yml --repo <owner>/<repo>

# 3. Monitor workflow
gh run list --workflow deploy-appservice.yml
gh run view <run-id>

# 4. View workflow logs
gh run view <run-id> --log
```

### Workflow Details

**Workflow:** `.github/workflows/deploy-appservice.yml`

```yaml
name: deploy-appservice
on:
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - run: dotnet restore ./src/JPVOS/JPVOS.csproj
      - run: dotnet build ./src/JPVOS/JPVOS.csproj --configuration Release
      - run: dotnet publish ./src/JPVOS/JPVOS.csproj -c Release -o ./publish
      - uses: azure/webapps-deploy@v3
        with:
          app-name: jpv-os-access-gateway
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

## Health Endpoint Validation

### Automatic Health Check

The script automatically:

1. Waits up to 5 minutes for application to start
2. Polls `/api/health` endpoint every 10 seconds
3. Validates successful response (HTTP 200)
4. Displays response JSON

### Manual Health Check

```bash
# Via browser
https://jpv-os-access-gateway.azurewebsites.net/api/health

# Via curl
curl https://jpv-os-access-gateway.azurewebsites.net/api/health

# Via PowerShell
$response = Invoke-WebRequest -Uri "https://jpv-os-access-gateway.azurewebsites.net/api/health"
$response.Content | ConvertFrom-Json | Format-Table
```

### Expected Response

```json
{
  "status": "healthy",
  "app": "JPV-OS Access Gateway",
  "runtime": ".NET",
  "utc": "2024-05-17T23:29:50.191Z"
}
```

## Troubleshooting

### Common Issues

#### 1. Azure CLI Not Found

**Error:** `Azure CLI is not installed`

**Solution:**
```bash
# Windows (PowerShell)
winget install Microsoft.AzureCLI

# macOS
brew install azure-cli

# Linux (Ubuntu/Debian)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

#### 2. Not Logged Into Azure

**Error:** `Not logged into Azure. Run: az login`

**Solution:**
```bash
az login
# Complete browser authentication
# Select correct subscription if prompted
```

#### 3. No Subscriptions Found

**Error:** `No subscriptions found. The current account has no subscriptions assigned.`

**Solution:**

1. Contact Azure administrator
2. Request subscription assignment for your account
3. For JayPVentures LLC: Submit request through Azure portal
4. Re-run script after subscription is assigned

#### 4. Wrong Role/Insufficient Permissions

**Error:** `Current account does not have required roles for resource creation`

**Solution:**
```bash
# Check current roles
az role assignment list --query "[].roleDefinitionName" -o table

# Request Owner, Contributor, or Website Contributor role
# Contact subscription owner for role assignment
```

#### 5. Microsoft.Web Provider Not Registered

**Error:** `Failed to register Microsoft.Web provider`

**Solution:**

```bash
# Manual registration
az provider register --namespace Microsoft.Web

# Check status
az provider show --namespace Microsoft.Web --query "registrationState"

# Wait 1-2 minutes and re-run script
```

#### 6. No Viable Regions (Quota = 0)

**Error:** `No viable regions found with available quota for SKU: B1`

**Solution:**

1. Navigate to Azure Portal: https://portal.azure.com
2. Go to: **Subscriptions** > **Usage + quotas**
3. For each region (East US, Central US, East US 2, West US 2):
   - Check "Total VM Cores" quota
   - If 0, click "Request quota increase"
   - Request 2-4 cores (sufficient for B1-B2 SKUs)
4. Wait for approval (typically 30 minutes)
5. Re-run provisioning script

**Example Quota Increase:**

```
Region: East US
Current Quota: Total VM Cores: 0/10
New Request: Total VM Cores: 4/10
Reason: Application deployment (JPV-OS Access Gateway)
```

#### 7. App Service Plan Creation Failed

**Error:** `Failed to create App Service plan. This is a blocker. Do not continue.`

**Causes:**
- Insufficient VM quota
- Invalid SKU for region
- Resource group creation failed
- RBAC restrictions

**Solution:**

1. Verify quota (see "No Viable Regions" above)
2. Check SKU availability:
   ```bash
   az appservice list-locations --query "[?name=='eastus'].{name:name, skus:skus}"
   ```
3. Verify RBAC permissions
4. Contact Azure support if issue persists

#### 8. Publish Profile Not Generated

**Error:** `Failed to generate publish profile`

**Solution:**

```bash
# Manual generation
az webapp deployment list-publishing-profiles \
  --name jpv-os-access-gateway \
  --resource-group rg-jpv-os-prod > azure-publish-profile.xml

# Verify file was created
ls -l azure-publish-profile.xml

# Manually set GitHub secret with content
```

#### 9. GitHub Secret Configuration Failed

**Error:** `Failed to set GitHub secret`

**Solution:**

1. Ensure GitHub CLI is installed: `gh --version`
2. Authenticate: `gh auth login`
3. Verify repository access: `gh repo view`
4. Manually set secret:
   - Go to: GitHub repo > Settings > Secrets and variables > Actions
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: (paste profile.xml content)

#### 10. Health Endpoint Timeout

**Error:** `Health endpoint not responding after X minutes`

**Cause:** Application still deploying or startup issues

**Solution:**

```bash
# Check deployment status in Azure Portal
az webapp deployment list --name jpv-os-access-gateway --resource-group rg-jpv-os-prod

# View application logs
az webapp log tail --name jpv-os-access-gateway --resource-group rg-jpv-os-prod

# Check if app is running
az webapp show --name jpv-os-access-gateway --resource-group rg-jpv-os-prod --query "state"

# Restart app
az webapp restart --name jpv-os-access-gateway --resource-group rg-jpv-os-prod

# Retry health check
curl https://jpv-os-access-gateway.azurewebsites.net/api/health
```

## Post-Provisioning Configuration

### Custom Domain Setup

```bash
# 1. Get App Service endpoint
az webapp show --name jpv-os-access-gateway --resource-group rg-jpv-os-prod --query "defaultHostName"
# Result: jpv-os-access-gateway.azurewebsites.net

# 2. Add custom domain binding
az webapp config hostname add \
  --webapp-name jpv-os-access-gateway \
  --resource-group rg-jpv-os-prod \
  --hostname api.jpvos.org

# 3. Configure DNS (in Cloudflare/registrar)
# Add CNAME record:
# Name: api
# Content: jpv-os-access-gateway.azurewebsites.net
# Proxy status: Proxied (Cloudflare)

# 4. Add SSL certificate
az webapp config ssl upload \
  --name jpv-os-access-gateway \
  --resource-group rg-jpv-os-prod \
  --certificate-file cert.pfx \
  --certificate-password <password>
```

### Environment Variables

```bash
# Set application settings
az webapp config appsettings set \
  --name jpv-os-access-gateway \
  --resource-group rg-jpv-os-prod \
  --settings \
  STRIPE_SECRET_KEY="sk_live_..." \
  DISCORD_TOKEN="..." \
  ENVIRONMENT="production"

# View settings
az webapp config appsettings list \
  --name jpv-os-access-gateway \
  --resource-group rg-jpv-os-prod
```

### Monitoring and Alerts

```bash
# Enable Application Insights
az monitor app-insights component create \
  --app jpv-os-access-gateway \
  --location eastus \
  --resource-group rg-jpv-os-prod

# Set up alerts for health checks
az monitor metrics alert create \
  --name "JPV-OS Health Alert" \
  --resource-group rg-jpv-os-prod \
  --scopes /subscriptions/<sub-id>/resourceGroups/rg-jpv-os-prod/providers/Microsoft.Web/sites/jpv-os-access-gateway \
  --condition "avg RequestsSuccessful < 95" \
  --window-size 5m \
  --evaluation-frequency 1m
```

## Security Considerations

### Publish Profile Security

⚠️ **Critical**: Publish profile contains deployment credentials

```bash
# Do NOT commit to git
git add .gitignore  # ensure *.xml is excluded

# Do NOT share in public channels
# Only store in GitHub Secrets

# Regenerate if compromised
az webapp deployment list-publishing-profiles --name <web-app> --resource-group <rg> --query [0] -o json > new-profile.xml
```

### GitHub Secret Security

```bash
# Verify secret is set correctly
gh secret list

# Secret value is never displayed, only name and update date shown
# To update, use:
cat profile.xml | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -

# To delete (if compromised)
gh secret delete AZURE_WEBAPP_PUBLISH_PROFILE
```

### HTTPS Enforcement

The script automatically:
- Enables HTTPS-only traffic
- Enforces TLS 1.2+
- Configures HTTP → HTTPS redirect

```bash
# Verify HTTPS configuration
az webapp show --name jpv-os-access-gateway --resource-group rg-jpv-os-prod --query "httpsOnly"
# Expected: true
```

### Secrets Protection

⚠️ **DO NOT expose in client code:**

- STRIPE_SECRET_KEY
- DISCORD_TOKEN
- Database connection strings

**Safe pattern:**
```csharp
// In Program.cs (server-side only)
Stripe.StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

// NOT in Blazor components (client-side)
// Use API controllers to expose only safe data
```

## Verification Checklist

After provisioning completes:

- [ ] Publish profile generated: `azure-publish-profile-jpv-os-access-gateway.xml`
- [ ] GitHub secret set: `AZURE_WEBAPP_PUBLISH_PROFILE` visible in repo settings
- [ ] Deployment triggered: Check GitHub Actions > deploy-appservice workflow
- [ ] Deployment successful: Workflow completed without errors
- [ ] Health endpoint responding: `https://jpv-os-access-gateway.azurewebsites.net/api/health` returns HTTP 200
- [ ] Response contains valid JSON with status "healthy"
- [ ] Cloudflare DNS configured (if applicable)
- [ ] Custom domain resolves (if applicable)
- [ ] HTTPS enforced: Only https:// URLs work

## Advanced Configuration

### High Availability (Premium Plan)

For production workloads requiring high availability:

```bash
# Use Premium plan with auto-scale
./provision-azure-appservice.ps1 -SkuName "P1V2" -ResourceGroupName "rg-jpv-os-prod-ha"

# Configure auto-scaling
az monitor autoscale create \
  --resource-group rg-jpv-os-prod-ha \
  --resource-name asp-jpv-os-prod \
  --resource-type "Microsoft.Web/serverfarms" \
  --min-count 2 \
  --max-count 10 \
  --count 2
```

### Multi-Region Deployment

For geographic distribution:

```bash
# Deploy to multiple regions
./provision-azure-appservice.ps1 -Regions "eastus,westus,northeurope"

# Configure Traffic Manager for load balancing
az network traffic-manager profile create \
  --name tm-jpvos \
  --resource-group rg-jpv-os-prod \
  --routing-method Geographic
```

### Disaster Recovery

```bash
# Create backup resource group in different region
./provision-azure-appservice.ps1 \
  -ResourceGroupName "rg-jpv-os-dr" \
  -Regions "westus2"

# Set up geo-replication for database
az appservice plan create \
  --name asp-jpv-os-dr \
  --resource-group rg-jpv-os-dr \
  --location westus2 \
  --sku P1V2
```

## Support and Documentation

- **Azure CLI Docs**: https://learn.microsoft.com/en-us/cli/azure/
- **App Service Docs**: https://learn.microsoft.com/en-us/azure/app-service/
- **GitHub Actions**: https://docs.github.com/en/actions
- **Issue Tracker**: https://github.com/JayPVentures-LLC/jpv-os-access-gateway/issues

## Deprecation and Cleanup

To remove Azure resources:

```bash
# Delete entire resource group and all resources
az group delete --name rg-jpv-os-prod --yes

# Delete specific app
az webapp delete --name jpv-os-access-gateway --resource-group rg-jpv-os-prod

# Remove GitHub secret
gh secret delete AZURE_WEBAPP_PUBLISH_PROFILE
```

## Version History

- **2024-05-17** - Initial release with comprehensive validation, quota checking, multi-region support, and automated deployment
# Azure App Service Provisioning Guide

## Overview

This guide helps you provision Azure App Service infrastructure for the JPV-OS Access Gateway application. Azure infrastructure provisioning is critical for deploying the real production instance of the app.

## Prerequisites

### Required Tools

1. **Azure CLI** - Command-line interface for Azure management
   - Download: https://aka.ms/azure-cli
   - Verify: `az --version`

2. **PowerShell 7.0+** (for Windows provisioning script)
   - Download: https://github.com/PowerShell/PowerShell/releases
   - Verify: `pwsh --version`

3. **GitHub CLI** (optional, for automated secret registration)
   - Download: https://cli.github.com
   - Verify: `gh --version`

### Azure Subscription

You must have an Azure subscription with usable App Service quota. Three paths are available:

- **Path A**: JayPVentures LLC tenant subscription
- **Path B**: Existing Default Directory subscription (may require quota increase)
- **Path C**: Alternate runtime host (Render, Railway, Fly.io, etc.)

## Quick Start

### Windows (PowerShell)

```powershell
# Validate prerequisites only
.\scripts\provision-azure-appservice.ps1 -Validate

# Provision with Path A (JayPVentures LLC tenant)
.\scripts\provision-azure-appservice.ps1 -Path A -SubscriptionId "YOUR-SUBSCRIPTION-ID"

# Provision with Path B (existing subscription, after quota increase)
.\scripts\provision-azure-appservice.ps1 -Path B -SubscriptionId "YOUR-SUBSCRIPTION-ID"

# List alternate runtime options (Path C)
.\scripts\provision-azure-appservice.ps1 -Path C
```

### Linux / macOS (Bash)

```bash
# Validate prerequisites only
./scripts/provision-azure-appservice.sh -Validate

# Provision with Path A (JayPVentures LLC tenant)
./scripts/provision-azure-appservice.sh -Path A -SubscriptionId "YOUR-SUBSCRIPTION-ID"

# Provision with Path B (existing subscription, after quota increase)
./scripts/provision-azure-appservice.sh -Path B -SubscriptionId "YOUR-SUBSCRIPTION-ID"

# List alternate runtime options (Path C)
./scripts/provision-azure-appservice.sh -Path C
```

## Deployment Paths

### Path A: JayPVentures LLC Tenant Subscription

**Recommended when:** You have access to a subscription under the JayPVentures LLC tenant.

**Steps:**

1. Obtain a JayPVentures LLC tenant subscription ID
2. Run the provisioning script:
   ```powershell
   .\scripts\provision-azure-appservice.ps1 -Path A -SubscriptionId "sub-xxx"
   ```
3. The script will:
   - Authenticate with Azure
   - Create resource group
   - Create App Service plan (B1 SKU recommended)
   - Create Web App with .NET 8 runtime
   - Generate publish profile
   - Register GitHub secrets automatically (if GitHub CLI available)

4. Verify prerequisites are met:
   ```
   ✓ Azure subscription with usable App Service quota is active
   ✓ `az appservice plan create` succeeded
   ✓ `az webapp create` succeeded
   ✓ Real publish profile generated
   ```

5. Configure app settings in [Azure Portal](https://portal.azure.com):
   - Set `STRIPE_SECRET_KEY` and webhook secret
   - Set `DISCORD_CLIENT_ID`, `DISCORD_CLIENT_SECRET`, etc.
   - Configure other required environment variables

6. Deploy using GitHub Actions workflow:
   - Go to Repository > Actions
   - Select "Deploy to Azure App Service"
   - Click "Run workflow"

### Path B: Existing Subscription Quota Increase

**Recommended when:** You have an existing Azure subscription but quota is insufficient.

**Common Issue:** "Total VMs quota is 0" or similar quota limit error

**Steps:**

1. Request quota increase in Azure Portal:
   - Go to https://portal.azure.com
   - Search for "Subscriptions" > Select your subscription
   - Select "Usage + quotas" on the left panel
   - Filter by Region: Select your region (e.g., "East US")
   - Find "Total vCores" or similar capacity metric
   - Click "Request quota increase"
   - Request minimum: 1 vCore
   - Recommended: 2-4 vCores for production

2. Wait for quota increase approval (typically 1-24 hours)

3. Run the provisioning script:
   ```powershell
   .\scripts\provision-azure-appservice.ps1 -Path B -SubscriptionId "your-sub-id"
   ```

4. Follow the same deployment steps as Path A

### Path C: Alternate Runtime Hosts

**Recommended when:** Azure provisioning cannot be completed in your timeframe.

**Supported Platforms:**

#### 1. Fly.io (Recommended for quick start)

```bash
fly auth login
fly deploy  # Uses existing fly.toml
```

- Zero-downtime deployments
- Global Anycast network
- Health checks configured at `/health`
- Configuration: `fly.toml` (already configured)

#### 2. Render

```bash
render deploy  # Uses existing render.yaml
```

- Free and paid tiers available
- Automatic HTTPS
- Configuration: `render.yaml` (already configured)

#### 3. Railway

```bash
railway deploy
```

- Container-based deployment
- Easy environment variable management

#### 4. DigitalOcean App Platform

- Container support
- Integrated monitoring
- Documentation: https://docs.digitalocean.com/products/app-platform/

#### 5. AWS App Runner

- Container orchestration
- Auto-scaling
- Documentation: https://aws.amazon.com/apprunner/

For detailed instructions on alternate platforms, see [CONTAINER-DEPLOYMENT.md](./CONTAINER-DEPLOYMENT.md).

## Advanced Configuration

### Custom Parameters

The provisioning scripts accept optional parameters:

```powershell
# PowerShell example with all options
.\scripts\provision-azure-appservice.ps1 `
    -Path A `
    -SubscriptionId "your-id" `
    -TenantId "f2f234f1-e912-4f16-a31d-6a102faea644" `
    -ResourceGroup "custom-rg" `
    -AppServicePlan "custom-plan" `
    -WebAppName "custom-app-name" `
    -Region "westus2" `
    -SkuSize "S1"  # B1 (1 core), S1 (1 core), S2 (2 cores), etc.
```

### App Service Plan SKU Options

> **Note:** Pricing shown below is approximate and based on historical rates. Prices may change. For current pricing, see [Azure App Service Pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/).

| SKU | Cores | RAM | Price (approx.) | Best For |
|-----|-------|-----|------------------|----------|
| B1  | 1     | 1GB | ~$10/mo | Development, small production |
| B2  | 2     | 3.5GB | ~$50/mo | Small production workloads |
| B3  | 4     | 7GB | ~$100/mo | Medium production |
| S1  | 1     | 1.75GB | ~$75/mo | Production (1 instance) |
| S2  | 2     | 3.5GB | ~$150/mo | Production (2 instances) |
| S3  | 4     | 7GB | ~$300/mo | Production (3 instances) |
| P1V2 | 1 | 3.5GB | ~$195/mo | High-performance |

Recommendation: **B1 for quick validation, S1+ for production**

### Troubleshooting

#### Authentication Failures

```powershell
# Sign out and re-authenticate
az logout
.\scripts\provision-azure-appservice.ps1 -Path A -SubscriptionId "your-id"
```

#### Quota Errors

```
Error: Total VMs quota is 0
```

**Solution:** Request quota increase (Path B) or use alternate runtime (Path C)

#### Resource Already Exists

The script checks for existing resources and reuses them. If you want to recreate:

```powershell
# Delete via Azure Portal or CLI
az group delete --name "rg-jpv-os-prod"

# Re-run provisioning script
.\scripts\provision-azure-appservice.ps1 -Path A -SubscriptionId "your-id"
```

#### Publish Profile Secret Not Registered

If GitHub CLI is not available, manually register the secret:

```powershell
# PowerShell (recommended)
Get-Content -Raw ".\scripts\azure-publish-profile-jpv-os-access-gateway.xml" | `
    gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -
```

```bash
# Bash
cat ./scripts/azure-publish-profile-jpv-os-access-gateway.xml | \
    gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -
```

## Deployment Workflow

After provisioning, the complete deployment workflow is:

```
1. Provisioning (this script)
   ├─ Azure subscription verified
   ├─ Resource group created
   ├─ App Service plan created
   ├─ Web app created (.NET 8)
   ├─ HTTPS configured
   └─ Publish profile registered

2. Pre-Deployment (manual)
   ├─ App settings configured (Stripe, Discord, etc.)
   ├─ Custom domain configured (optional)
   └─ Monitoring configured (optional)

3. GitHub Actions Deployment
   ├─ Code pushed to main
   ├─ CI workflow builds and tests
   ├─ Deploy workflow publishes to Azure
   └─ Application runs at https://jpv-os-access-gateway.azurewebsites.net
```

## Production Checklist

Before deploying to production:

- [ ] Azure subscription provisioned successfully
- [ ] `az appservice plan create` succeeded
- [ ] `az webapp create` succeeded
- [ ] Publish profile downloaded and registered
- [ ] Environment variables configured (Stripe, Discord, etc.)
- [ ] HTTPS enforced (automatic with provisioning script)
- [ ] Health check endpoint accessible (`GET /health`)
- [ ] Database backup strategy in place
- [ ] Monitoring/logging configured
- [ ] Incident response plan documented

## Support & References

- **Azure Documentation**: https://docs.microsoft.com/en-us/azure/app-service/
- **Azure CLI Reference**: https://docs.microsoft.com/en-us/cli/azure/
- **App Service Limits**: https://docs.microsoft.com/en-us/azure/app-service/limits-quotas-constraints
- **Pricing**: https://azure.microsoft.com/en-us/pricing/details/app-service/

## Related Documentation

- [Azure App Service Deployment Plan](./AZURE-APP-SERVICE-DEPLOYMENT.md) - Infrastructure requirements
- [Container Deployment Guide](./CONTAINER-DEPLOYMENT.md) - Alternate platforms
- [Deployment Guide](./DEPLOYMENT.md) - General deployment information
- [Local Setup](./LOCAL-SETUP.md) - Development environment setup
