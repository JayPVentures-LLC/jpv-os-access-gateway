# Final Launch Curation Report

Status: **PASS**

## Build Result

Exit code: 0

```text
  Determining projects to restore...
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Dapper (>= 2.1.38) but Dapper 2.1.38 was not found. Dapper 2.1.42 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Stripe.net (>= 47.18.0) but Stripe.net 47.18.0 was not found. Stripe.net 48.0.0 was resolved instead.
  All projects are up-to-date for restore.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Dapper (>= 2.1.38) but Dapper 2.1.38 was not found. Dapper 2.1.42 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Stripe.net (>= 47.18.0) but Stripe.net 47.18.0 was not found. Stripe.net 48.0.0 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Components/Pages/Admin.razor(16,13): warning RZ10012: Found markup element with unexpected name 'InfoCard'. If this is intended to be a component, add a @using directive for its namespace. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Components/Pages/Admin.razor(19,13): warning RZ10012: Found markup element with unexpected name 'InfoCard'. If this is intended to be a component, add a @using directive for its namespace. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Components/Pages/Admin.razor(22,13): warning RZ10012: Found markup element with unexpected name 'InfoCard'. If this is intended to be a component, add a @using directive for its namespace. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(29,118): warning CS8604: Possible null reference argument for parameter 'stringToEscape' in 'string Uri.EscapeDataString(string stringToEscape)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(43,137): warning CS8604: Possible null reference argument for parameter 'json' in 'Session? JsonSerializer.Deserialize<Session>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(44,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(64,119): warning CS8604: Possible null reference argument for parameter 'json' in 'Invoice? JsonSerializer.Deserialize<Invoice>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(65,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(77,119): warning CS8604: Possible null reference argument for parameter 'json' in 'Invoice? JsonSerializer.Deserialize<Invoice>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(78,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(89,125): warning CS8604: Possible null reference argument for parameter 'json' in 'Subscription? JsonSerializer.Deserialize<Subscription>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(90,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(103,125): warning CS8604: Possible null reference argument for parameter 'json' in 'Subscription? JsonSerializer.Deserialize<Subscription>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(104,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(44,33): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(45,37): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(48,36): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(68,33): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(75,55): warning CS8604: Possible null reference argument for parameter 'discordUserId' in 'Task DiscordService.AssignRoleAsync(string discordUserId, string roleId)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/usr/share/dotnet/sdk/10.0.201/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(308,5): warning NETSDK1206: Found version-specific or distribution-specific runtime identifier(s): alpine-arm, alpine-arm64, alpine-x64. Affected libraries: SQLitePCLRaw.lib.e_sqlite3. In .NET 8.0 and higher, assets for version-specific and distribution-specific runtime identifiers will not be found by default. See https://aka.ms/dotnet/rid-usage for details. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
  JPVOS -> /home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/bin/Debug/net8.0/JPVOS.dll

Build succeeded.

/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Dapper (>= 2.1.38) but Dapper 2.1.38 was not found. Dapper 2.1.42 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Stripe.net (>= 47.18.0) but Stripe.net 47.18.0 was not found. Stripe.net 48.0.0 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Dapper (>= 2.1.38) but Dapper 2.1.38 was not found. Dapper 2.1.42 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj : warning NU1603: JPVOS depends on Stripe.net (>= 47.18.0) but Stripe.net 47.18.0 was not found. Stripe.net 48.0.0 was resolved instead.
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Components/Pages/Admin.razor(16,13): warning RZ10012: Found markup element with unexpected name 'InfoCard'. If this is intended to be a component, add a @using directive for its namespace. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Components/Pages/Admin.razor(19,13): warning RZ10012: Found markup element with unexpected name 'InfoCard'. If this is intended to be a component, add a @using directive for its namespace. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Components/Pages/Admin.razor(22,13): warning RZ10012: Found markup element with unexpected name 'InfoCard'. If this is intended to be a component, add a @using directive for its namespace. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(29,118): warning CS8604: Possible null reference argument for parameter 'stringToEscape' in 'string Uri.EscapeDataString(string stringToEscape)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(43,137): warning CS8604: Possible null reference argument for parameter 'json' in 'Session? JsonSerializer.Deserialize<Session>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(44,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(64,119): warning CS8604: Possible null reference argument for parameter 'json' in 'Invoice? JsonSerializer.Deserialize<Invoice>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(65,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(77,119): warning CS8604: Possible null reference argument for parameter 'json' in 'Invoice? JsonSerializer.Deserialize<Invoice>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(78,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(89,125): warning CS8604: Possible null reference argument for parameter 'json' in 'Subscription? JsonSerializer.Deserialize<Subscription>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(90,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(103,125): warning CS8604: Possible null reference argument for parameter 'json' in 'Subscription? JsonSerializer.Deserialize<Subscription>(string json, JsonSerializerOptions? options = null)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/StripeWebhookController.cs(104,34): warning CS8602: Dereference of a possibly null reference. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(44,33): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(45,37): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(48,36): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(68,33): warning CS8601: Possible null reference assignment. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/Api/DiscordOAuthController.cs(75,55): warning CS8604: Possible null reference argument for parameter 'discordUserId' in 'Task DiscordService.AssignRoleAsync(string discordUserId, string roleId)'. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
/usr/share/dotnet/sdk/10.0.201/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.targets(308,5): warning NETSDK1206: Found version-specific or distribution-specific runtime identifier(s): alpine-arm, alpine-arm64, alpine-x64. Affected libraries: SQLitePCLRaw.lib.e_sqlite3. In .NET 8.0 and higher, assets for version-specific and distribution-specific runtime identifiers will not be found by default. See https://aka.ms/dotnet/rid-usage for details. [/home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/JPVOS.csproj]
    24 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.30

```

## verify-ui Result

Exit code: 0

```text


```

## Banned Public Terms

None found.

## Placeholder / Weak Launch Copy

- placeholder => /home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/reports/final-launch-curation-report.md

## Missing Image References

None found.

## External Image References

None found.

## Public Hero / Founder / Background Assets Outside Approved Folder

None found.

## Bootstrap / Default UI Markers

- Bootstrap/default UI marker => /home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/reports/final-launch-curation-report.md

## First-Person Founder Voice Review

- Founder/first-person review => /home/runner/work/jpv-os-access-gateway/jpv-os-access-gateway/src/JPVOS/reports/final-launch-curation-report.md

## Preferred Public Terms

- orchestration
- routing
- governance
- operational integrity
- infrastructure authority
- validation layer
- alignment
- coordination
- structured execution
- access gateway

## Owner Review List

- Review Home hero imagery and headline.
- Review Pricing wording for public clarity.
- Review Access/Login wording for init and JPV-OS alignment.
- Review Partners page for infrastructure authority language.
- Review Ecosystem page for operational-layer language.
- Confirm approved production imagery is under wwwroot/assets/approved.

## Final Decision

Launch curation passed. Proceed to final visual review and deployment.
