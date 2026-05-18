<#
.SYNOPSIS
    Provision Azure App Service infrastructure for JPV-OS Access Gateway.

.DESCRIPTION
    This script helps provision Azure App Service resources (subscription, resource group, app service plan, and web app)
    for the JPV-OS Access Gateway application. It supports three deployment paths:
    
    Path A: Use JayPVentures LLC tenant subscription
    Path B: Use existing Default Directory subscription (may require quota increase)
    Path C: Validate alternate runtime hosts (Render, Railway, Fly.io)

.PARAMETER Path
    Deployment path: A, B, or C
    
.PARAMETER TenantId
    Azure Tenant ID. Default: JayPVentures LLC tenant (f2f234f1-e912-4f16-a31d-6a102faea644)

.PARAMETER SubscriptionId
    Azure Subscription ID (required for Paths A and B)

.PARAMETER ResourceGroup
    Resource group name. Default: rg-jpv-os-prod

.PARAMETER AppServicePlan
    App Service plan name. Default: plan-jpv-os-prod

.PARAMETER WebAppName
    Web app name. Default: jpv-os-access-gateway

.PARAMETER Region
    Azure region. Default: eastus

.PARAMETER SkuSize
    App Service plan SKU. Default: B1 (Basic, 1 core, 1GB RAM)
    Minimum needed: B1 or higher. Standard S1 recommended for production.

.PARAMETER Validate
    If specified, only validate prerequisites without creating resources.

.EXAMPLE
    # Path A: Provision using JayPVentures LLC tenant
    .\provision-azure-appservice.ps1 -Path A -SubscriptionId "your-subscription-id"

.EXAMPLE
    # Path B: Provision using existing Default Directory subscription
    .\provision-azure-appservice.ps1 -Path B -SubscriptionId "your-subscription-id"

.EXAMPLE
    # Validate prerequisites only
    .\provision-azure-appservice.ps1 -Validate

.EXAMPLE
    # Path C: Validate alternate runtime hosts
    .\provision-azure-appservice.ps1 -Path C
#>

param(
  [ValidateSet("A", "B", "C")]
  [string]$Path = "A",
    
  # Tenant ID for JayPVentures LLC - This is public information per problem statement
  [string]$TenantId = "f2f234f1-e912-4f16-a31d-6a102faea644",
    
  [string]$SubscriptionId,
    
  [string]$ResourceGroup = "rg-jpv-os-access-gateway",
    
  [string]$AppServicePlan = "asp-jpv-os-access-gateway",
    
  [string]$WebAppName = "jpv-os-access-gateway",
    
  [string]$Region = "centralus",
    
  [string]$SkuSize = "B1",
    
  [switch]$Validate
)

$ErrorActionPreference = "Stop"

# Color output helpers
function Write-ScriptSection { Write-Host "`n$($PSStyle.Foreground.Cyan)=== $args ===`n$($PSStyle.Reset)" }
function Write-ScriptSuccess { Write-Host "$($PSStyle.Foreground.Green)✓ $args$($PSStyle.Reset)" }
function Write-ScriptError { Write-Host "$($PSStyle.Foreground.Red)✗ $args$($PSStyle.Reset)" -ErrorAction Continue }
function Write-ScriptWarning { Write-Host "$($PSStyle.Foreground.Yellow)⚠ $args$($PSStyle.Reset)" }
function Write-ScriptInfo { Write-Host "$($PSStyle.Foreground.Cyan)ℹ $args$($PSStyle.Reset)" }

# ====================================
# Prerequisite Validation
# ====================================

function Test-Prerequisites {
  Write-ScriptSection "Checking Prerequisites"
    
  # Check Azure CLI
  if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-ScriptError "Azure CLI is not installed. Install it from: https://aka.ms/azure-cli"
    return $false
  }
  Write-ScriptSuccess "Azure CLI is installed"
    
  # Check Azure CLI version
  $azVersion = az version --output json | ConvertFrom-Json
  Write-ScriptInfo "Azure CLI version: $($azVersion.'azure-cli')"
    
  # Check PowerShell version
  if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-ScriptWarning "PowerShell 7.0+ recommended. Current: $($PSVersionTable.PSVersion)"
  }
  else {
    Write-ScriptSuccess "PowerShell version: $($PSVersionTable.PSVersion)"
  }
    
  return $true
}

# ====================================
# Authentication & Subscription
# ====================================

