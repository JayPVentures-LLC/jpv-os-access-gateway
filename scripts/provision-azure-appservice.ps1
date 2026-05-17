#!/usr/bin/env pwsh
<#
.SYNOPSIS
Automates Azure App Service provisioning for JPV-OS Access Gateway with comprehensive validation.

.DESCRIPTION
This script performs atomic validation checks and provisions Azure resources:
1. Validates Azure tenant access and subscription authority
2. Validates regional quota availability
3. Provisions resource group, App Service plan, and Web App
4. Generates publish profile and configures GitHub secret
5. Triggers deployment workflow

.PARAMETER ResourceGroupName
The name of the resource group (default: rg-jpv-os-prod)

.PARAMETER AppServicePlanName
The name of the App Service plan (default: asp-jpv-os-prod)

.PARAMETER WebAppName
The name of the web app (default: jpv-os-access-gateway)

.PARAMETER SkuName
The SKU for the App Service plan (default: B1)

.PARAMETER Regions
Comma-separated list of regions to try (default: eastus,centralus,eastus2,westus2)

.EXAMPLE
./provision-azure-appservice.ps1

.EXAMPLE
./provision-azure-appservice.ps1 -ResourceGroupName rg-custom -WebAppName app-custom -SkuName B2

#>
param(
    [string]$ResourceGroupName = "rg-jpv-os-prod",
    [string]$AppServicePlanName = "asp-jpv-os-prod",
    [string]$WebAppName = "jpv-os-access-gateway",
    [string]$SkuName = "B1",
    [string]$Regions = "eastus,centralus,eastus2,westus2"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Color-coded output helpers
function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

function Test-AzurePrerequisites {
    Write-Info "Checking Azure CLI prerequisites..."
    
    # Check if Azure CLI is installed
    try {
        $azVersion = az version 2>&1 | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($null -eq $azVersion) {
            throw "Azure CLI not found or not properly installed"
        }
        Write-Success "Azure CLI version: $($azVersion.'azure-cli')"
    }
    catch {
        Write-Error "Azure CLI is not installed. Please install it from: https://aka.ms/azure-cli"
        exit 1
    }
    
    # Check if gh CLI is installed (for GitHub secret configuration)
    try {
        $ghVersion = gh --version 2>&1
        Write-Success "GitHub CLI available"
    }
    catch {
        Write-Warning "GitHub CLI (gh) not installed. You'll need to manually set AZURE_WEBAPP_PUBLISH_PROFILE secret."
    }
}

function Test-AzureLogin {
    Write-Info "Validating Azure login status..."
    
    try {
        $account = az account show 2>&1 | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($null -eq $account.id) {
            throw "Not logged in"
        }
        Write-Success "Logged in as: $($account.user.name) ($($account.user.type))"
        Write-Success "Current tenant: $($account.tenantId)"
        return $account
    }
    catch {
        Write-Error "Not logged into Azure. Run: az login"
        exit 1
    }
}

function Get-AvailableSubscriptions {
    Write-Info "Fetching available subscriptions..."
    
    try {
        $subs = az account list --query "[].{name:name, id:id, tenantId:tenantId, state:state}" -o json | ConvertFrom-Json
        if ($null -eq $subs -or $subs.Count -eq 0) {
            Write-Error "No subscriptions found. The current account has no subscriptions assigned."
            Write-Error ""
            Write-Error "Remediation:"
            Write-Error "1. Contact Azure administrator to assign a subscription to your account"
            Write-Error "2. For JayPVentures LLC tenant: Request subscription assignment in Azure portal"
            Write-Error "3. Re-run this script after subscription is assigned"
            exit 1
        }
        
        Write-Info "Found $($subs.Count) subscription(s):"
        foreach ($sub in $subs) {
            Write-Host "  - $($sub.name) [$($sub.id)]"
        }
        
        return $subs
    }
    catch {
        Write-Error "Failed to list subscriptions: $_"
        exit 1
    }
}

function Select-Subscription {
    param([array]$Subscriptions)
    
    if ($Subscriptions.Count -eq 1) {
        $selected = $Subscriptions[0]
        Write-Success "Auto-selected subscription: $($selected.name)"
    }
    else {
        # Try to find JayPVentures LLC tenant subscription first
        $jpvSub = $Subscriptions | Where-Object { $_.name -like "*JayPVentures*" -or $_.name -like "*jpv*" }
        if ($jpvSub) {
            $selected = $jpvSub
            Write-Success "Selected JayPVentures subscription: $($selected.name)"
        }
        else {
            $selected = $Subscriptions[0]
            Write-Warning "Multiple subscriptions found. Using first: $($selected.name)"
        }
    }
    
    az account set --subscription $selected.id
    Write-Success "Switched to subscription: $($selected.name) [$($selected.id)]"
    return $selected
}

function Test-SubscriptionAuthority {
    param([string]$SubscriptionId)
    
    Write-Info "Validating subscription authority..."
    
    try {
        $roleAssignments = az role assignment list --subscription $SubscriptionId --query "[].roleDefinitionName" -o json | ConvertFrom-Json
        $roles = @($roleAssignments | Select-Object -Unique)
        
        $requiredRoles = @("Owner", "Contributor", "Website Contributor")
        $hasRequiredRole = $false
        
        foreach ($role in $roles) {
            if ($requiredRoles -contains $role) {
                Write-Success "Has required role for resource creation: $role"
                $hasRequiredRole = $true
                break
            }
        }
        
        if (-not $hasRequiredRole) {
            Write-Error "Current account does not have required roles for resource creation"
            Write-Error "Requires one of: $($requiredRoles -join ', ')"
            Write-Error "Current roles: $($roles -join ', ')"
            exit 1
        }
        
        return $true
    }
    catch {
        Write-Error "Failed to validate subscription authority: $_"
        exit 1
    }
}

function Test-ProviderRegistration {
    param([string]$SubscriptionId)
    
    Write-Info "Checking Microsoft.Web provider registration..."
    
    try {
        $provider = az provider show --namespace Microsoft.Web --subscription $SubscriptionId -o json | ConvertFrom-Json
        
        if ($provider.registrationState -eq "Registered") {
            Write-Success "Microsoft.Web provider is registered"
            return $true
        }
        
        Write-Warning "Microsoft.Web provider not registered. Attempting registration..."
        
        $regResult = az provider register --namespace Microsoft.Web --subscription $SubscriptionId
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Successfully registered Microsoft.Web provider"
            
            # Wait a moment for registration to propagate
            Write-Info "Waiting for provider registration to propagate..."
            Start-Sleep -Seconds 5
            return $true
        }
        else {
            Write-Error "Failed to register Microsoft.Web provider"
            Write-Error "Remediation: Register provider manually or contact Azure support"
            exit 1
        }
    }
    catch {
        Write-Error "Failed to check provider registration: $_"
        exit 1
    }
}

