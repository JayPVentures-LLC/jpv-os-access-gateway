param(
  [Parameter(Mandatory = $true)][string]$GitHubUsername,
  [Parameter(Mandatory = $true)][string]$PersonalAccessToken,
  [Parameter(Mandatory = $true)][string]$Owner,
  [Parameter(Mandatory = $true)][string]$Repo,
  [Parameter(Mandatory = $true)][string]$Branch
)

$ErrorActionPreference = "Stop"

$originalUrl = git remote get-url origin
$newUrl = "https://$GitHubUsername:$PersonalAccessToken@github.com/$Owner/$Repo.git"

Write-Host "Temporarily setting remote URL with PAT..."
git remote set-url origin $newUrl

Write-Host "Pushing branch $Branch..."
git push --set-upstream origin $Branch

Write-Host "Restoring original remote URL..."
git remote set-url origin $originalUrl

Write-Host "Done. For security, clear your shell history if needed."