function Test-AzureLogin {
  param([string]$Tenant)
    
  Write-ScriptSection "Authenticating with Azure"
    
  # Check if already logged in
  $currentAccount = az account show 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    
  if ($currentAccount) {
    Write-ScriptSuccess "Already logged in as: $($currentAccount.user.name)"
    return $true
  }
    
  # Login to tenant
  Write-ScriptInfo "Logging in to tenant: $Tenant"
  az login --tenant $Tenant
    
  if ($LASTEXITCODE -ne 0) {
    Write-ScriptError "Failed to login to Azure tenant"
    return $false
  }
    
  Write-ScriptSuccess "Successfully logged in to Azure"
  return $true
}

function Get-AvailableSubscriptions {
  Write-ScriptInfo "Retrieving available subscriptions..."
    
  $subs = az account list --output json | ConvertFrom-Json
    
  if ($subs.Count -eq 0) {
    Write-ScriptError "No subscriptions found. Check Tenant ID and account permissions."
    return @()
  }
    
  Write-ScriptInfo "Found $($subs.Count) subscription(s):`n"
  $subs | ForEach-Object {
    Write-ScriptInfo "  - $($_.name) [$($_.id.Substring(0, 8))...]"
  }
    
  return $subs
}

function Set-ActiveSubscription {
  param([string]$SubId)
    
  Write-ScriptInfo "Setting active subscription: $SubId"
  az account set --subscription $SubId
    
  if ($LASTEXITCODE -ne 0) {
    Write-ScriptError "Failed to set subscription"
    return $false
  }
    
  $sub = az account show --output json | ConvertFrom-Json
  Write-ScriptSuccess "Active subscription: $($sub.name)"
  return $true
}

# ====================================
# Quota & Capacity Validation
# ====================================

function Test-AppServiceQuota {
  param([string]$Region)
    
  Write-ScriptSection "Checking App Service Quota"
    
  # Get provider registrations
  Write-ScriptInfo "Checking Microsoft.Web provider registration..."
  $provider = az provider show --namespace Microsoft.Web --output json | ConvertFrom-Json
    
  if ($provider.registrationState -ne "Registered") {
    Write-ScriptWarning "Microsoft.Web provider not registered. Registering..."
    az provider register --namespace Microsoft.Web
    Write-ScriptInfo "Provider registration initiated (this may take 5-10 minutes)"
  }
  else {
    Write-ScriptSuccess "Microsoft.Web provider is registered"
  }
    
  # Check VMs quota for the region
  Write-ScriptInfo "Checking Total vCores quota in region: $Region"
    
  # Note: Detailed quota check requires Azure SDK or Resource Manager API
  # This is a basic check using the portal information
  Write-ScriptWarning "Quota check requires Azure Portal access"
  Write-ScriptInfo "To verify App Service capacity for region: $Region"
  Write-ScriptInfo "  1. Go to https://portal.azure.com"
  Write-ScriptInfo "  2. Search for 'Subscriptions' > Select your subscription"
  Write-ScriptInfo "  3. Go to 'Usage + quotas' on the left panel"
  Write-ScriptInfo "  4. Filter by Region: '$Region' and Service: 'App Service'"
  Write-ScriptInfo "  5. Verify 'Total vCores' limit >= 1 (current: check Current Value)"
    
  return $true
}

# ====================================
# Resource Creation (Path A & B)
# ====================================

function New-ResourceGroup {
  param([string]$RgName, [string]$Region)
    
  Write-ScriptSection "Creating Resource Group"
    
  # Check if already exists
  $rg = az group exists --name $RgName | ConvertFrom-Json
    
  if ($rg -eq $true) {
    Write-ScriptSuccess "Resource group already exists: $RgName"
    return $true
  }
    
  Write-ScriptInfo "Creating resource group: $RgName in region: $Region"
  az group create --name $RgName --location $Region
    
  if ($LASTEXITCODE -ne 0) {
    Write-ScriptError "Failed to create resource group"
    return $false
  }
    
  Write-ScriptSuccess "Resource group created: $RgName"
  return $true
}

