param(
    [Parameter(Mandatory=$true)]
    [string]$RedirectUri,

    [Parameter(Mandatory=$true)]
    [string]$MemberAccessRoleId,

    [Parameter(Mandatory=$true)]
    [string]$VipVentureRoleId,

    [Parameter(Mandatory=$true)]
    [string]$CreatorLaneRoleId,

    [Parameter(Mandatory=$true)]
    [string]$OperatorRoleId,

    [Parameter(Mandatory=$true)]
    [string]$EnterpriseRoleId,

    [ValidateSet("User","Process")]
    [string]$Scope = "User"
)

$ErrorActionPreference = "Stop"

$values = @{
    DISCORD_REDIRECT_URI = $RedirectUri
    DISCORD_ROLE_MEMBER_ACCESS = $MemberAccessRoleId
    DISCORD_ROLE_VIP_VENTURE = $VipVentureRoleId
    DISCORD_ROLE_CREATOR_LANE = $CreatorLaneRoleId
    DISCORD_ROLE_OPERATOR = $OperatorRoleId
    DISCORD_ROLE_ENTERPRISE = $EnterpriseRoleId
}

foreach ($item in $values.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace($item.Value)) {
        throw "$($item.Key) cannot be empty."
    }

    if ($item.Value -in @("YOUR_REDIRECT_URI","ROLE_ID","REAL_ROLE_ID")) {
        throw "$($item.Key) still contains a placeholder value."
    }

    [Environment]::SetEnvironmentVariable($item.Key, $item.Value, $Scope)
}

Write-Host "Discord runtime environment values were set for scope: $Scope"
Write-Host "No secret or role values were printed."
