[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WebAppName,

    [string]$Repository = 'JayPVentures-LLC/jpv-os-access-gateway',

    [string]$Branch = 'main',

    [string]$ApplicationDisplayName = 'jpv-os-access-gateway-github-actions',

    [switch]$SkipWorkflowDispatch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    $output = & $FilePath @ArgumentList 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath $($ArgumentList -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return ($output -join [Environment]::NewLine).Trim()
}

function Assert-CommandAvailable {
    param([Parameter(Mandatory = $true)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is not installed or is not on PATH."
    }
}

Assert-CommandAvailable -Name 'az'
Assert-CommandAvailable -Name 'gh'

Write-Host 'Verifying authenticated Azure and GitHub sessions...'
$accountJson = Invoke-Native -FilePath 'az' -ArgumentList @('account', 'show', '--output', 'json') # az account show
$account = $accountJson | ConvertFrom-Json
$null = Invoke-Native -FilePath 'gh' -ArgumentList @('auth', 'status')
$null = Invoke-Native -FilePath 'gh' -ArgumentList @('repo', 'view', $Repository, '--json', 'nameWithOwner')

$tenantId = [string]$account.tenantId
$subscriptionId = [string]$account.id
if ([string]::IsNullOrWhiteSpace($tenantId) -or [string]::IsNullOrWhiteSpace($subscriptionId)) {
    throw 'Azure account context did not provide tenant and subscription identifiers.'
}

Write-Host "Resolving production App Service '$WebAppName'..."
$webAppId = Invoke-Native -FilePath 'az' -ArgumentList @(
    'webapp', 'list',
    '--query', "[?name=='$WebAppName'].id | [0]",
    '--output', 'tsv'
)
if ([string]::IsNullOrWhiteSpace($webAppId)) {
    throw "Azure App Service '$WebAppName' was not found in subscription '$subscriptionId'."
}

Write-Host "Resolving deployment application '$ApplicationDisplayName'..."
$appListJson = Invoke-Native -FilePath 'az' -ArgumentList @(
    'ad', 'app', 'list', # az ad app
    '--display-name', $ApplicationDisplayName,
    '--query', '[].{appId:appId,id:id,displayName:displayName}',
    '--output', 'json'
)
$appMatches = @($appListJson | ConvertFrom-Json)
if ($appMatches.Count -gt 1) {
    throw "Multiple Azure app registrations named '$ApplicationDisplayName' exist. Refusing ambiguous identity selection."
}

if ($appMatches.Count -eq 0) {
    $appJson = Invoke-Native -FilePath 'az' -ArgumentList @(
        'ad', 'app', 'create', # az ad app create
        '--display-name', $ApplicationDisplayName,
        '--output', 'json'
    )
    $app = $appJson | ConvertFrom-Json
} else {
    $app = $appMatches[0]
}

$clientId = [string]$app.appId
$appObjectId = [string]$app.id
if ([string]::IsNullOrWhiteSpace($clientId) -or [string]::IsNullOrWhiteSpace($appObjectId)) {
    throw 'Azure app registration did not expose both appId and object id.'
}

$servicePrincipalObjectId = Invoke-Native -FilePath 'az' -ArgumentList @(
    'ad', 'sp', 'show', '--id', $clientId, '--query', 'id', '--output', 'tsv'
)
if ([string]::IsNullOrWhiteSpace($servicePrincipalObjectId)) {
    $servicePrincipalObjectId = Invoke-Native -FilePath 'az' -ArgumentList @(
        'ad', 'sp', 'create', '--id', $clientId, '--query', 'id', '--output', 'tsv'
    )
}

$credentialName = 'github-main'
$subject = "repo:$Repository`:ref:refs/heads/$Branch"
$existingCredentialJson = Invoke-Native -FilePath 'az' -ArgumentList @(
    'ad', 'app', 'federated-credential', 'list', # az ad app federated-credential
    '--id', $appObjectId,
    '--query', "[?name=='$credentialName']",
    '--output', 'json'
)
$existingCredentials = @($existingCredentialJson | ConvertFrom-Json)

$credentialPayload = @{
    name        = $credentialName
    issuer      = 'https://token.actions.githubusercontent.com'
    subject     = $subject
    audiences   = @('api://AzureADTokenExchange')
    description = 'JPV production deployment from GitHub Actions main branch'
}

if ($existingCredentials.Count -eq 0) {
    $tempFile = Join-Path ([System.IO.Path]::GetTempPath()) "jpv-oidc-$([guid]::NewGuid().ToString('N')).json"
    try {
        $credentialPayload | ConvertTo-Json -Depth 4 | Set-Content -Path $tempFile -Encoding utf8NoBOM
        $null = Invoke-Native -FilePath 'az' -ArgumentList @(
            'ad', 'app', 'federated-credential', 'create', # az ad app federated-credential create
            '--id', $appObjectId,
            '--parameters', $tempFile,
            '--output', 'none'
        )
    } finally {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
} else {
    $existing = $existingCredentials[0]
    $audiences = @($existing.audiences)
    $matchesContract =
        ([string]$existing.issuer -eq $credentialPayload.issuer) -and
        ([string]$existing.subject -eq $credentialPayload.subject) -and
        ($audiences.Count -eq 1) -and
        ([string]$audiences[0] -eq $credentialPayload.audiences[0])

    if (-not $matchesContract) {
        throw "Existing federated credential '$credentialName' does not match the required GitHub main-branch trust contract. Refusing silent trust expansion."
    }
}

Write-Host 'Ensuring least-privilege App Service role assignment...'
$roleAssignmentId = Invoke-Native -FilePath 'az' -ArgumentList @(
    'role', 'assignment', 'list',
    '--assignee-object-id', $servicePrincipalObjectId,
    '--scope', $webAppId,
    '--role', 'Website Contributor',
    '--query', '[0].id',
    '--output', 'tsv'
)
if ([string]::IsNullOrWhiteSpace($roleAssignmentId)) {
    $null = Invoke-Native -FilePath 'az' -ArgumentList @(
        'role', 'assignment', 'create', # az role assignment create
        '--assignee-object-id', $servicePrincipalObjectId,
        '--assignee-principal-type', 'ServicePrincipal',
        '--role', 'Website Contributor',
        '--scope', $webAppId,
        '--output', 'none'
    )
}

Write-Host 'Writing GitHub Actions deployment identity secrets...'
$clientId | gh secret set AZURE_CLIENT_ID --repo $Repository
if ($LASTEXITCODE -ne 0) { throw 'gh secret set AZURE_CLIENT_ID failed.' }
$tenantId | gh secret set AZURE_TENANT_ID --repo $Repository
if ($LASTEXITCODE -ne 0) { throw 'gh secret set AZURE_TENANT_ID failed.' }
$subscriptionId | gh secret set AZURE_SUBSCRIPTION_ID --repo $Repository
if ($LASTEXITCODE -ne 0) { throw 'gh secret set AZURE_SUBSCRIPTION_ID failed.' }
$WebAppName | gh secret set AZURE_WEBAPP_NAME --repo $Repository
if ($LASTEXITCODE -ne 0) { throw 'gh secret set AZURE_WEBAPP_NAME failed.' }

if (-not $SkipWorkflowDispatch) {
    Write-Host 'Triggering canonical production deployment...'
    $mainSha = Invoke-Native -FilePath 'gh' -ArgumentList @(
        'api', "repos/$Repository/commits/$Branch", '--jq', '.sha'
    )
    $dispatchStartedAt = [DateTimeOffset]::UtcNow

    $null = Invoke-Native -FilePath 'gh' -ArgumentList @(
        'workflow', 'run', 'deploy-appservice.yml', # gh workflow run deploy-appservice.yml
        '--repo', $Repository,
        '--ref', $Branch
    )

    $runId = $null
    for ($attempt = 0; $attempt -lt 30 -and -not $runId; $attempt++) {
        Start-Sleep -Seconds 2
        $runsJson = Invoke-Native -FilePath 'gh' -ArgumentList @(
            'run', 'list',
            '--repo', $Repository,
            '--workflow', 'deploy-appservice.yml',
            '--branch', $Branch,
            '--event', 'workflow_dispatch',
            '--limit', '10',
            '--json', 'databaseId,headSha,createdAt'
        )
        $runs = @($runsJson | ConvertFrom-Json)
        $match = $runs |
            Where-Object {
                $_.headSha -eq $mainSha -and
                ([DateTimeOffset]$_.createdAt) -ge $dispatchStartedAt.AddSeconds(-5)
            } |
            Sort-Object { [DateTimeOffset]$_.createdAt } -Descending |
            Select-Object -First 1
        if ($match) {
            $runId = [string]$match.databaseId
        }
    }

    if (-not $runId) {
        throw 'Production deployment workflow was dispatched but its run could not be resolved deterministically.'
    }

    & gh run watch $runId --repo $Repository --exit-status # gh run watch
    if ($LASTEXITCODE -ne 0) {
        throw "Production deployment workflow run $runId failed."
    }
}

$healthUri = "https://$WebAppName.azurewebsites.net/health" # /health
Write-Host "Verifying deployed runtime at $healthUri..."
$health = Invoke-RestMethod -Uri $healthUri -Method Get -TimeoutSec 30
if ($health.status -ne 'healthy') {
    throw "Production health endpoint reported '$($health.status)' instead of healthy."
}
if ($health.productionAttentionAdmission.registered -ne $true) { # productionAttentionAdmission
    throw 'Production attention admission service is not registered.'
}
if ($health.productionAttentionAdmission.mode -ne 'fail-closed') { # fail-closed
    throw "Production attention admission mode is '$($health.productionAttentionAdmission.mode)' instead of fail-closed."
}

$receipt = [ordered]@{
    repository                    = $Repository
    branch                        = $Branch
    webAppName                    = $WebAppName
    azureSubscriptionId           = $subscriptionId
    azureTenantId                 = $tenantId
    deploymentClientId            = $clientId
    deploymentServicePrincipalId  = $servicePrincipalObjectId
    federatedCredential           = $credentialName
    federatedSubject              = $subject
    role                          = 'Website Contributor'
    scope                         = $webAppId
    health                        = 'healthy'
    productionAttentionAdmission  = 'fail-closed'
    verifiedAtUtc                 = [DateTimeOffset]::UtcNow.ToString('O')
}

$receipt | ConvertTo-Json -Depth 4
