[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [int]$PullRequest,

    [string]$Repository,

    [switch]$ResolveOutdated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][hashtable]$Fields
    )

    $args = @('api') + $Arguments
    foreach ($entry in $Fields.GetEnumerator()) {
        $args += @('-F', "$($entry.Key)=$($entry.Value)")
    }

    $raw = & gh @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "gh failed: $($raw -join [Environment]::NewLine)"
    }
    return ($raw -join [Environment]::NewLine) | ConvertFrom-Json -Depth 100
}

if ([string]::IsNullOrWhiteSpace($Repository)) {
    $Repository = (& gh repo view --json nameWithOwner --jq '.nameWithOwner').Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Repository)) {
        throw 'Unable to resolve repository. Pass -Repository owner/name explicitly.'
    }
}

$parts = $Repository.Split('/', 2)
if ($parts.Count -ne 2) { throw 'Repository must be owner/name.' }
$owner = $parts[0]
$name = $parts[1]

$query = @'
query($owner:String!,$name:String!,$number:Int!,$after:String) {
  repository(owner:$owner,name:$name) {
    pullRequest(number:$number) {
      number
      url
      headRefOid
      mergeable
      mergeStateStatus
      reviewThreads(first:100,after:$after) {
        pageInfo { hasNextPage endCursor }
        nodes {
          id
          isResolved
          isOutdated
          path
          comments(first:1) { nodes { body author { login } } }
        }
      }
    }
  }
}
'@

function Get-LiveThreads {
    $cursor = $null
    $threads = @()
    $pr = $null
    do {
        $fields = @{ owner = $owner; name = $name; number = $PullRequest; query = $query }
        if ($null -ne $cursor) { $fields.after = $cursor }
        $result = Invoke-GhJson -Arguments @('graphql') -Fields $fields
        $pr = $result.data.repository.pullRequest
        if ($null -eq $pr) { throw "PR #$PullRequest was not found in $Repository." }
        $threads += @($pr.reviewThreads.nodes)
        $cursor = $pr.reviewThreads.pageInfo.endCursor
    } while ($pr.reviewThreads.pageInfo.hasNextPage)

    [pscustomobject]@{ PullRequest = $pr; Threads = $threads }
}

$snapshot = Get-LiveThreads

if ($ResolveOutdated) {
    $mutation = @'
mutation($threadId:ID!) {
  resolveReviewThread(input:{threadId:$threadId}) {
    thread { id isResolved }
  }
}
'@

    $stale = @($snapshot.Threads | Where-Object { -not $_.isResolved -and $_.isOutdated })
    foreach ($thread in $stale) {
        $null = Invoke-GhJson -Arguments @('graphql') -Fields @{ threadId = $thread.id; query = $mutation }
    }
    $snapshot = Get-LiveThreads
}

$current = @($snapshot.Threads | Where-Object { -not $_.isResolved -and -not $_.isOutdated })
$outdated = @($snapshot.Threads | Where-Object { -not $_.isResolved -and $_.isOutdated })

$receipt = [ordered]@{
    repository = $Repository
    pullRequest = $PullRequest
    url = $snapshot.PullRequest.url
    head = $snapshot.PullRequest.headRefOid
    mergeable = $snapshot.PullRequest.mergeable
    mergeStateStatus = $snapshot.PullRequest.mergeStateStatus
    currentUnresolved = $current.Count
    outdatedUnresolved = $outdated.Count
    ready = ($current.Count -eq 0 -and $outdated.Count -eq 0 -and $snapshot.PullRequest.mergeable -ne 'CONFLICTING')
}

$receipt | ConvertTo-Json -Depth 10

if ($current.Count -gt 0) {
    Write-Error "Merge blocked: $($current.Count) current unresolved review thread(s). Fix them; do not auto-resolve them."
    exit 2
}
if ($outdated.Count -gt 0) {
    Write-Error "Merge blocked: $($outdated.Count) outdated unresolved review thread(s). Verify their superseding changes, then rerun with -ResolveOutdated."
    exit 3
}
if ($snapshot.PullRequest.mergeable -eq 'CONFLICTING') {
    Write-Error 'Merge blocked: content conflict with base branch.'
    exit 4
}

exit 0
