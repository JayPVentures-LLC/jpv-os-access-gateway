[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$required = @(
    'README.md',
    'GOVERNANCE.md',
    'docs/COMMERCIAL-ACCESS-SETUP.md',
    'docs/governance/JPV-ORG-REPO-GOVERNANCE.md',
    'docs/repo-consolidation-manifest.md',
    'src/JPVOS/Components/Pages/Home.razor',
    'src/JPVOS/Pages/AccessRouting.razor',
    'src/JPVOS/Api/HealthController.cs',
    'scripts/validate-jpv-nexus-authority.mjs',
    'authority/nexus-authority.json'
)

$failures = New-Object System.Collections.Generic.List[string]
$identitySurfaces = @{}

foreach ($relativePath in $required) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required JPV Nexus surface: $relativePath")
        continue
    }

    if ($relativePath -ne 'authority/nexus-authority.json') {
        $identitySurfaces[$relativePath] = Get-Content -LiteralPath $path -Raw
    }
}

if ($failures.Count -eq 0) {
    $authority = Get-Content -LiteralPath (Join-Path $repoRoot 'authority/nexus-authority.json') -Raw | ConvertFrom-Json

    foreach ($surface in $identitySurfaces.GetEnumerator()) {
        if ($surface.Value -notmatch [regex]::Escape('JPV Nexus')) {
            $failures.Add("$($surface.Key) missing JPV Nexus identity.")
        }
    }

    foreach ($relativePath in @(
        'README.md',
        'GOVERNANCE.md',
        'docs/COMMERCIAL-ACCESS-SETUP.md',
        'docs/governance/JPV-ORG-REPO-GOVERNANCE.md',
        'docs/repo-consolidation-manifest.md',
        'src/JPVOS/Components/Pages/Home.razor',
        'src/JPVOS/Pages/AccessRouting.razor',
        'src/JPVOS/Api/HealthController.cs'
    )) {
        $content = $identitySurfaces[$relativePath]
        if ($content -match 'JPV-OS Access Gateway|\bAccess Gateway\b') {
            $failures.Add("$relativePath still exposes legacy Access Gateway product identity.")
        }
    }

    if ($authority.system -ne 'jpv-nexus') {
        $failures.Add("authority.system must be jpv-nexus; got '$($authority.system)'.")
    }
    if ($authority.product -ne 'JPV Nexus') {
        $failures.Add("authority.product must be JPV Nexus; got '$($authority.product)'.")
    }

    $validator = $identitySurfaces['scripts/validate-jpv-nexus-authority.mjs']
    if ($validator -notmatch [regex]::Escape('authority/nexus-authority.json')) {
        $failures.Add('Validator does not target authority/nexus-authority.json.')
    }

    foreach ($retiredPath in @(
        'authority/access-gateway-authority.json',
        'scripts/validate-access-gateway-authority.mjs',
        '.github/workflows/access-gateway-authority-validation.yml',
        'docs/governance/access-gateway-authority-validation.md'
    )) {
        if (Test-Path -LiteralPath (Join-Path $repoRoot $retiredPath)) {
            $failures.Add("Legacy Access Gateway canonical surface still exists: $retiredPath")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "JPV Nexus identity validation failed with $($failures.Count) error(s)."
}

Write-Output 'JPV Nexus identity validation: PASS'