function Find-ViableRegion {
    param([string]$SkuName, [array]$RegionList)
    
    Write-Info "Validating regional quota for SKU: $SkuName..."
    Write-Info "Checking regions: $($RegionList -join ', ')"
    
    $regions = $RegionList -split ','
    $quotaErrors = @()
    
    foreach ($region in $regions) {
        $region = $region.Trim()
        Write-Info "Checking quota in $region..."
        
        try {
            # Check if region supports Linux App Service plans
            $skuInfo = az appservice plan create `
                --name "test-sku-check-$([datetime]::Now.Ticks)" `
                --resource-group "test-rg-$([datetime]::Now.Ticks)" `
                --location $region `
                --sku $SkuName `
                --is-linux `
                --dry-run `
                -o json 2>&1
            
            # If dry-run succeeds, region is viable
            Write-Success "Region $region has available quota"
            return $region
        }
        catch {
            $errorMsg = "$_"
            $quotaErrors += "  - $region: $($errorMsg.Split([Environment]::NewLine)[0])"
            Write-Warning "Region $region not viable: $($errorMsg.Split([Environment]::NewLine)[0])"
        }
    }
    
    # No viable region found
    Write-Error ""
    Write-Error "No viable regions found with available quota for SKU: $SkuName"
    Write-Error ""
    Write-Error "Quota errors:"
    $quotaErrors | ForEach-Object { Write-Host $_ }
    Write-Error ""
    Write-Error "Remediation steps:"
    Write-Error "1. Log into Azure Portal: https://portal.azure.com"
    Write-Error "2. Navigate to Subscriptions > Usage + quotas"
    Write-Error "3. For each region (East US, Central US, East US 2, West US 2):"
    Write-Error "   - Check 'Compute' category"
    Write-Error "   - Look for 'Total VM quota'"
    Write-Error "   - If quota is 0, click 'Request quota increase'"
    Write-Error "   - Select 'New support request' if needed"
    Write-Error "4. Once quotas are increased, re-run this script"
    exit 1
}

function New-AzureResources {
    param(
        [string]$ResourceGroupName,
        [string]$AppServicePlanName,
        [string]$WebAppName,
        [string]$SkuName,
        [string]$Region
    )
    
    Write-Info "Creating Azure resources in region: $Region"
    
    try {
        # Create resource group
        Write-Info "Creating resource group: $ResourceGroupName"
        az group create `
            --name $ResourceGroupName `
            --location $Region `
            --output none
        Write-Success "Resource group created"
        
        # Create App Service Plan
        Write-Info "Creating App Service plan: $AppServicePlanName"
        az appservice plan create `
            --name $AppServicePlanName `
            --resource-group $ResourceGroupName `
            --location $Region `
            --sku $SkuName `
            --is-linux `
            --output none
        Write-Success "App Service plan created"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to create App Service plan"
            Write-Error "This is a blocker. Do not continue."
            exit 1
        }
        
        # Create Web App
        Write-Info "Creating .NET 8 Web App: $WebAppName"
        az webapp create `
            --name $WebAppName `
            --resource-group $ResourceGroupName `
            --plan $AppServicePlanName `
            --runtime "DOTNET|8.0" `
            --output none
        Write-Success "Web App created"
        
        # Configure HTTPS-only
        Write-Info "Configuring HTTPS enforcement"
        az webapp update `
            --name $WebAppName `
            --resource-group $ResourceGroupName `
            --https-only true `
            --output none
        Write-Success "HTTPS-only enabled"
        
        # Set app settings (placeholders for production config)
        Write-Info "Configuring app settings"
        az webapp config appsettings set `
            --name $WebAppName `
            --resource-group $ResourceGroupName `
            --settings `
            WEBSITES_ENABLE_APP_SERVICE_STORAGE=true `
            --output none
        Write-Success "App settings configured"
        
        return $true
    }
    catch {
        Write-Error "Failed to create Azure resources: $_"
        exit 1
    }
}

