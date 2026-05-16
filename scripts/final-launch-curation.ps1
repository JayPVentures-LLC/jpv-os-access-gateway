#!/usr/bin/env pwsh

# JPV-OS Access Gateway - Final Launch Curation Script
# Validates that all launch requirements are met before deployment

Write-Host "[final-launch-curation] Running final launch checks..." -ForegroundColor Cyan

$exitCode = 0

# 1. Build check
Write-Host "`n[1/5] Verifying release build..." -ForegroundColor Yellow
$buildResult = dotnet build JPVOS.sln -c Release 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "    ✗ Build failed" -ForegroundColor Red
    $exitCode = 1
} else {
    Write-Host "    ✓ Build succeeded" -ForegroundColor Green
}

# 2. UI term verification
Write-Host "`n[2/5] Scanning for banned public-facing terms..." -ForegroundColor Yellow
$bannedTerms = @("division", "master", "control")
$uiFiles = Get-ChildItem -Path "src/JPVOS/Components" -Recurse -Include "*.razor", "*.html" -File
$violations = @()

foreach ($file in $uiFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    foreach ($term in $bannedTerms) {
        if ($content -match "\b$term\b") {
            $violations += "$($file.Name): contains '$term'"
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "    ✗ Found banned terms:" -ForegroundColor Red
    $violations | ForEach-Object { Write-Host "      - $_" -ForegroundColor Red }
    $exitCode = 1
} else {
    Write-Host "    ✓ No banned terms found" -ForegroundColor Green
}

# 3. Navigation structure check
Write-Host "`n[3/5] Verifying navigation structure..." -ForegroundColor Yellow
$headerFile = "src/JPVOS/Components/SiteHeader.razor"
$headerContent = Get-Content -Path $headerFile -Raw

# Extract just the primary site-nav section using simpler pattern
$pattern = '(?s)<nav class="site-nav"[^>]*>(.*?)</nav>'
if ($headerContent -match $pattern) {
    $siteNavContent = $Matches[1]
    # Count all <a> tags in site-nav
    $allLinks = ([regex]::Matches($siteNavContent, '<a\s+[^>]*>')).Count
    
    # We expect 4 primary links + 1 CTA = 5 total
    if ($allLinks -eq 5) {
        Write-Host "    ✓ Navigation structure is clean (4 primary links + 1 CTA)" -ForegroundColor Green
    } elseif ($allLinks -lt 5) {
        Write-Host "    ✗ Header has only $allLinks links (expected 5: 4 primary + 1 CTA)" -ForegroundColor Red
        $exitCode = 1
    } else {
        Write-Host "    ✗ Header has $allLinks links (expected 5: 4 primary + 1 CTA)" -ForegroundColor Red
        $exitCode = 1
    }
} else {
    Write-Host "    ✗ Could not parse site-nav section" -ForegroundColor Red
    $exitCode = 1
}

# 4. Copy verification
Write-Host "`n[4/5] Verifying owner-approved copy..." -ForegroundColor Yellow
$copyChecks = @(
    @{ File = "src/JPVOS/Components/Pages/Home.razor"; Pattern = "JPV-OS are the operating standards" },
    @{ File = "src/JPVOS/Components/Pages/Home.razor"; Pattern = "init is the application interface for the JPV-OS ecosystem" },
    @{ File = "src/JPVOS/Components/Pages/Partners.razor"; Pattern = "Partner access is routed through JayPVentures LLC" },
    @{ File = "src/JPVOS/Components/Pages/JayPVenturesLLC.razor"; Pattern = "partnership strategy, venture alignment, and infrastructure execution" },
    @{ File = "src/JPVOS/Components/Pages/Jaypventures.razor"; Pattern = "creator-facing influence, products, community access, and market activation" },
    @{ File = "src/JPVOS/Components/Pages/JPVInstitute.razor"; Pattern = "standards, doctrine, and infrastructure literacy for responsible systems" },
    @{ File = "src/JPVOS/Components/Pages/JaypVLabs.razor"; Pattern = "validates research, prototypes, AI behavior, and system patterns" }
)

$copyViolations = 0
foreach ($check in $copyChecks) {
    if (Test-Path $check.File) {
        $content = Get-Content -Path $check.File -Raw
        if ($content -notmatch [regex]::Escape($check.Pattern)) {
            Write-Host "    ✗ Missing expected copy in $($check.File)" -ForegroundColor Red
            $copyViolations++
        }
    }
}

if ($copyViolations -gt 0) {
    Write-Host "    ✗ $copyViolations copy checks failed" -ForegroundColor Red
    $exitCode = 1
} else {
    Write-Host "    ✓ All owner-approved copy verified" -ForegroundColor Green
}

# 5. Wix checkout documentation
Write-Host "`n[5/5] Verifying Wix checkout documentation..." -ForegroundColor Yellow
if (-not (Test-Path "docs/WIX-CHECKOUT-ROUTING.md")) {
    Write-Host "    ✗ Wix checkout documentation missing" -ForegroundColor Red
    $exitCode = 1
} else {
    Write-Host "    ✓ Wix checkout documentation exists" -ForegroundColor Green
}

# Final summary
Write-Host "`n========================================" -ForegroundColor Cyan
if ($exitCode -eq 0) {
    Write-Host "✓ PASS: All launch curation checks passed" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
} else {
    Write-Host "✗ FAIL: Some launch curation checks failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
}

exit $exitCode
