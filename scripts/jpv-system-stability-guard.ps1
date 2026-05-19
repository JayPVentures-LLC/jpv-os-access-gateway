$ErrorActionPreference = "Continue"

$Root = "C:\Users\jaypv\JPV-OS-Workspace\01-Active-Apps\jpv-os-access-gateway"
$LogDir = "C:\Users\jaypv\JPV-OS-Workspace\_system-health"
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"

New-Item -ItemType Directory -Path $LogDir -Force | Out-Null

$Log = Join-Path $LogDir "jpv-system-health-$Stamp.txt"

function Write-Log {
    param([string]$Message)
    $Line = "$(Get-Date -Format s)  $Message"
    Write-Host $Line
    Add-Content -Path $Log -Value $Line
}

Write-Log "======================================"
Write-Log "JPV SYSTEM STABILITY GUARD"
Write-Log "======================================"

Write-Log "Stopping WSL..."
wsl --shutdown 2>$null

Write-Log "Stopping heavy nonessential dev/media processes..."
Get-Process Docker*,vmmem*,wsl,node,npm,dotnet,M365Copilot,Canva -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

Write-Log "Checking disk space..."
$Drive = Get-PSDrive C
$FreeGB = [math]::Round($Drive.Free / 1GB, 2)
$UsedGB = [math]::Round($Drive.Used / 1GB, 2)

Write-Log "C: UsedGB=$UsedGB FreeGB=$FreeGB"

if ($FreeGB -lt 40) {
    Write-Log "WARNING: Disk free space below 40 GB. Running temp cleanup."

    Remove-Item "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\Windows\Temp\*" -Recurse -Force -ErrorAction SilentlyContinue
    Clear-RecycleBin -Force -ErrorAction SilentlyContinue
}

Write-Log "Cleaning active repo build artifacts only..."
Set-Location $Root

$SafeDeleteDirs = @(
    ".\publish",
    ".\dist-publish",
    ".\release-artifacts",
    ".\src\JPVOS\bin",
    ".\src\JPVOS\obj"
)

foreach ($Dir in $SafeDeleteDirs) {
    if (Test-Path $Dir) {
        Write-Log "Deleting $Dir"
        Remove-Item $Dir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$SafeDeleteFiles = @(
    ".\publish.zip",
    ".\stripe-test.env",
    ".\github_pat.txt",
    ".\azure-publish-profile-jpv-os-access-gateway.xml"
)

foreach ($File in $SafeDeleteFiles) {
    if (Test-Path $File) {
        Write-Log "Deleting $File"
        Remove-Item $File -Force -ErrorAction SilentlyContinue
    }
}

Write-Log "Clearing NuGet caches..."
dotnet nuget locals all --clear | Add-Content -Path $Log

Write-Log "Checking repo status..."
git status --short | Add-Content -Path $Log

Write-Log "Checking current branch..."
git branch --show-current | Add-Content -Path $Log

Write-Log "Checking top memory processes..."
Get-Process |
    Sort-Object WorkingSet -Descending |
    Select-Object -First 15 ProcessName,Id,@{Name="MemoryGB";Expression={[math]::Round($_.WorkingSet/1GB,2)}} |
    Format-Table -AutoSize |
    Out-String |
    Add-Content -Path $Log

Write-Log "Final disk space..."
$DriveAfter = Get-PSDrive C
$FreeAfterGB = [math]::Round($DriveAfter.Free / 1GB, 2)
$UsedAfterGB = [math]::Round($DriveAfter.Used / 1GB, 2)

Write-Log "C: UsedGB=$UsedAfterGB FreeGB=$FreeAfterGB"

Write-Log "======================================"
Write-Log "STABILITY GUARD COMPLETE"
Write-Log "Log: $Log"
Write-Log "======================================"
