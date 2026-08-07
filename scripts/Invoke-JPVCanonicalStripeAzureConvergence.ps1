#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('test','live')]
    [string]$Mode = 'live',
    [string]$ResourceGroup = 'rg-jpv-os-access-gateway',
    [string]$WebAppName = 'jpv-os-access-gateway',
    [string]$BaseUrl = 'https://jpv-os-access-gateway.azurewebsites.net'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$PricingAuthority = 'JPV-OS-v2.1.0'
$Provisioner = Join-Path $PSScriptRoot 'configure-full-stripe-pricing.ps1'
$GeneratedMap = Join-Path $RepoRoot "infrastructure\stripe\generated\stripe-pricing.$Mode.json"
$ReceiptDirectory = Join-Path $RepoRoot 'artifacts\commercial\stripe'
$ReceiptPath = Join-Path $ReceiptDirectory "canonical-stripe-$Mode-convergence.json"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Required command not available: az'
}

if (-not (Test-Path -LiteralPath $Provisioner -PathType Leaf)) {
    throw "Canonical Stripe provisioner missing: $Provisioner"
}

Push-Location $RepoRoot
try {
    & pwsh -NoProfile -ExecutionPolicy Bypass -File $Provisioner -Mode $Mode -NonInteractive
    if ($LASTEXITCODE -ne 0) {
        throw "Canonical Stripe provisioning failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $GeneratedMap -PathType Leaf)) {
    throw "Canonical generated Stripe map missing after provisioning: $GeneratedMap"
}

$map = Get-Content -LiteralPath $GeneratedMap -Raw | ConvertFrom-Json -Depth 50
if ($map.pricing_authority -ne $PricingAuthority) {
    throw "Generated map authority mismatch. Expected $PricingAuthority; found $($map.pricing_authority)."
}

$expected = [ordered]@{
    member_access_monthly = @{ amount = 2000; interval = 'month' }
    member_access_annual = @{ amount = 20000; interval = 'year' }
    creator_infrastructure_monthly = @{ amount = 50000; interval = 'month' }
    creator_infrastructure_annual = @{ amount = 500000; interval = 'year' }
    partner_infrastructure_monthly = @{ amount = 250000; interval = 'month' }
    partner_infrastructure_annual = @{ amount = 2500000; interval = 'year' }
    enterprise_infrastructure_monthly = @{ amount = 1000000; interval = 'month' }
    enterprise_infrastructure_annual = @{ amount = 10000000; interval = 'year' }
}

$settings = [System.Collections.Generic.List[string]]::new()
$settings.Add("STRIPE_MODE=$Mode")
$settings.Add("JPV_PRICING_AUTHORITY=$PricingAuthority")

foreach ($lookupKey in $expected.Keys) {
    $entry = $map.prices.$lookupKey
    if ($null -eq $entry) {
        throw "Generated map missing canonical lookup key: $lookupKey"
    }
    if ([int64]$entry.amount -ne [int64]$expected[$lookupKey].amount) {
        throw "Generated amount mismatch for $lookupKey"
    }
    if ([string]$entry.interval -ne [string]$expected[$lookupKey].interval) {
        throw "Generated interval mismatch for $lookupKey"
    }
    if ([string]$entry.currency -ne 'usd') {
        throw "Generated currency mismatch for $lookupKey"
    }
    if ([string]$entry.pricing_authority -ne $PricingAuthority) {
        throw "Generated price entry lacks canonical authority: $lookupKey"
    }
    if ([string]$entry.price_id -notmatch '^price_[A-Za-z0-9_]+$') {
        throw "Generated Stripe price ID is invalid for $lookupKey"
    }

    $environmentName = 'STRIPE_PRICE_' + $lookupKey.ToUpperInvariant()
    $settings.Add("$environmentName=$($entry.price_id)")
}

az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --settings @($settings) `
    --output none
if ($LASTEXITCODE -ne 0) {
    throw "Azure canonical Stripe setting convergence failed with exit code $LASTEXITCODE"
}

az webapp restart `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --output none
if ($LASTEXITCODE -ne 0) {
    throw "Azure restart failed with exit code $LASTEXITCODE"
}

Start-Sleep -Seconds 10
$health = Invoke-WebRequest "$BaseUrl/health" -UseBasicParsing -ErrorAction Stop
if ($health.StatusCode -ne 200) {
    throw "Post-convergence health check failed with HTTP $($health.StatusCode)"
}

$checkoutStatus = Invoke-RestMethod "$BaseUrl/api/checkout/status" -ErrorAction Stop
if (-not $checkoutStatus.environmentHealthy -or -not $checkoutStatus.pricingAuthorityHealthy) {
    throw 'Post-convergence checkout pricing health verification failed.'
}
if ([string]$checkoutStatus.canonicalPricingAuthority -ne $PricingAuthority) {
    throw "Runtime canonical pricing authority mismatch: $($checkoutStatus.canonicalPricingAuthority)"
}

New-Item -ItemType Directory -Path $ReceiptDirectory -Force | Out-Null
$receipt = [ordered]@{
    schema_version = 'jpv.canonical-stripe-convergence.v1'
    pricing_authority = $PricingAuthority
    mode = $Mode
    resource_group = $ResourceGroup
    web_app = $WebAppName
    canonical_lookup_keys = @($expected.Keys)
    configured_setting_names = @('STRIPE_MODE','JPV_PRICING_AUTHORITY') + @($expected.Keys | ForEach-Object { 'STRIPE_PRICE_' + $_.ToUpperInvariant() })
    health_url = "$BaseUrl/health"
    health_status = [int]$health.StatusCode
    checkout_status_url = "$BaseUrl/api/checkout/status"
    checkout_pricing_authority_healthy = [bool]$checkoutStatus.pricingAuthorityHealthy
    state = 'VERIFIED_COMPLETE'
    verified_at = [DateTimeOffset]::UtcNow.ToString('o')
}
$receipt | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt | ConvertTo-Json -Depth 30
