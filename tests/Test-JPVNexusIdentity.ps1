[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$required = @(
    'README.md',
    'src/JPVOS/Components/Pages/Home.razor',
    'src/JPVOS/Pages/AccessRouting.razor',
    'src/JPVOS/Api/HealthController.cs',
    'scripts/validate-access-gateway-authority.mjs',
    'authority/nexus-authority.json'
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($relativePath in $required) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required JPV Nexus surface: $relativePath")
    }
}

if ($failures.Count -eq 0) {
    $readme = Get-Content -LiteralPath (Join-Path $repoRoot 'README.md') -Raw
    $home = Get-Content -LiteralPath (Join-Path $repoRoot 'src/JPVOS/Components/Pages/Home.razor') -Raw
    $routing = Get-Content -LiteralPath (Join-Path $repoRoot 'src/JPVOS/Pages/AccessRouting.razor') -Raw
    $health = Get-Content -LiteralPath (Join-Path $repoRoot 'src/JPVOS/Api/HealthController.cs') -Raw
    $validator = Get-Content -LiteralPath (Join-Path $repoRoot 'scripts/validate-access-gateway-authority.mjs') -Raw
    $authority = Get-Content -LiteralPath (Join-Path $repoRoot 'authority/nexus-authority.json') -Raw | ConvertFrom-Json

    foreach ($surface in @{
        'README.md' = $readme
        'Home.razor' = $home
        'AccessRouting.razor' = $routing
        'HealthController.cs' = $health
        'validator' = $validator
    }.GetEnumerator()) {
        if ($surface.Value -notmatch [regex]::Escape('JPV Nexus')) {
            $failures.Add("$($surface.Key) missing JPV Nexus identity.")
        }
    }

    foreach ($surface in @{
        'README.md' = $readme
        'Home.razor' = $home
        'AccessRouting.razor' = $routing
        'HealthController.cs' = $health
    }.GetEnumerator()) {
        if ($surface.Value -match 'JPV-OS Access Gateway|\bAccess Gateway\b') {
            $failures.Add("$($surface.Key) still exposes legacy Access Gateway product identity.")
        }
    }

    if ($authority.system -ne 'jpv-nexus') {
        $failures.Add("authority.system must be jpv-nexus; got '$($authority.system)'.")
    }
    if ($authority.product -ne 'JPV Nexus') {
        $failures.Add("authority.product must be JPV Nexus; got '$($authority.product)'.")
    }
    if ($validator -notmatch [regex]::Escape('authority/nexus-authority.json')) {
        $failures.Add('Validator does not target authority/nexus-authority.json.')
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "JPV Nexus identity validation failed with $($failures.Count) error(s)."
}

Write-Output 'JPV Nexus identity validation: PASS'
