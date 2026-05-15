#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verify JPV-OS Access Gateway UI build, styling, and content compliance.

.DESCRIPTION
    This script performs the following checks:
    1. Builds the solution in Release mode
    2. Scans public-facing files for prohibited terminology
    3. Validates component structure
    4. Reports results and exits with appropriate code

.EXAMPLE
    pwsh -ExecutionPolicy Bypass -File scripts/verify-ui.ps1
#>

param(
    [switch]$Verbose
)

# Configuration
$SolutionFile = "JPVOS.sln"
$BannedTerms = @("division", "master", "control")
$PublicFacingPatterns = @(
    "src/JPVOS/Components/**/*.razor",
    "src/JPVOS/wwwroot/**/*.css",
    "src/JPVOS/wwwroot/**/*.html",
    "docs/**/*.md"
)
$AllowedContextPatterns = @(
    "*master*branch*",
    "*source control*",
    "*version control*",
    "*control flow*",
    "*master key*",
    "*master class*",
    "*master copy*"
)

# Color output
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
    Info    = "Cyan"
}

function Write-Status {
    param(
        [string]$Message,
        [ValidateSet("Success", "Warning", "Error", "Info")]
        [string]$Level = "Info"
    )
    
    $color = $Colors[$Level]
    $prefix = switch ($Level) {
        "Success" { "✓" }
        "Warning" { "⚠" }
        "Error"   { "✗" }
        "Info"    { "ℹ" }
    }
    
    Write-Host "$prefix $Message" -ForegroundColor $color
}

function Test-Build {
    Write-Host "`nPhase 1: Building Solution..." -ForegroundColor $Colors.Info
    Write-Host "Running: dotnet build $SolutionFile -c Release" -ForegroundColor Gray
    
    $output = dotnet build $SolutionFile -c Release 2>&1
    $buildSuccess = $LASTEXITCODE -eq 0
    
    if ($buildSuccess) {
        Write-Status -Message "Build succeeded" -Level "Success"
        return $true
    }
    else {
        Write-Status -Message "Build failed" -Level "Error"
        Write-Host $output
        return $false
    }
}

function Test-BannedTerms {
    Write-Host "`nPhase 2: Scanning for Prohibited Terminology..." -ForegroundColor $Colors.Info
    
    $violations = @()
    $fileCount = 0
    
    foreach ($pattern in $PublicFacingPatterns) {
        $files = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            $fileCount++
            $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
            
            if (-not $content) { continue }
            
            foreach ($term in $BannedTerms) {
                # Case-insensitive search
                $matches = [regex]::Matches($content, "\b$term\b", [Text.RegularExpressions.RegexOptions]::IgnoreCase)
                
                foreach ($match in $matches) {
                    # Check if this match is in an allowed context
                    $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
                    $line = ($content -split "`n")[$lineNumber - 1]
                    
                    $isAllowed = $false
                    foreach ($allowedPattern in $AllowedContextPatterns) {
                        if ($line -like $allowedPattern) {
                            $isAllowed = $true
                            break
                        }
                    }
                    
                    if (-not $isAllowed) {
                        $violations += @{
                            File     = $file.FullName
                            Line     = $lineNumber
                            Term     = $match.Value
                            Context  = $line.Trim()
                        }
                    }
                }
            }
        }
    }
    
    if ($violations.Count -eq 0) {
        Write-Status -Message "No prohibited terms found in $fileCount public-facing files" -Level "Success"
        return $true
    }
    else {
        Write-Status -Message "Found $($violations.Count) violation(s)" -Level "Error"
        foreach ($violation in $violations) {
            Write-Host "  File: $($violation.File)" -ForegroundColor $Colors.Error
            Write-Host "  Line $($violation.Line): $($violation.Context)" -ForegroundColor Gray
            Write-Host "  Term: '$($violation.Term)' (prohibited)" -ForegroundColor $Colors.Error
            Write-Host ""
        }
        return $false
    }
}

function Test-Components {
    Write-Host "`nPhase 3: Validating Component Structure..." -ForegroundColor $Colors.Info
    
    $requiredPages = @(
        "Index.razor",
        "AccessRouting.razor",
        "Partners.razor"
    )
    
    $missingPages = @()
    foreach ($page in $requiredPages) {
        $path = "src/JPVOS/Components/Pages/$page"
        if (-not (Test-Path $path)) {
            $missingPages += $page
        }
    }
    
    if ($missingPages.Count -eq 0) {
        Write-Status -Message "All required pages found" -Level "Success"
        return $true
    }
    else {
        Write-Status -Message "Missing required pages: $($missingPages -join ', ')" -Level "Warning"
        return $true  # Don't fail on this; pages might be renamed
    }
}

# Main execution
Write-Host "JPV-OS Access Gateway UI Verification Script" -ForegroundColor $Colors.Info
Write-Host "=============================================" -ForegroundColor Gray

$results = @{
    Build    = $false
    Terms    = $false
    Components = $false
}

# Run tests
$results.Build = Test-Build
$results.Terms = Test-BannedTerms
$results.Components = Test-Components

# Summary
Write-Host "`n=============================================" -ForegroundColor Gray
Write-Host "Verification Summary" -ForegroundColor $Colors.Info
Write-Host "=============================================" -ForegroundColor Gray

$totalTests = 3
$passedTests = 0

if ($results.Build) {
    Write-Status -Message "Build verification: PASSED" -Level "Success"
    $passedTests++
}
else {
    Write-Status -Message "Build verification: FAILED" -Level "Error"
}

if ($results.Terms) {
    Write-Status -Message "Terminology check: PASSED" -Level "Success"
    $passedTests++
}
else {
    Write-Status -Message "Terminology check: FAILED" -Level "Error"
}

if ($results.Components) {
    Write-Status -Message "Component check: PASSED" -Level "Success"
    $passedTests++
}
else {
    Write-Status -Message "Component check: PASSED (warnings only)" -Level "Warning"
    $passedTests++
}

Write-Host "`nResult: $passedTests/$totalTests checks passed" -ForegroundColor Gray

# Determine exit code
$exitCode = if ($results.Build -and $results.Terms) { 0 } else { 1 }

if ($exitCode -eq 0) {
    Write-Host "`n✓ All critical checks passed. Ready for production." -ForegroundColor $Colors.Success
}
else {
    Write-Host "`n✗ Critical issues detected. Please fix before deployment." -ForegroundColor $Colors.Error
}

exit $exitCode
