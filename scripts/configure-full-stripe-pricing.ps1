param(
    [ValidateSet("test","live")]
    [string]$Mode = "test",

    [switch]$NonInteractive,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path ".").Path
$GeneratedDir = Join-Path $RepoRoot "infrastructure\stripe\generated"
New-Item -ItemType Directory -Force -Path $GeneratedDir | Out-Null

$PricingAuthority = "JPV-OS-v2.1.0"
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$JsonPath = Join-Path $GeneratedDir "stripe-pricing.$Mode.json"
$ReportPath = Join-Path $GeneratedDir "stripe-pricing.$Mode.$Stamp.md"
$EnvTemplatePath = Join-Path $GeneratedDir "stripe-env.$Mode.template"

$StripeCmd = Join-Path $RepoRoot "stripe.exe"
if (!(Test-Path $StripeCmd)) {
    throw "Local stripe.exe not found at $StripeCmd"
}

function Invoke-StripeJson {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$CommandArgs
    )

    $raw = & $StripeCmd @CommandArgs 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($raw -join "`n").Trim()

    if ($exitCode -ne 0) {
        throw "Stripe CLI failed with exit code $exitCode`: $text"
    }

    if (-not ($text.StartsWith("{") -or $text.StartsWith("["))) {
        throw "Stripe CLI did not return JSON: $text"
    }

    return $text | ConvertFrom-Json
}

$Tiers = @(
    @{ key="member_access_monthly"; name="Member Access"; amount=2000; interval="month" },
    @{ key="member_access_annual"; name="Member Access Annual"; amount=20000; interval="year" },
    @{ key="creator_infrastructure_monthly"; name="Creator Infrastructure"; amount=50000; interval="month" },
    @{ key="creator_infrastructure_annual"; name="Creator Infrastructure Annual"; amount=500000; interval="year" },
    @{ key="partner_infrastructure_monthly"; name="Partner Infrastructure"; amount=250000; interval="month" },
    @{ key="partner_infrastructure_annual"; name="Partner Infrastructure Annual"; amount=2500000; interval="year" },
    @{ key="enterprise_infrastructure_monthly"; name="Enterprise Infrastructure"; amount=1000000; interval="month" },
    @{ key="enterprise_infrastructure_annual"; name="Enterprise Infrastructure Annual"; amount=10000000; interval="year" }
)

$Results = [ordered]@{}

"# Stripe Pricing Report`nMode: $Mode`nCanonical pricing: $PricingAuthority`nGenerated: $(Get-Date)`nStripe CLI: $StripeCmd`n" |
    Set-Content $ReportPath -Encoding UTF8