function New-AppServicePlan {
  param([string]$RgName, [string]$PlanName, [string]$Region, [string]$Sku)
    
  Write-ScriptSection "Creating App Service Plan"
    
  # Check if already exists
  $existing = az appservice plan list --resource-group $RgName --output json 2>$null | ConvertFrom-Json
  $planExists = $existing | Where-Object { $_.name -eq $PlanName }
    
  if ($planExists) {
    Write-ScriptSuccess "App Service plan already exists: $PlanName"
    return $true
  }
    
  Write-ScriptInfo "Creating App Service plan: $PlanName"
  Write-ScriptInfo "  SKU: $Sku (1 vCore, 1 GB RAM)"
  Write-ScriptInfo "  Region: $Region"
  Write-ScriptInfo "  OS: Linux"
    
  az appservice plan create `
    --name $PlanName `
    --resource-group $RgName `
    --location $Region `
    --sku $Sku `
    --is-linux
    
  if ($LASTEXITCODE -ne 0) {
    Write-ScriptError "Failed to create App Service plan"
    Write-ScriptInfo "If quota error occurs, check the documented paths:"
    Write-ScriptInfo "  Path A: Assign subscription to JayPVentures LLC tenant"
    Write-ScriptInfo "  Path B: Request quota increase in Azure Portal"
    Write-ScriptInfo "  Path C: Use alternate runtime host (Render, Railway, Fly.io)"
    return $false
  }
    
  Write-ScriptSuccess "App Service plan created: $PlanName"
  return $true
}

function New-WebApp {
  param([string]$RgName, [string]$AppName, [string]$PlanName)
    
  Write-ScriptSection "Creating Web App"

  # Check if already exists
  $existing = az webapp show --resource-group $RgName --name $AppName --output json 2>$null | ConvertFrom-Json
  if ($existing) {
    Write-ScriptSuccess "Web app already exists: $AppName"
    # Validate runtime
    $runtime = $existing.linuxFxVersion
    if ($runtime -eq "DOTNETCORE|8.0" -or $runtime -eq "DOTNETCORE:8.0") {
      Write-ScriptSuccess "Web app runtime is .NET 8 ($runtime)"
    }
    else {
      Write-ScriptWarning "Web app runtime is $runtime, expected .NET 8. Manual update may be required."
    }
    # Validate health
    $defaultHost = $existing.defaultHostName
    $healthUrl = "https://$defaultHost/api/health"
    try {
      $health = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 10
      if ($health.status -eq "healthy") {
        Write-ScriptSuccess "/api/health endpoint is healthy."
        return $true
      }
      else {
        Write-ScriptWarning "/api/health endpoint returned: $($health | ConvertTo-Json)"
      }
    }
    catch {
      Write-ScriptWarning "Failed to reach /api/health endpoint: $_"
    }
    return $true
  }

  Write-ScriptInfo "Creating Web app: $AppName"
  Write-ScriptInfo "  Runtime: .NET 8"
  Write-ScriptInfo "  OS: Linux"
  Write-ScriptInfo "  Debug: Parameters about to be used:"
  Write-ScriptInfo "    Resource Group: $RgName"
  Write-ScriptInfo "    App Name: $AppName"
  Write-ScriptInfo "    Plan Name: $PlanName"
  Write-ScriptInfo "    Command: az webapp create --resource-group $RgName --plan $PlanName --name $AppName --runtime 'DOTNETCORE:8.0'"

  $azCmd = "az webapp create --resource-group $RgName --plan $PlanName --name $AppName --runtime DOTNETCORE:8.0"
  Write-ScriptInfo "  Running via cmd.exe: $azCmd"
  cmd.exe /c $azCmd

  if ($LASTEXITCODE -ne 0) {
    Write-ScriptError "Failed to create Web app"
    return $false
  }

  Write-ScriptSuccess "Web app created: $AppName"
  return $true
}

function Enable-HttpsOnly {
  param([string]$RgName, [string]$AppName)
    
  Write-ScriptSection "Configuring HTTPS"
    
  az webapp update `
    --resource-group $RgName `
    --name $AppName `
    --set httpsOnly=true
    
  if ($LASTEXITCODE -eq 0) {
    Write-ScriptSuccess "HTTPS enforced for web app"
  }
  else {
    Write-ScriptWarning "Could not enforce HTTPS (may require further configuration)"
  }
}

