$ErrorActionPreference = "Continue"

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$ReportPath = Join-Path $Root "reports\final-launch-curation-report.md"

$bannedTerms = @(
    "division",
    "master",
    "control"
)

$placeholderTerms = @(
    "lorem ipsum",
    "coming soon",
    "todo",
    "placeholder",
    "fake testimonial",
    "generic enterprise"
)

$preferredTerms = @(
    "orchestration",
    "routing",
    "governance",
    "operational integrity",
    "infrastructure authority",
    "validation layer",
    "alignment",
    "coordination",
    "structured execution",
    "access gateway"
)

$ignoredPathPattern = "\\bin\\|\\obj\\|\\.git\\|backup|archive|reference|\.disabled"

$publicFiles = Get-ChildItem -Recurse -File -Include *.razor,*.cshtml,*.html,*.md,*.css |
    Where-Object { $_.FullName -notmatch $ignoredPathPattern }

$bannedFindings = @()
$placeholderFindings = @()
$missingImageFindings = @()
$externalImageFindings = @()
$nonApprovedImageFindings = @()
$bootstrapFindings = @()
$firstPersonReviewFindings = @()

foreach ($file in $publicFiles) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($content)) { continue }

    foreach ($term in $bannedTerms) {
        if ($content -match "\b$([regex]::Escape($term))\b") {
            $bannedFindings += "$term => $($file.FullName)"
        }
    }

    foreach ($term in $placeholderTerms) {
        if ($content -match [regex]::Escape($term)) {
            $placeholderFindings += "$term => $($file.FullName)"
        }
    }

    if ($content -match "bootstrap|btn-primary|container-fluid|navbar-expand|card-body") {
        $bootstrapFindings += "Bootstrap/default UI marker => $($file.FullName)"
    }

    if ($file.Extension -in ".razor",".html",".cshtml",".md") {
        if ($content -match "founder|I built|I designed|my ecosystem|my infrastructure|my system") {
            $firstPersonReviewFindings += "Founder/first-person review => $($file.FullName)"
        }
    }

    $imageMatches = [regex]::Matches($content, '(src|href)\s*=\s*["'']([^"'']+\.(png|jpg|jpeg|webp|svg|gif))["'']', 'IgnoreCase')

    foreach ($match in $imageMatches) {
        $path = $match.Groups[2].Value

        if ($path -match "^https?://") {
            $externalImageFindings += "$path => $($file.FullName)"
            continue
        }

        if ($path.StartsWith("/")) {
            $relative = $path.TrimStart("/")
            $full = Join-Path $Root "wwwroot\$relative"
        }
        elseif ($path.StartsWith("wwwroot/") -or $path.StartsWith("wwwroot\")) {
            $full = Join-Path $Root $path
        }
        else {
            $full = Join-Path (Split-Path $file.FullName -Parent) $path
        }

        if (-not (Test-Path $full)) {
            $missingImageFindings += "$path => $($file.FullName)"
        }

        if (
            ($file.Name -match "Home|Index|Founder|About|Ecosystem|Pricing|Partners|Access|Login") -and
            ($path -match "hero|founder|background|bg|poster|ecosystem|gateway") -and
            ($path -notmatch "assets/approved|assets\\approved")
        ) {
            $nonApprovedImageFindings += "$path => $($file.FullName)"
        }
    }
}

$buildOutput = & dotnet build ".\JPVOS.csproj" 2>&1
$buildExit = $LASTEXITCODE

$verifyOutput = ""
$verifyExit = 0

if (Test-Path ".\scripts\verify-ui.ps1") {
    $verifyOutput = & powershell -ExecutionPolicy Bypass -File ".\scripts\verify-ui.ps1" 2>&1
    $verifyExit = $LASTEXITCODE
}
else {
    $verifyOutput = "scripts/verify-ui.ps1 not found."
    $verifyExit = 1
}

$fail = $false

if ($buildExit -ne 0) { $fail = $true }
if ($verifyExit -ne 0) { $fail = $true }
if ($bannedFindings.Count -gt 0) { $fail = $true }
if ($missingImageFindings.Count -gt 0) { $fail = $true }

$status = if ($fail) { "FAIL" } else { "PASS" }

$report = @()
$report += "# Final Launch Curation Report"
$report += ""
$report += "Status: **$status**"
$report += ""
$report += "## Build Result"
$report += ""
$report += "Exit code: $buildExit"
$report += ""
$report += '```text'
$report += ($buildOutput | Out-String)
$report += '```'
$report += ""
$report += "## verify-ui Result"
$report += ""
$report += "Exit code: $verifyExit"
$report += ""
$report += '```text'
$report += ($verifyOutput | Out-String)
$report += '```'
$report += ""
$report += "## Banned Public Terms"
$report += ""
if ($bannedFindings.Count) { $bannedFindings | ForEach-Object { $report += "- $_" } } else { $report += "None found." }
$report += ""
$report += "## Placeholder / Weak Launch Copy"
$report += ""
if ($placeholderFindings.Count) { $placeholderFindings | ForEach-Object { $report += "- $_" } } else { $report += "None found." }
$report += ""
$report += "## Missing Image References"
$report += ""
if ($missingImageFindings.Count) { $missingImageFindings | ForEach-Object { $report += "- $_" } } else { $report += "None found." }
$report += ""
$report += "## External Image References"
$report += ""
if ($externalImageFindings.Count) { $externalImageFindings | ForEach-Object { $report += "- $_" } } else { $report += "None found." }
$report += ""
$report += "## Public Hero / Founder / Background Assets Outside Approved Folder"
$report += ""
if ($nonApprovedImageFindings.Count) { $nonApprovedImageFindings | ForEach-Object { $report += "- $_" } } else { $report += "None found." }
$report += ""
$report += "## Bootstrap / Default UI Markers"
$report += ""
if ($bootstrapFindings.Count) { $bootstrapFindings | ForEach-Object { $report += "- $_" } } else { $report += "None found." }
$report += ""
$report += "## First-Person Founder Voice Review"
$report += ""
if ($firstPersonReviewFindings.Count) { $firstPersonReviewFindings | ForEach-Object { $report += "- $_" } } else { $report += "No founder/first-person review targets detected." }
$report += ""
$report += "## Preferred Public Terms"
$report += ""
$preferredTerms | ForEach-Object { $report += "- $_" }
$report += ""
$report += "## Owner Review List"
$report += ""
$report += "- Review Home hero imagery and headline."
$report += "- Review Pricing wording for public clarity."
$report += "- Review Access/Login wording for init and JPV-OS alignment."
$report += "- Review Partners page for infrastructure authority language."
$report += "- Review Ecosystem page for operational-layer language."
$report += "- Confirm approved production imagery is under wwwroot/assets/approved."
$report += "- Complete final visual approval checklist: docs/FINAL-VISUAL-APPROVAL.md"
$report += ""
$report += "## Final Decision"
$report += ""
if ($fail) {
    $report += "Launch curation failed. Correct the findings above, then rerun scripts/final-launch-curation.ps1."
}
else {
    $report += "Launch curation passed. Proceed to final visual review using docs/FINAL-VISUAL-APPROVAL.md before deployment."
}

$report | Set-Content $ReportPath -Encoding UTF8

Write-Host "Final launch curation report written to: $ReportPath"

if ($fail) {
    Write-Host "Final launch curation: FAIL" -ForegroundColor Red
    exit 1
}

Write-Host "Final launch curation: PASS" -ForegroundColor Green
exit 0
