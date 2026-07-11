[CmdletBinding()]
param(
    [string]$ProjectId = "jpv-nexus-production-502019"
)

$ErrorActionPreference = "Stop"

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is not installed or is not on PATH."
    }
}

Require-Command "gcloud"
Require-Command "firebase"

Write-Host "Verifying authenticated Google Cloud identity..."
gcloud auth list --filter=status:ACTIVE --format="value(account)" | ForEach-Object {
    if (-not $_) { throw "No active gcloud identity. Run: gcloud auth login" }
    Write-Host "Active identity: $_"
}

Write-Host "Verifying project access: $ProjectId"
$resolvedProject = gcloud projects describe $ProjectId --format="value(projectId)"
if ($resolvedProject -ne $ProjectId) {
    throw "Resolved project '$resolvedProject' does not match required project '$ProjectId'."
}

Write-Host "Verifying billing association..."
$billingJson = gcloud beta billing projects describe $ProjectId --format=json | ConvertFrom-Json
if (-not $billingJson.billingEnabled) {
    throw "Billing is not enabled for $ProjectId. No Firebase provisioning should proceed."
}
if (-not $billingJson.billingAccountName) {
    throw "No billing account is linked to $ProjectId."
}

Write-Host "Billing enabled: $($billingJson.billingEnabled)"
Write-Host "Billing account resource: $($billingJson.billingAccountName)"

Write-Host "Verifying Firebase CLI project resolution..."
$firebaseProjects = firebase projects:list --json | ConvertFrom-Json
$match = @($firebaseProjects.result | Where-Object { $_.projectId -eq $ProjectId })
if ($match.Count -ne 1) {
    throw "Firebase CLI cannot resolve exactly one project with ID $ProjectId."
}

Write-Host "PASS: project identity, billing association, and Firebase visibility verified."
Write-Host "No production resources were created or modified."
