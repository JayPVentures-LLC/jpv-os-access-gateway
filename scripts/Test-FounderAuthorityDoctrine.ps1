[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$GovernancePath = Join-Path $RepositoryRoot '.jpv/jpv-os-governance.json'
if (-not (Test-Path -LiteralPath $GovernancePath)) {
    throw "Required governance manifest missing: $GovernancePath"
}

$Manifest = Get-Content -LiteralPath $GovernancePath -Raw | ConvertFrom-Json
$RequiredContract = 'governance/contracts/founder-authority-continuity.v1.json'

$Failures = [System.Collections.Generic.List[string]]::new()

if ($RequiredContract -notin @($Manifest.requiredContracts)) {
    $Failures.Add("Missing required founder-authority contract: $RequiredContract")
}

$ExpectedRules = [ordered]@{
    settledFounderDecisionsAreAuthoritative = $true
    founderIsFinalInternalAuthority = $true
    delegationTransfersExecutionNotOwnership = $true
    delegatedAuthorityMustBeScopedAuditableAndRevocable = $true
    delegatesMayNotExpandOwnAuthority = $true
    founderRecoveryAuthorityMustBePreserved = $true
    automationHasNoInherentGovernanceAuthority = $true
    temporaryContinuityAuthorityIsCustodialOnly = $true
    softwareMayNotInferSuccession = $true
    repeatedMaterialDecisionFailureRequiresAuthorityReview = $true
    externalLegalAndBindingConstraintsRemainEffective = $true
    downstreamRulesMayNotWeakenAuthority = $true
}

foreach ($Entry in $ExpectedRules.GetEnumerator()) {
    $Property = $Manifest.rules.PSObject.Properties[$Entry.Key]
    if ($null -eq $Property -or [bool]$Property.Value -ne [bool]$Entry.Value) {
        $Failures.Add("Governance rule mismatch: $($Entry.Key) must equal $($Entry.Value)")
    }
}

if ($Manifest.enforcement -ne 'fail_closed') {
    $Failures.Add('Governance enforcement must remain fail_closed')
}

if ($Failures.Count -gt 0) {
    $Message = "Founder authority doctrine validation failed:`n - " + ($Failures -join "`n - ")
    throw $Message
}

[pscustomobject]@{
    Status = 'PASS'
    Contract = $RequiredContract
    Enforcement = $Manifest.enforcement
    ValidatedRules = $ExpectedRules.Count
} | ConvertTo-Json -Depth 4
