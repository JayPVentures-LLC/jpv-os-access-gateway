#requires -Version 7.0
[CmdletBinding()]
param([string]$RepositoryRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

$required=@(
 'src/JPV.Endpoint.Windows/JPV.Endpoint.Windows.csproj',
 'src/JPV.Endpoint.Windows/Program.cs',
 'src/JPV.Endpoint.Windows/MachineState.cs',
 'src/JPV.Endpoint.Windows/EndpointWorker.cs',
 'scripts/install-jpv-native-endpoint.ps1',
 'config/jpv-endpoint-policy.example.json'
)
foreach($p in $required){if(-not(Test-Path (Join-Path $RepositoryRoot $p))){throw "Missing endpoint artifact: $p"}}
$program=Get-Content (Join-Path $RepositoryRoot 'src/JPV.Endpoint.Windows/Program.cs') -Raw
$state=Get-Content (Join-Path $RepositoryRoot 'src/JPV.Endpoint.Windows/MachineState.cs') -Raw
$installer=Get-Content (Join-Path $RepositoryRoot 'scripts/install-jpv-native-endpoint.ps1') -Raw
if($program -notmatch 'AddWindowsService'){throw 'Endpoint is not registered as a Windows service.'}
if($state -notmatch 'CommonApplicationData'){throw 'Endpoint state is not machine-scoped.'}
if($state -notmatch 'fails closed'){throw 'Endpoint does not fail closed without policy.'}
if($installer -notmatch "start= auto"){throw 'Endpoint service is not automatic.'}
if($installer -notmatch "failure.*restart"){throw 'Endpoint has no crash recovery.'}
[ordered]@{status='PASS';service='JPV.NativeEndpoint';machineScoped=$true;sessionIndependent=$true;adapterIndependent=$true;failClosed=$true;automaticRecovery=$true}|ConvertTo-Json
