$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "src\JPVOS\JPVOS.csproj"
$Url = if ($args.Count -gt 0) { $args[0] } else { "http://localhost:5111" }

if (-not (Test-Path $Project)) {
    throw "Project not found: $Project"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw "dotnet is missing and winget is unavailable. Install the .NET 8 SDK from Microsoft, then rerun this script."
    }

    Write-Host "Installing .NET 8 SDK..."
    winget install Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements

    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "The .NET SDK installer finished, but dotnet is not on PATH yet. Close and reopen VS Code, then rerun this script."
    }
}

dotnet --info
dotnet build $Project
dotnet run --project $Project --no-launch-profile --urls $Url
