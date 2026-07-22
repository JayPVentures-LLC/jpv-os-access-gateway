$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$required=@('README.md','src/JPVOS','render.yaml','docs/CONTAINER-DEPLOYMENT.md')
foreach($path in $required){if(-not(Test-Path (Join-Path $root $path))){throw "Missing access gateway artifact: $path"}}
Write-Host 'JPV access gateway executable baseline verified.'
exit 0