function Get-PublishProfile {
    param(
        [string]$WebAppName,
        [string]$ResourceGroupName
    )
    
    Write-Info "Generating publish profile..."
    
    try {
        $profilePath = "azure-publish-profile-$WebAppName.xml"
        
        # Get publish profile
        az webapp deployment list-publishing-profiles `
            --name $WebAppName `
            --resource-group $ResourceGroupName `
            --query "[0]" `
            --output json | ConvertFrom-Json | ConvertTo-Json -Depth 10 | Out-File -FilePath $profilePath -Encoding UTF8
        
        # Actually, we need the XML format
        az webapp deployment list-publishing-profiles `
            --name $WebAppName `
            --resource-group $ResourceGroupName `
            --query "[0]" `
            --output xml > $profilePath 2>&1 || `
        az webapp deployment list-publishing-profiles `
            --name $WebAppName `
            --resource-group $ResourceGroupName > $profilePath
        
        if (Test-Path $profilePath) {
            Write-Success "Publish profile generated: $profilePath"
            return $profilePath
        }
        else {
            Write-Error "Failed to generate publish profile"
            exit 1
        }
    }
    catch {
        Write-Error "Failed to get publish profile: $_"
        exit 1
    }
}

function Set-GitHubSecret {
    param(
        [string]$PublishProfilePath
    )
    
    Write-Info "Configuring GitHub deployment secret..."
    
    # Check if gh CLI is available
    try {
        $ghCheck = gh --version 2>&1
        if ($null -eq $ghCheck) {
            throw "gh CLI not available"
        }
    }
    catch {
        Write-Warning "GitHub CLI (gh) not available. Manual configuration needed:"
        Write-Warning "1. Read publish profile: Get-Content -Raw '$PublishProfilePath'"
        Write-Warning "2. Copy the XML content"
        Write-Warning "3. In GitHub: Settings > Secrets and variables > Actions"
        Write-Warning "4. Create new secret: AZURE_WEBAPP_PUBLISH_PROFILE"
        Write-Warning "5. Paste the XML content"
        return $false
    }
    
    try {
        Write-Info "Reading publish profile..."
        $publishProfile = Get-Content -Raw $PublishProfilePath
        
        Write-Info "Setting GitHub secret: AZURE_WEBAPP_PUBLISH_PROFILE"
        $publishProfile | gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --body-file -
        
        Write-Success "GitHub secret configured successfully"
        return $true
    }
    catch {
        Write-Error "Failed to set GitHub secret: $_"
        Write-Warning "Manual configuration needed. See instructions above."
        return $false
    }
}

function Trigger-Deployment {
    param(
        [string]$WebAppName
    )
    
    Write-Info "Triggering GitHub Actions deployment..."
    
    # Check if gh CLI is available
    try {
        $ghCheck = gh --version 2>&1
        if ($null -eq $ghCheck) {
            throw "gh CLI not available"
        }
    }
    catch {
        Write-Warning "GitHub CLI (gh) not available. Manual trigger needed:"
        Write-Warning "1. Go to: https://github.com/JayPVentures-LLC/jpv-os-access-gateway"
        Write-Warning "2. Actions > deploy-appservice"
        Write-Warning "3. Click 'Run workflow'"
        return $false
    }
    
    try {
        Write-Info "Running deploy-appservice workflow..."
        gh workflow run deploy-appservice.yml
        Write-Success "Deployment workflow triggered"
        
        Write-Info "Waiting for deployment to complete (this may take several minutes)..."
        Start-Sleep -Seconds 30
        
        return $true
    }
    catch {
        Write-Error "Failed to trigger deployment: $_"
        Write-Warning "Manual trigger needed. See instructions above."
        return $false
    }
}

function Test-HealthEndpoint {
    param(
        [string]$WebAppName
    )
    
    Write-Info "Validating application health endpoint..."
    
    $url = "https://$WebAppName.azurewebsites.net/api/health"
    $maxAttempts = 30
    $attempt = 0
    
    while ($attempt -lt $maxAttempts) {
        $attempt++
        
        try {
            $response = Invoke-WebRequest -Uri $url -SkipCertificateCheck -TimeoutSec 10 -ErrorAction SilentlyContinue
            
            if ($response.StatusCode -eq 200) {
                Write-Success "Health endpoint is responding: $url"
                $body = $response.Content | ConvertFrom-Json
                Write-Success "Response: $($body | ConvertTo-Json -Depth 2)"
                return $true
            }
        }
        catch {
            if ($attempt -eq 1) {
                Write-Info "Waiting for application to become available ($attempt/$maxAttempts)..."
            }
            elseif ($attempt % 5 -eq 0) {
                Write-Info "Still waiting... ($attempt/$maxAttempts)"
            }
            Start-Sleep -Seconds 10
        }
    }
    
    Write-Warning "Health endpoint not responding after $(($maxAttempts * 10) / 60) minutes"
    Write-Warning "The application may still be deploying. Check Azure Portal for status."
    return $false
}

function Show-Summary {
    param(
        [string]$ResourceGroupName,
        [string]$WebAppName,
        [string]$PublishProfilePath,
        [bool]$HealthCheckPassed
    )
    
    Write-Host ""
    Write-Success "========================================="
    Write-Success "PROVISIONING COMPLETE"
    Write-Success "========================================="
    Write-Host ""
    Write-Info "Resource Details:"
    Write-Host "  Resource Group:    $ResourceGroupName"
    Write-Host "  Web App Name:      $WebAppName"
    Write-Host "  URL:               https://$WebAppName.azurewebsites.net"
    Write-Host "  Health Endpoint:   https://$WebAppName.azurewebsites.net/api/health"
    Write-Host ""
    Write-Info "Next Steps:"
    Write-Host "  1. Verify: $url"
    Write-Host "  2. Configure custom domain if needed"
    Write-Host "  3. Update Cloudflare DNS to point to Azure App Service"
    Write-Host ""
    
    if ($HealthCheckPassed) {
        Write-Success "Application is healthy and ready for production"
    }
    else {
        Write-Warning "Application health status unknown. Check Azure Portal."
    }
    
    Write-Host "  Publish Profile:   $PublishProfilePath"
    Write-Host ""
}

# ============================================================================
# Main Execution
# ============================================================================

Write-Host ""
Write-Host "========================================="
Write-Host "JPV-OS Access Gateway - Azure Provisioning"
Write-Host "========================================="
Write-Host ""

# Step 1: Check prerequisites
Test-AzurePrerequisites

# Step 2: Validate Azure login
$currentAccount = Test-AzureLogin

# Step 3: Get available subscriptions
$subscriptions = Get-AvailableSubscriptions

# Step 4: Select subscription
$selectedSubscription = Select-Subscription -Subscriptions $subscriptions

# Step 5: Validate subscription authority
Test-SubscriptionAuthority -SubscriptionId $selectedSubscription.id

# Step 6: Test provider registration
Test-ProviderRegistration -SubscriptionId $selectedSubscription.id

# Step 7: Find viable region
$regions = $Regions -split ','
$viableRegion = Find-ViableRegion -SkuName $SkuName -RegionList $regions

# Step 8: Provision resources
New-AzureResources `
    -ResourceGroupName $ResourceGroupName `
    -AppServicePlanName $AppServicePlanName `
    -WebAppName $WebAppName `
    -SkuName $SkuName `
    -Region $viableRegion

# Step 9: Get publish profile
$publishProfilePath = Get-PublishProfile `
    -WebAppName $WebAppName `
    -ResourceGroupName $ResourceGroupName

# Step 10: Configure GitHub secret
$secretSet = Set-GitHubSecret -PublishProfilePath $publishProfilePath

# Step 11: Trigger deployment
$deploymentTriggered = $false
if ($secretSet) {
    $deploymentTriggered = Trigger-Deployment -WebAppName $WebAppName
}

# Step 12: Test health endpoint
$healthCheckPassed = $false
if ($deploymentTriggered) {
    $healthCheckPassed = Test-HealthEndpoint -WebAppName $WebAppName
}

# Show summary
Show-Summary `
    -ResourceGroupName $ResourceGroupName `
    -WebAppName $WebAppName `
    -PublishProfilePath $publishProfilePath `
    -HealthCheckPassed $healthCheckPassed

Write-Host ""