foreach ($tier in $Tiers) {
    $lookup = $tier.key
    "`n## $lookup" | Add-Content $ReportPath

    $existing = Invoke-StripeJson -CommandArgs @(
        "get",
        "/v1/prices",
        "-d",
        "lookup_keys[]=$lookup",
        "-d",
        "active=true",
        "-d",
        "limit=1"
    )

    $price = $null
    $productId = $null

    if ($existing.data.Count -gt 0) {
        $candidate = $existing.data[0]
        $candidateAuthority = if ($candidate.metadata -and $candidate.metadata.pricing_authority) { [string]$candidate.metadata.pricing_authority } else { "" }
        $amountMatches = ([int64]$candidate.unit_amount -eq [int64]$tier.amount)
        $intervalMatches = ($candidate.recurring.interval -eq $tier.interval)
        $currencyMatches = ([string]$candidate.currency -eq "usd")
        $authorityMatches = ($candidateAuthority -eq $PricingAuthority)

        if ($amountMatches -and $intervalMatches -and $currencyMatches -and $authorityMatches) {
            $price = $candidate
            $productId = $candidate.product
            "Reused verified canonical price: $($price.id)" | Add-Content $ReportPath
        } else {
            "Detected divergent lookup-key price: $($candidate.id) amount=$($candidate.unit_amount) interval=$($candidate.recurring.interval) currency=$($candidate.currency) authority=$candidateAuthority" | Add-Content $ReportPath

            $productId = $candidate.product
            $price = Invoke-StripeJson -CommandArgs @(
                "post",
                "/v1/prices",
                "-d",
                "product=$productId",
                "-d",
                "currency=usd",
                "-d",
                "unit_amount=$($tier.amount)",
                "-d",
                "recurring[interval]=$($tier.interval)",
                "-d",
                "lookup_key=$lookup",
                "-d",
                "transfer_lookup_key=true",
                "-d",
                "tax_behavior=exclusive",
                "-d",
                "metadata[ecosystem]=JPV-OS",
                "-d",
                "metadata[legal_entity]=JayPVentures LLC",
                "-d",
                "metadata[pricing_authority]=$PricingAuthority",
                "-d",
                "metadata[mode]=$Mode"
            )

            Invoke-StripeJson -CommandArgs @(
                "post",
                "/v1/prices/$($candidate.id)",
                "-d",
                "active=false"
            ) | Out-Null

            "Replaced divergent price with canonical price: $($price.id)" | Add-Content $ReportPath
        }
    } else {
        $product = Invoke-StripeJson -CommandArgs @(
            "post",
            "/v1/products",
            "-d",
            "name=$($tier.name)",
            "-d",
            "description=JPV-OS Access Gateway tier: $($tier.name)",
            "-d",
            "metadata[ecosystem]=JPV-OS",
            "-d",
            "metadata[legal_entity]=JayPVentures LLC",
            "-d",
            "metadata[pricing_authority]=$PricingAuthority",
            "-d",
            "metadata[mode]=$Mode"
        )

        $price = Invoke-StripeJson -CommandArgs @(
            "post",
            "/v1/prices",
            "-d",
            "product=$($product.id)",
            "-d",
            "currency=usd",
            "-d",
            "unit_amount=$($tier.amount)",
            "-d",
            "recurring[interval]=$($tier.interval)",
            "-d",
            "lookup_key=$lookup",
            "-d",
            "tax_behavior=exclusive",
            "-d",
            "metadata[ecosystem]=JPV-OS",
            "-d",
            "metadata[legal_entity]=JayPVentures LLC",
            "-d",
            "metadata[pricing_authority]=$PricingAuthority",
            "-d",
            "metadata[mode]=$Mode"
        )

        $productId = $product.id
        "Created canonical product: $productId" | Add-Content $ReportPath
        "Created canonical price: $($price.id)" | Add-Content $ReportPath
    }

    $Results[$lookup] = [ordered]@{
        name = $tier.name
        amount = $tier.amount
        currency = "usd"
        interval = $tier.interval
        product_id = $productId
        price_id = $price.id
        lookup_key = $lookup
        pricing_authority = $PricingAuthority
    }
}

$Output = [ordered]@{
    mode = $Mode
    pricing_authority = $PricingAuthority
    generated = (Get-Date).ToString("o")
    stripe_cli = $StripeCmd
    prices = $Results
}

$Output | ConvertTo-Json -Depth 10 | Set-Content $JsonPath -Encoding UTF8

$envLines = @(
    "# Stripe $Mode environment template",
    "# Canonical pricing authority: $PricingAuthority",
    "# Generated $(Get-Date)",
    "STRIPE_MODE=$Mode",
    "JPV_PRICING_AUTHORITY=$PricingAuthority"
)

foreach ($k in $Results.Keys) {
    $envName = "STRIPE_PRICE_" + $k.ToUpper()
    $envLines += "$envName=$($Results[$k].price_id)"
}

$envLines | Set-Content $EnvTemplatePath -Encoding UTF8

Write-Host "======================================"
Write-Host "STRIPE CONFIG COMPLETE"
Write-Host "Mode: $Mode"
Write-Host "Authority: $PricingAuthority"
Write-Host "JSON: $JsonPath"
Write-Host "Report: $ReportPath"
Write-Host "Env template: $EnvTemplatePath"
Write-Host "======================================"
exit 0
