$ErrorActionPreference = "Stop"

$ResourceGroup = "rg-jpv-os-access-gateway"
$WebApp = "jpv-os-access-gateway"

Write-Host ""
Write-Host "======================================"
Write-Host "STRIPE FULL PRICING CONFIGURATION"
Write-Host "======================================"

# List all required Stripe price config names
$SettingNames = @(
  "STRIPE_PRICE_MEMBER_MONTHLY",
  "STRIPE_PRICE_MEMBER_ANNUAL",
  "STRIPE_PRICE_VIP_MONTHLY",
  "STRIPE_PRICE_VIP_ANNUAL",
  "STRIPE_PRICE_CREATOR_LANE_MONTHLY",
  "STRIPE_PRICE_OPERATOR_MONTHLY",
  "STRIPE_PRICE_ENTERPRISE_MONTHLY",
  # Compatibility/legacy
  "STRIPE_PRICE_ID_COMMUNITY",
  "STRIPE_PRICE_ID_VIP",
  "STRIPE_PRICE_ENTERPRISE_ANNUAL",
  "STRIPE_PRICE_CUSTOM_IMPLEMENTATION"
)

Write-Host ""
Write-Host "Detected Stripe config names:"
$SettingNames | ForEach-Object { Write-Host " - $_" }

Write-Host ""
Write-Host "[1] Prompting for Stripe TEST price IDs..."

$Settings = @{}
foreach ($Name in $SettingNames) {
  do {
    $Value = Read-Host "Enter TEST Stripe price ID for $Name (or leave blank to skip)"
    if ($Value -and $Value -notmatch "^price_") {
      Write-Host "Invalid. Stripe price IDs must start with 'price_'"
    }
  } until (-not $Value -or $Value -match "^price_")
  if ($Value) { $Settings[$Name] = $Value }
}

Write-Host ""
Write-Host "[2] Set PUBLIC_APP_BASE_URL (default: https://jpv-os-access-gateway.azurewebsites.net)"
$BaseUrl = Read-Host "Enter PUBLIC_APP_BASE_URL (or leave blank for default)"
if (-not $BaseUrl) { $BaseUrl = "https://jpv-os-access-gateway.azurewebsites.net" }

Write-Host "[3] Enable automatic tax? (y/N)"
$AutoTax = Read-Host "Enable STRIPE_AUTOMATIC_TAX_ENABLED? (y/N)"
$AutoTaxValue = if ($AutoTax -eq "y" -or $AutoTax -eq "Y") { "true" } else { "false" }

Write-Host ""
Write-Host "[4] Building Azure appsettings payload..."
$Args = @()
foreach ($Pair in $Settings.GetEnumerator()) {
  $Args += "$($Pair.Key)=$($Pair.Value)"
}
$Args += "PUBLIC_APP_BASE_URL=$BaseUrl"
$Args += "STRIPE_AUTOMATIC_TAX_ENABLED=$AutoTaxValue"

Write-Host ""
Write-Host "[5] Applying Azure settings..."
az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $WebApp `
  --settings $Args

Write-Host ""
Write-Host "[6] Restarting Azure Web App..."
az webapp restart `
  --resource-group $ResourceGroup `
  --name $WebApp

Write-Host ""
Write-Host "======================================"
Write-Host "STRIPE CONFIG COMPLETE"
Write-Host "======================================"
