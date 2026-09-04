$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot 'ops/azure/bootstrap-github-oidc-deployment.ps1'

if (-not (Test-Path $scriptPath)) {
    throw "Missing bootstrap script: $scriptPath"
}

$tokens = $null
$errors = $null
[System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors) | Out-Null
if ($errors.Count -gt 0) {
    $messages = @($errors | ForEach-Object { $_.Message }) -join '; '
    throw "Bootstrap script has PowerShell parse errors: $messages"
}

$content = Get-Content $scriptPath -Raw
$requiredSnippets = @(
    'az account show',
    'az ad app',
    'az ad app federated-credential',
    'az role assignment create',
    'Website Contributor',
    'repo:$Repository`:ref:refs/heads/$Branch',
    'https://token.actions.githubusercontent.com',
    'api://AzureADTokenExchange',
    'gh secret set AZURE_CLIENT_ID',
    'gh secret set AZURE_TENANT_ID',
    'gh secret set AZURE_SUBSCRIPTION_ID',
    'gh secret set AZURE_WEBAPP_NAME',
    'gh workflow run deploy-appservice.yml',
    'gh run watch',
    '/health',
    'productionAttentionAdmission',
    'fail-closed'
)

foreach ($snippet in $requiredSnippets) {
    if ($content -notmatch [regex]::Escape($snippet)) {
        throw "Bootstrap script is missing required behavior marker: $snippet"
    }
}

if ($content -match 'client-secret|password-credentials|AZURE_WEBAPP_PUBLISH_PROFILE') {
    throw 'Bootstrap script must not reintroduce long-lived client secrets or publish-profile authentication.'
}

Write-Host 'Azure OIDC bootstrap contract validated.'
