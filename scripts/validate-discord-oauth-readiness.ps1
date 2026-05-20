param(
    [string]$ReportPath = "reports/discord-oauth-readiness.md"
)

$ErrorActionPreference = "Stop"

$keys = @(
    "DISCORD_CLIENT_ID",
    "DISCORD_CLIENT_SECRET",
    "DISCORD_REDIRECT_URI",
    "DISCORD_GUILD_ID",
    "DISCORD_BOT_TOKEN"
)

New-Item -ItemType Directory -Force (Split-Path -Parent $ReportPath) | Out-Null

"# Discord OAuth Readiness Report" | Set-Content $ReportPath
"" | Add-Content $ReportPath
"Generated: $(Get-Date -Format o)" | Add-Content $ReportPath
"" | Add-Content $ReportPath

$failed = $false

foreach ($key in $keys) {
    $value = [Environment]::GetEnvironmentVariable($key)

    if ([string]::IsNullOrWhiteSpace($value)) {
        $failed = $true
        "- $key : MISSING" | Add-Content $ReportPath
        Write-Host "MISSING: $key"
    }
    elseif ($value -match "PASTE|REAL_|COPIED_|ACTUAL_|YOUR_|TOKEN_HERE|ROLE_ID") {
        $failed = $true
        "- $key : PLACEHOLDER" | Add-Content $ReportPath
        Write-Host "PLACEHOLDER: $key"
    }
    else {
        "- $key : SET" | Add-Content $ReportPath
        Write-Host "SET: $key"
    }
}

$clientId = [Environment]::GetEnvironmentVariable("DISCORD_CLIENT_ID")
$redirectUri = [Environment]::GetEnvironmentVariable("DISCORD_REDIRECT_URI")
$guildId = [Environment]::GetEnvironmentVariable("DISCORD_GUILD_ID")
$botToken = [Environment]::GetEnvironmentVariable("DISCORD_BOT_TOKEN")

"" | Add-Content $ReportPath
"## OAuth Authorize URL" | Add-Content $ReportPath
"" | Add-Content $ReportPath

if (-not [string]::IsNullOrWhiteSpace($clientId) -and -not [string]::IsNullOrWhiteSpace($redirectUri)) {
    $scope = [Uri]::EscapeDataString("identify email guilds.join")
    $encodedRedirect = [Uri]::EscapeDataString($redirectUri)
    $url = "https://discord.com/api/oauth2/authorize?client_id=$clientId&redirect_uri=$encodedRedirect&response_type=code&scope=$scope&state=REPLACE_WITH_STRIPE_CUSTOMER_ID"

    "Authorize URL template:" | Add-Content $ReportPath
    "" | Add-Content $ReportPath
    $url | Add-Content $ReportPath
    "" | Add-Content $ReportPath

    Write-Host "OAuth authorize URL template generated."
}
else {
    $failed = $true
    "Cannot generate authorize URL because client ID or redirect URI is missing." | Add-Content $ReportPath
}

"" | Add-Content $ReportPath
"## Bot Token API Test" | Add-Content $ReportPath
"" | Add-Content $ReportPath

if (-not [string]::IsNullOrWhiteSpace($botToken)) {
    try {
        $headers = @{ Authorization = "Bot $botToken" }

        $bot = Invoke-RestMethod `
            -Uri "https://discord.com/api/v10/users/@me" `
            -Headers $headers `
            -Method Get

        "Bot API: PASS" | Add-Content $ReportPath
        "Bot username: $($bot.username)" | Add-Content $ReportPath
        Write-Host "Bot API: PASS"

        try {
            $roles = Invoke-RestMethod `
                -Uri "https://discord.com/api/v10/guilds/$guildId/roles" `
                -Headers $headers `
                -Method Get

            "Guild roles API: PASS" | Add-Content $ReportPath
            "Role count: $($roles.Count)" | Add-Content $ReportPath
            Write-Host "Guild roles API: PASS"

            "" | Add-Content $ReportPath
            "## Role Names" | Add-Content $ReportPath
            "" | Add-Content $ReportPath

            $roles |
                Sort-Object position -Descending |
                ForEach-Object {
                    "- $($_.name)" | Add-Content $ReportPath
                }
        }
        catch {
            $failed = $true
            "Guild roles API: FAIL" | Add-Content $ReportPath
            "Reason: $($_.Exception.Message)" | Add-Content $ReportPath
            Write-Host "Guild roles API: FAIL"
        }
    }
    catch {
        $failed = $true
        "Bot API: FAIL" | Add-Content $ReportPath
        "Reason: $($_.Exception.Message)" | Add-Content $ReportPath
        Write-Host "Bot API: FAIL"
    }
}
else {
    $failed = $true
    "Bot API: SKIPPED; token missing." | Add-Content $ReportPath
}

"" | Add-Content $ReportPath
"## Result" | Add-Content $ReportPath
"" | Add-Content $ReportPath

if ($failed) {
    "FAILED: Discord OAuth/runtime setup is not complete." | Add-Content $ReportPath
    exit 1
}

"PASSED: Discord OAuth/runtime setup appears ready." | Add-Content $ReportPath
exit 0
