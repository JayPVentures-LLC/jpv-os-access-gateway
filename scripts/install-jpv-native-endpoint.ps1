#requires -RunAsAdministrator
#requires -Version 7.0
[CmdletBinding()]
param([string]$RepositoryRoot=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'

$project=Join-Path $RepositoryRoot 'src/JPV.Endpoint.Windows/JPV.Endpoint.Windows.csproj'
$installRoot=Join-Path $env:ProgramFiles 'JPV/Endpoint'
$stateRoot=Join-Path $env:ProgramData 'JPV/Endpoint'
$publish=Join-Path $env:TEMP 'jpv-endpoint-publish'
Remove-Item $publish -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish $project -c Release -r win-x64 --self-contained true -o $publish
if($LASTEXITCODE){throw 'JPV endpoint publish failed.'}
New-Item -ItemType Directory -Path $installRoot,$stateRoot -Force | Out-Null
Copy-Item (Join-Path $publish '*') $installRoot -Recurse -Force
$policy=Join-Path $stateRoot 'policy.json'
if(-not(Test-Path $policy)){Copy-Item (Join-Path $RepositoryRoot 'config/jpv-endpoint-policy.example.json') $policy}
$exe=Join-Path $installRoot 'JPV.Endpoint.Windows.exe'
if(Get-Service 'JPV.NativeEndpoint' -ErrorAction SilentlyContinue){Stop-Service 'JPV.NativeEndpoint' -Force; sc.exe delete 'JPV.NativeEndpoint' | Out-Null; Start-Sleep 1}
sc.exe create 'JPV.NativeEndpoint' binPath= ('"' + $exe + '"') start= auto obj= LocalSystem DisplayName= 'JPV Native Endpoint' | Out-Null
sc.exe failure 'JPV.NativeEndpoint' reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Null
sc.exe failureflag 'JPV.NativeEndpoint' 1 | Out-Null
Start-Service 'JPV.NativeEndpoint'
Start-Sleep 2
$svc=Get-Service 'JPV.NativeEndpoint'
if($svc.Status -ne 'Running'){throw "JPV.NativeEndpoint did not reach Running."}
$receipt=Join-Path $stateRoot 'endpoint-receipt.json'
if(-not(Test-Path $receipt)){throw 'JPV endpoint emitted no authoritative receipt.'}
Get-Content $receipt -Raw
