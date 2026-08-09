#requires -Version 7.0
[CmdletBinding()]
param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$manifestPath = Join-Path $RepositoryRoot '.jpv/jpv-os-governance.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Governance manifest missing: $manifestPath" }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -Depth 50

$contract = 'governance/contracts/root-continuity-invariant.v1.json'
if ($contract -notin @($manifest.requiredContracts)) { throw "Root continuity contract not inherited: $contract" }

$required = [ordered]@{
    authorizedIntentProducesAutonomousVerifiedOutcome = $true
    deviceIndependentContinuity = $true
    sessionIndependentContinuity = $true
    transportIndependentAfterAdmission = $true
    checkoutIndependentContinuity = $true
    actuatorIndependentContinuity = $true
    hostedPrimaryForCloud = $true
    acceptedOperationSurvivesIngressExpiration = $true
    windowsOnlyForExplicitWindowsCapabilities = $true
    automaticRetryAndEligibleRerouting = $true
    routineRecoveryRequiresFounderAttention = $false
    githubIsFallbackAdmissionAndRecoveryOnly = $true
    mutationSuccessIsNotCompletion = $true
    terminalReceiptRequiredForCompletion = $true
    downstreamRulesMayNotWeakenAuthority = $true
}

$failures = @()
foreach ($entry in $required.GetEnumerator()) {
    $property = $manifest.rules.PSObject.Properties[$entry.Key]
    if ($null -eq $property -or [bool]$property.Value -ne [bool]$entry.Value) {
        $failures += "$($entry.Key) must equal $($entry.Value)"
    }
}
if ([string]$manifest.enforcement -ne 'fail_closed') { $failures += 'enforcement must remain fail_closed' }
if ($failures.Count) { throw "Root continuity validation failed:`n - $($failures -join "`n - ")" }

[ordered]@{
    status='PASS'
    invariant='authorized_intent_in_autonomous_verified_outcome_out'
    contract=$contract
    rules_verified=$required.Count
    local_machine_required_for_cloud=$false
} | ConvertTo-Json -Depth 10
