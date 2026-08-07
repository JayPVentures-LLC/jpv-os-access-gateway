[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$policyPath=Join-Path $root '.jpv/governance/no-manual-founder-fallback.v1.json'
$governancePath=Join-Path $root 'GOVERNANCE.md'
foreach($p in @($policyPath,$governancePath)){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw "Missing governance artifact: $p"}}
$policy=Get-Content $policyPath -Raw -Encoding UTF8|ConvertFrom-Json -Depth 30
$gov=Get-Content $governancePath -Raw -Encoding UTF8
if($policy.enforcement -ne 'REJECT'){throw 'No-manual-founder-fallback enforcement must be REJECT.'}
foreach($required in @('merge','deploy','run_local_command','retry','monitor_dependency','reconcile_runtime')){if($required -notin @($policy.forbidden_founder_labor)){throw "Missing forbidden founder labor class: $required"}}
foreach($decision in @('material_authority_grant','legally_required_consent','irreversible_business_decision','financial_commitment','material_policy_choice')){if($decision -notin @($policy.founder_decision_classes)){throw "Missing founder decision class: $decision"}}
if(-not $gov.Contains('JPV-NO-MANUAL-FOUNDER-FALLBACK-V1')){throw 'Repository governance does not inherit enterprise standard.'}
Write-Host 'PASS: access gateway inherits no-manual-founder-fallback standard'