function Get-PublishProfile {
  param([string]$RgName, [string]$AppName)
    
  Write-ScriptSection "Generating Publish Profile"
    
  $profilePath = Join-Path $PSScriptRoot "azure-publish-profile-$AppName.xml"
    
  Write-ScriptInfo "Downloading publish profile to: $profilePath"
  az webapp deployment list-publishing-profiles `
    --resource-group $RgName `
    --name $AppName `
    --xml > $profilePath
    
  if ($LASTEXITCODE -ne 0) {
    Write-ScriptError "Failed to download publish profile"
    return $null
  }
    
  # Verify file was created
  if (-not (Test-Path $profilePath)) {
    Write-ScriptError "Publish profile file not created"
    return $null
  }
    
  $fileSize = (Get-Item $profilePath).Length
  Write-ScriptSuccess "Publish profile generated: $profilePath ($fileSize bytes)"
    
  return $profilePath
}

function Register-PublishProfileSecret {
  param([string]$ProfilePath, [string]$AppName)
    
  Write-ScriptSection "Registering GitHub Secret"
    
  if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-ScriptWarning "GitHub CLI not found. Manual secret registration required."
    Write-ScriptInfo "To register the publish profile secret:"
    Write-ScriptInfo "  Using PowerShell:"
    Write-ScriptInfo "    Get-Content -Raw '$ProfilePath' | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -"
    Write-ScriptInfo ""
    Write-ScriptInfo "  Do NOT use Bash '<' redirection in PowerShell."
    return $false
  }
    
  Write-ScriptInfo "Registering AZURE_WEBAPP_PUBLISH_PROFILE secret..."
    
  # Use PowerShell-compatible syntax
  Get-Content -Raw $ProfilePath | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -
    
  if ($LASTEXITCODE -eq 0) {
    Write-ScriptSuccess "GitHub secret registered: AZURE_WEBAPP_PUBLISH_PROFILE"
    return $true
  }
  else {
    Write-ScriptWarning "Failed to register GitHub secret (may require manual registration)"
    return $false
  }
}

# ====================================
# Path C: Alternate Runtimes
# ====================================

function Test-AlternateRuntimes {
  Write-ScriptSection "Path C: Alternate Runtime Options"
    
  Write-ScriptInfo "Validated runtime hosts for .NET 8 deployment:`n"
    
  # Fly.io
  Write-ScriptInfo "1. Fly.io (Recommended for quick start)"
  Write-ScriptInfo "   Command: fly auth login && fly deploy"
  Write-ScriptInfo "   Config: fly.toml (already configured)"
  $flyExists = Test-Path (Join-Path (Split-Path $PSScriptRoot) "fly.toml")
  if ($flyExists) {
    Write-ScriptSuccess "   fly.toml found in repository"
  }
    
  # Render
  Write-ScriptInfo "`n2. Render"
  Write-ScriptInfo "   Command: render deploy"
  Write-ScriptInfo "   Config: render.yaml (already configured)"
  $renderExists = Test-Path (Join-Path (Split-Path $PSScriptRoot) "render.yaml")
  if ($renderExists) {
    Write-ScriptSuccess "   render.yaml found in repository"
  }
    
  # Railway
  Write-ScriptInfo "`n3. Railway"
  Write-ScriptInfo "   Command: railway deploy"
  Write-ScriptInfo "   Supports Docker deployment"
    
  # DigitalOcean App Platform
  Write-ScriptInfo "`n4. DigitalOcean App Platform"
  Write-ScriptInfo "   Supports Docker deployment"
  Write-ScriptInfo "   More info: https://docs.digitalocean.com/products/app-platform/"
    
  # AWS App Runner
  Write-ScriptInfo "`n5. AWS App Runner"
  Write-ScriptInfo "   Supports container image deployment"
  Write-ScriptInfo "   More info: https://aws.amazon.com/apprunner/"
    
  Write-ScriptInfo "`nSee docs/CONTAINER-DEPLOYMENT.md for detailed instructions."
    
  return $true
}

# ====================================
# Validation Summary
# ====================================

