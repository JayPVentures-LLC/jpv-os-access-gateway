$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "[verify-ui] Building solution..." -ForegroundColor Cyan
    dotnet build JPVOS.sln -c Release | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[verify-ui] FAIL: dotnet build failed." -ForegroundColor Red
        exit 1
    }

    Write-Host "[verify-ui] Scanning for banned public-facing terms..." -ForegroundColor Cyan

    $bannedTerms = @("division", "master", "control")
    $publicUiTargets = @(
        "src/JPVOS/Components/**/*.razor",
        "src/JPVOS/Pages/**/*.razor",
        "dist-static/**/*.html"
    )

    $violations = @()

    foreach ($term in $bannedTerms) {
        foreach ($target in $publicUiTargets) {
            $matches = Select-String -Path $target -Pattern ("\\b" + [regex]::Escape($term) + "\\b") -AllMatches -CaseSensitive:$false -ErrorAction SilentlyContinue
            if ($matches) {
                $violations += $matches
            }
        }
    }

    if ($violations.Count -gt 0) {
        Write-Host "[verify-ui] FAIL: banned public-facing terms found." -ForegroundColor Red
        $violations |
            Sort-Object Path, LineNumber |
            ForEach-Object {
                Write-Host (" - {0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line.Trim()) -ForegroundColor Yellow
            }
        exit 1
    }

    Write-Host "[verify-ui] PASS: build succeeded and no banned public-facing terms were found." -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
