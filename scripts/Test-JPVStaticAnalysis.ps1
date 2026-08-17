#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$Solution = 'JPVOS.sln'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    throw "JPV_STATIC_ANALYSIS_FAILURE: $Message"
}

$solutionPath = Join-Path $RepositoryRoot $Solution
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    Fail "solution not found: $Solution"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { Fail 'dotnet SDK is required for static analysis' }

& $dotnet.Source restore $solutionPath --nologo
if ($LASTEXITCODE -ne 0) { Fail 'dotnet restore failed' }

& $dotnet.Source build $solutionPath --configuration Release --no-restore --nologo -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { Fail 'Release build/static analysis failed or emitted warnings' }

[ordered]@{
    schema_version = 'jpv.static-analysis-receipt.v1'
    status = 'PASS'
    repository = 'JayPVentures-LLC/jpv-os-access-gateway'
    solution = $Solution
    configuration = 'Release'
    warnings_as_errors = $true
    continuous_integration_build = $true
    provider_sarif_upload_required = $false
    evidence_mode = 'LOCAL_EQUIVALENT_STATIC_ANALYSIS'
    validated_at_utc = [DateTimeOffset]::UtcNow.ToString('o')
} | ConvertTo-Json -Depth 10
