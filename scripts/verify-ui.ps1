$ErrorActionPreference = "Stop"

# Banned terms for public-facing content
# These terms are prohibited to maintain inclusive language standards and brand consistency in user-facing materials:
# - "division" implies departmental silos; use "team" or "group" instead
# - "master" has problematic historical connotations; use "primary", "main", or "primary copy" instead
#   (Note: This may flag technical terms in documentation like "version control". Review matches contextually.)
# - "control" focuses on dominance; use "manage", "govern", or "administer" instead
#   (Note: Technical terms like "access control" should use "permission" or "authorization" in UI text.)
$BannedTerms = @(
  "division",
  "master",
  "control"
)

# File extensions to scan
$FileExtensions = @("*.razor", "*.cshtml", "*.html", "*.md", "*.css")

# Excluded folder patterns
$ExcludeFolders = @("bin", "obj", ".git", "backup", "archive", "reference")

Write-Host "=== UI Verification Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Find the solution file and active .csproj file
Write-Host "Step 1: Finding active project files..." -ForegroundColor Cyan

$slnFiles = @(Get-ChildItem -Path "." -Depth 1 -Filter "*.sln" -ErrorAction SilentlyContinue)
if ($slnFiles.Count -eq 0) {
  Write-Host "FAIL: No .sln file found in repository root" -ForegroundColor Red
  exit 1
}

if ($slnFiles.Count -gt 1) {
  Write-Host "WARNING: Multiple .sln files found; using the first one" -ForegroundColor Yellow
}

$slnPath = $slnFiles[0].FullName
$slnName = $slnFiles[0].Name

# Verify src directory exists
if (-not (Test-Path "src")) {
  Write-Host "FAIL: src directory not found" -ForegroundColor Red
  exit 1
}

# Find .csproj files in first-level subdirectories of src (expected structure: src/ProjectName/ProjectName.csproj)
$csprojFiles = @(Get-ChildItem -Path "src" -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match "src[\/\\][^\/\\]+[\/\\]" })

if ($csprojFiles.Count -eq 0) {
  Write-Host "FAIL: No .csproj file found" -ForegroundColor Red
  exit 1
}

$csprojPath = $csprojFiles[0].FullName
$csprojDir = Split-Path -Parent $csprojPath
$projectName = Split-Path -Leaf $csprojDir

Write-Host "  Found solution: $slnName" -ForegroundColor Green
Write-Host "  Found project: $projectName" -ForegroundColor Green

# Step 2: Run dotnet build
Write-Host ""
Write-Host "Step 2: Running dotnet build..." -ForegroundColor Cyan

try {
  $buildOutput = dotnet build $slnPath -c Release 2>&1
  if ($LASTEXITCODE -ne 0) {
    Write-Host "FAIL: Build failed" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
  }
  Write-Host "  Build successful" -ForegroundColor Green
}
catch {
  Write-Host "FAIL: Build error - $_" -ForegroundColor Red
  exit 1
}

# Step 3: Scan for banned terms in public-facing files
Write-Host ""
Write-Host "Step 3: Scanning public-facing files for banned terms..." -ForegroundColor Cyan

$filesWithBannedTerms = @()
$bannedTermsFound = @()

# Build exclusion filter
$excludeFilter = {
  $path = $_.FullName
  foreach ($excludeFolder in $ExcludeFolders) {
    if ($path -match "([\/\\]|^)$([regex]::Escape($excludeFolder))([\/\\]|$)") {
      return $false
    }
  }
  return $true
}

foreach ($ext in $FileExtensions) {
  $files = Get-ChildItem -Path "." -Recurse -Filter $ext -ErrorAction SilentlyContinue | Where-Object $excludeFilter
  
  foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { continue }
    
    foreach ($term in $BannedTerms) {
      # Case-insensitive search for whole word matches
      # Note: Uses \b for word boundaries; compound technical terms like 'master-branch' may match.
      # In UI files, such false positives are less common than in code; review matches contextually.
      if ($content -imatch "\b$([regex]::Escape($term))\b") {
        $filesWithBannedTerms += $file.FullName
        $bannedTermsFound += @{
          File = $file.FullName
          Term = $term
        }
      }
    }
  }
}

if ($bannedTermsFound.Count -gt 0) {
  Write-Host "FAIL: Banned terms found in public-facing files" -ForegroundColor Red
  Write-Host ""
  
  $bannedTermsFound | Group-Object { $_.File } | ForEach-Object {
    Write-Host "  File: $($_.Name)" -ForegroundColor Yellow
    $_.Group | ForEach-Object {
      Write-Host "    - Term: '$($_.Term)'" -ForegroundColor Red
    }
  }
  
  Write-Host ""
  exit 1
}

Write-Host "  No banned terms detected" -ForegroundColor Green

# All checks passed
Write-Host ""
Write-Host "PASS: UI verification successful" -ForegroundColor Green
Write-Host "  - Build completed successfully" -ForegroundColor Green
Write-Host "  - No banned terms found in public-facing files" -ForegroundColor Green
exit 0
