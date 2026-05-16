# Local Setup

## Prerequisites
- .NET SDK 8.x
- PowerShell 7+ (recommended on Windows)

## Repository Root
Run commands from:

`/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway`

On Windows, use your local clone path and the same command sequence.

## Restore and Build
```powershell
dotnet restore JPVOS.sln
dotnet build JPVOS.sln -c Release
```

## Run the Blazor App
```powershell
dotnet run --project src/JPVOS/JPVOS.csproj
```

## Validate UI and Banned Public Terms
```powershell
pwsh ./scripts/verify-ui.ps1
```

## Notes
- Do not commit secrets, credentials, or license files.
- Keep governance and People Protection artifacts unchanged unless a build fix requires touch points.
- The current UI is designed to run without Telerik dependencies configured locally.
