Write-Host "Running JPV-OS UI verification..." -ForegroundColor Cyan

dotnet build .\JPVOS.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED" -ForegroundColor Red
    exit 1
}

$banned = @("division","master","control")

$files = Get-ChildItem -Recurse -Include *.razor,*.cshtml,*.html,*.md,*.css |
Where-Object {
    $_.FullName -notmatch "\\bin\\|\\obj\\|\\.git\\|backup|archive|reference"
}

$violations = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue

    foreach ($word in $banned) {
        if ($content -match "\b$word\b") {
            $violations += "$word => $($file.FullName)"
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "`nBANNED PUBLIC TERMS FOUND:" -ForegroundColor Yellow
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}

Write-Host "`nVerification PASS" -ForegroundColor Green
exit 0