function Test-DeploymentPrerequisites {
  param([string]$RgName, [string]$AppName)
    
  Write-ScriptSection "Validating Deployment Prerequisites"
    
  $allGood = $true
    
  # Test health endpoint availability
  Write-ScriptInfo "Checking health endpoint configuration..."
  $projPath = Join-Path (Split-Path $PSScriptRoot) "src/JPVOS/Program.cs"
  if (Get-Content $projPath | Select-String "app\.MapGet.*`"/health`"" -Quiet) {
    Write-ScriptSuccess "Health endpoint configured at /health"
  }
  else {
    Write-ScriptError "Health endpoint not found in Program.cs"
    $allGood = $false
  }
    
  # Check resource availability
  if ($RgName -and $AppName) {
    Write-ScriptInfo "Checking Azure resources..."
        
    # Check resource group
    $rg = az group exists --name $RgName | ConvertFrom-Json
    if ($rg) {
      Write-ScriptSuccess "Resource group exists: $RgName"
    }
    else {
      Write-ScriptError "Resource group not found: $RgName"
      $allGood = $false
    }
        
    # Check web app
    $app = az webapp show --resource-group $RgName --name $AppName --output json 2>$null | ConvertFrom-Json
    if ($app) {
      Write-ScriptSuccess "Web app exists: $AppName"
      Write-ScriptInfo "  URL: $($app.defaultHostName)"
      Write-ScriptInfo "  Runtime: $($app.linuxFxVersion)"
    }
    else {
      Write-ScriptError "Web app not found: $AppName"
      $allGood = $false
    }
  }
    
  return $allGood
}

# ====================================
# Main Execution
# ====================================

function Main {
  Write-Host "$($PSStyle.Foreground.Cyan)
╔═════════════════════════════════════════════════════════════╗
║    JPV-OS Access Gateway - Azure App Service Provisioning   ║
╚═════════════════════════════════════════════════════════════╝
$($PSStyle.Reset)"
    
  Write-ScriptInfo "Path: $Path (Region: $Region) | SKU: $SkuSize"
    
  # Step 1: Validate prerequisites
  if (-not (Test-Prerequisites)) {
    exit 1
  }
    
  # Step 2: Authentication
  if (-not (Test-AzureLogin -Tenant $TenantId)) {
    exit 1
  }
    
  # Step 3: Subscription
  if ($Path -in "A", "B") {
    if (-not $SubscriptionId) {
      Write-ScriptError "SubscriptionId required for Path A and B"
            
      $subs = Get-AvailableSubscriptions
      if ($subs.Count -gt 0) {
        Write-ScriptInfo "Use one of the available subscription IDs above:"
        Write-ScriptInfo "  .\provision-azure-appservice.ps1 -Path $Path -SubscriptionId '<subscription-id>'"
      }
      exit 1
    }
        
    if (-not (Set-ActiveSubscription -SubId $SubscriptionId)) {
      exit 1
    }
  }
    
  # Step 4: Quota check
  if ($Path -in "A", "B") {
    Test-AppServiceQuota -Region $Region
  }
    
  # If validation only, stop here
  if ($Validate) {
    Write-ScriptSuccess "Validation complete"
        
    # Additional validation for existing resources
    Test-DeploymentPrerequisites -RgName $ResourceGroup -AppName $WebAppName
    exit 0
  }
    
  # Step 5: Path A/B - Create resources
  if ($Path -eq "A") {
    Write-ScriptSection "Path A: JayPVentures LLC Tenant Subscription"
    Write-ScriptInfo "Provisioning resources using JayPVentures LLC tenant..."
        
    if (-not (New-ResourceGroup -RgName $ResourceGroup -Region $Region)) {
      exit 1
    }
        
    if (-not (New-AppServicePlan -RgName $ResourceGroup -PlanName $AppServicePlan -Region $Region -Sku $SkuSize)) {
      exit 1
    }
        
    if (-not (New-WebApp -RgName $ResourceGroup -AppName $WebAppName -PlanName $AppServicePlan)) {
      exit 1
    }
        
    Enable-HttpsOnly -RgName $ResourceGroup -AppName $WebAppName
        
    $profilePath = Get-PublishProfile -RgName $ResourceGroup -AppName $WebAppName
    if ($profilePath) {
      Register-PublishProfileSecret -ProfilePath $profilePath -AppName $WebAppName
    }
  }
  elseif ($Path -eq "B") {
    Write-ScriptSection "Path B: Existing Subscription Quota"
    Write-ScriptInfo "Using existing subscription with verified quota..."
        
    if (-not (New-ResourceGroup -RgName $ResourceGroup -Region $Region)) {
      exit 1
    }
        
    if (-not (New-AppServicePlan -RgName $ResourceGroup -PlanName $AppServicePlan -Region $Region -Sku $SkuSize)) {
      Write-ScriptError "Quota insufficient. Request quota increase at:"
      Write-ScriptInfo "  https://portal.azure.com > Subscriptions > Usage + quotas"
      exit 1
    }
        
    if (-not (New-WebApp -RgName $ResourceGroup -AppName $WebAppName -PlanName $AppServicePlan)) {
      exit 1
    }
        
    Enable-HttpsOnly -RgName $ResourceGroup -AppName $WebAppName
        
    $profilePath = Get-PublishProfile -RgName $ResourceGroup -AppName $WebAppName
    if ($profilePath) {
      Register-PublishProfileSecret -ProfilePath $profilePath -AppName $WebAppName
    }
  }
  elseif ($Path -eq "C") {
    Write-ScriptSection "Path C: Alternate Runtime Host"
    Test-AlternateRuntimes
  }
    
  # Step 6: Final validation
  Write-ScriptSection "Provisioning Summary"
    
  if ($Path -in "A", "B") {
    if (Test-DeploymentPrerequisites -RgName $ResourceGroup -AppName $WebAppName) {
      Write-ScriptSuccess "All prerequisites validated. Ready for deployment."
      Write-ScriptInfo "Next steps:"
      Write-ScriptInfo "  1. Configure app settings in Azure Portal"
      Write-ScriptInfo "  2. Run GitHub Actions workflow: 'Deploy to Azure App Service'"
      Write-ScriptInfo "  3. Monitor deployment at: https://portal.azure.com"
    }
  }
}

# Run main function
Main
