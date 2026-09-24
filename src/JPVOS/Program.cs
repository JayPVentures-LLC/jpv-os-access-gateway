using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;
using Stripe;

using JPVOS.Components;
using JPVOS.Services;
using JPVOS.Services.SystemicAccess;
using JPVOS.Services.Reciprocity;
using JPVOS.Services.PrivilegedActions;
using JPVOS.Services.GitHubOrgMutation;
using JPVOS.Services.Attention;
using JPVOS.Services.ClaimsEvidence;
using JPVOS.Services.AgencySafety;
using JPVOS.Services.ProposalExecution;
using JPVOS.Infrastructure.Stripe;

var builder = WebApplication.CreateBuilder(args);

var systemicAccessPolicyPath = Path.Combine(builder.Environment.ContentRootPath, ".jpv", "governance", "systemic-access-hygiene.json");
var systemicAccessPolicy = SystemicAccessPolicyLoader.LoadAndValidate(systemicAccessPolicyPath);

var privilegedActionPolicyPath = Path.Combine(
    builder.Environment.ContentRootPath,
    ".jpv",
    "governance",
    "privileged-action-governance.json");
var privilegedActionPolicy = PrivilegedActionPolicyLoader.LoadAndValidate(privilegedActionPolicyPath);

var githubAppOptions = GitHubAppAuthenticationOptions.FromConfiguration(builder.Configuration);
var agencySafetyPolicyPath = Path.Combine(builder.Environment.ContentRootPath, ".jpv", "governance", "ai-agency-safety.json");
var agencySafetyPolicy = AgencySafetyPolicyLoader.LoadAndValidate(agencySafetyPolicyPath);
var claimsDataDir = builder.Configuration["JPV_CLAIMS_DATA_DIR"];
if (string.IsNullOrWhiteSpace(claimsDataDir))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("JPV_CLAIMS_DATA_DIR is required outside Development and must point to writable persistent storage.");
    claimsDataDir = Path.Combine(Path.GetTempPath(), "jpv-os-claims");
}
Directory.CreateDirectory(claimsDataDir);
var agencySafetyDataDir = builder.Configuration["JPV_AGENCY_SAFETY_DATA_DIR"];
if (string.IsNullOrWhiteSpace(agencySafetyDataDir))
    agencySafetyDataDir = Path.Combine(claimsDataDir, "agency-safety");
Directory.CreateDirectory(agencySafetyDataDir);
var claimsDataProtectionDir = Path.Combine(claimsDataDir, "data-protection-keys");
Directory.CreateDirectory(claimsDataProtectionDir);

var proposalDataDir = ProposalStoragePathResolver.Resolve(
    builder.Configuration["JPV_PROPOSAL_DATA_DIR"],
    builder.Configuration["JPV_OUTBOUND_DATA_DIR"],
    builder.Environment.IsDevelopment());
Directory.CreateDirectory(proposalDataDir);
var proposalBootstrapManifestPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "governance",
    "proposals",
    "JPV-PROPOSAL-REGISTRY.bootstrap.json");

var reciprocityDataDir = builder.Configuration["JPV_RECIPROCITY_DATA_DIR"];
if (string.IsNullOrWhiteSpace(reciprocityDataDir))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("JPV_RECIPROCITY_DATA_DIR must point to writable persistent storage in production.");
    reciprocityDataDir = Path.Combine(Path.GetTempPath(), "jpv-os-reciprocity");
}
Directory.CreateDirectory(reciprocityDataDir);

var reciprocityLedgerPath = builder.Configuration["JPV_RECIPROCITY_LEDGER_PATH"];
if (string.IsNullOrWhiteSpace(reciprocityLedgerPath))
    reciprocityLedgerPath = Path.Combine(reciprocityDataDir, "reciprocity-ledger.json");
var reciprocityAuditPath = Path.Combine(reciprocityDataDir, "reciprocity-access-receipts.jsonl");

StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "__Host-JPV.Auth"; options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict; options.SlidingExpiration = true; options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.LoginPath = "/login"; options.AccessDeniedPath = "/login?denied=1";
});
builder.Services.AddAuthorization(options => options.AddPolicy("FounderOnly", policy => policy.RequireRole("Founder")));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("FounderLogin", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("ClaimsEvidencePublic", httpContext => RateLimitPartition.GetTokenBucketLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 12,
            TokensPerPeriod = 4,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState(); builder.Services.AddControllers();
if (builder.Environment.IsDevelopment()) builder.Services.AddSingleton<IEntitlementService, InMemoryEntitlementService>();
else
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "entitlements.db");
    builder.Services.AddSingleton<IEntitlementRepository>(new SqliteEntitlementRepository(dbPath));
    builder.Services.AddSingleton<IEntitlementService, PersistentEntitlementService>();
    builder.Services.AddSingleton<EntitlementAccessProvider>();
    builder.Services.AddSingleton<ISystemicAccessInventorySource>(sp => sp.GetRequiredService<EntitlementAccessProvider>());
    builder.Services.AddSingleton<ISystemicAccessActionProvider>(sp => sp.GetRequiredService<EntitlementAccessProvider>());
}
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DiscordService>();
builder.Services.AddSingleton<StripePricingLoader>();
builder.Services.AddSingleton<StripeCheckoutService>();
builder.Services.AddSingleton<StripeWebhookEventStore>();
builder.Services.AddSingleton<StripeSubscriptionAuditStore>();
builder.Services.AddSingleton<JPVOS.Infrastructure.Discord.DiscordRoleSyncAuditStore>();
builder.Services.AddSingleton<ProductionAttentionAdmissionService>();
builder.Services.AddSingleton(agencySafetyPolicy);
builder.Services.AddSingleton(new FileAgencyDenialStateStore(Path.Combine(agencySafetyDataDir, "target-denials.json")));
builder.Services.AddSingleton(new FileAgencySecurityTestingGrantStore(Path.Combine(agencySafetyDataDir, "security-testing-grants.json")));
builder.Services.AddSingleton<AgencySafetyAuthorizer>();

builder.Services.AddDataProtection()
    .SetApplicationName("JPVOS.ClaimsEvidence")
    .PersistKeysToFileSystem(new DirectoryInfo(claimsDataProtectionDir));
builder.Services.AddSingleton<ITrackingCredentialService, TrackingCredentialService>();
builder.Services.AddSingleton<IEvidenceBlobStore, DisabledEvidenceBlobStore>();
builder.Services.AddSingleton<ClaimsEvidenceProjector>();
builder.Services.AddSingleton<IClaimsEvidenceEventStore>(sp => new SqliteClaimsEvidenceEventStore(
    Path.Combine(claimsDataDir, "claims-evidence.db"),
    sp.GetRequiredService<IDataProtectionProvider>()));
builder.Services.AddSingleton<IClaimsEvidenceService, ClaimsEvidenceService>();

builder.Services.AddSingleton<ProposalLifecycleValidator>();
builder.Services.AddSingleton<IProposalEventStore>(_ => new SqliteProposalEventStore(Path.Combine(proposalDataDir, "proposal-execution.db")));
builder.Services.AddSingleton<IProposalRegistryService, ProposalRegistryService>();
builder.Services.AddSingleton<ProposalBootstrapImporter>();

builder.Services.AddJpvReciprocityGate(reciprocityLedgerPath, reciprocityAuditPath);

builder.Services.AddSingleton(systemicAccessPolicy);
builder.Services.AddSingleton<SystemicAccessClassifier>();
builder.Services.AddSingleton<SystemicAccessRuntimeState>();
builder.Services.AddSingleton(sp => new SystemicAccessAuditStore(
    Path.Combine(AppContext.BaseDirectory, "audit", "systemic-access-receipts.jsonl")));
builder.Services.AddSingleton<SystemicAccessReconciler>();
builder.Services.AddHostedService<SystemicAccessReconciliationService>();

builder.Services.AddSingleton(privilegedActionPolicy);
builder.Services.AddSingleton<PrivilegedActionAuthorizer>();
builder.Services.AddSingleton<BreakGlassAuthorizationService>();
builder.Services.AddSingleton(sp => new PrivilegedActionAuditStore(
    Path.Combine(AppContext.BaseDirectory, "audit", "privileged-action-receipts.jsonl")));
builder.Services.AddSingleton<PrivilegedActionExecutionService>();

builder.Services.AddSingleton(githubAppOptions);
builder.Services.AddHttpClient<IGitHubAppTokenProvider, GitHubAppTokenProvider>();
builder.Services.AddHttpClient<IGitHubOrganizationClient, GitHubOrganizationClient>();
builder.Services.AddHttpClient<IGitHubCanonicalTopologySource, GitHubCanonicalTopologyLoader>();
builder.Services.AddSingleton(sp => new GitHubOrgMutationReceiptStore(
    Path.Combine(AppContext.BaseDirectory, "audit", "github-org-mutation-receipts.jsonl")));
builder.Services.AddSingleton<GitHubOrganizationReconciler>();
builder.Services.AddSingleton<GitHubOrgMutationRuntimeState>();
builder.Services.AddHostedService<GitHubOrgMutationHostedService>();

var app = builder.Build();
PeopleProtectionStartupGuard.Verify(app);
app.Services.GetRequiredService<SystemicAccessRuntimeState>().MarkPolicyLoaded();
_ = app.Services.GetRequiredService<ProductionAttentionAdmissionService>();
_ = app.Services.GetRequiredService<ReciprocityLedgerStore>();
await app.Services.GetRequiredService<ProposalBootstrapImporter>().ImportAsync(proposalBootstrapManifestPath, CancellationToken.None);

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error", createScopeForErrors: true); app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization(); app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode(); app.MapControllers();
app.MapGet("/health", (IConfiguration config, SystemicAccessRuntimeState systemicState, GitHubOrgMutationRuntimeState githubState, ProductionAttentionAdmissionService attentionGate) => Results.Ok(new
{
    status = systemicState.LastError is null && githubState.LastError is null ? "healthy" : "degraded",
    identity = new
    {
        founderProvisioned = !string.IsNullOrWhiteSpace(config["JPV_FOUNDER_ID"]) &&
                             !string.IsNullOrWhiteSpace(config["JPV_FOUNDER_ACCESS_KEY_SHA256"]),
        session = "cookie",
        founderProfile = "/profile",
        founderWorkspace = "/workspace"
    },
    claimsEvidence = new { registered = true, persistentStorageRequired = !app.Environment.IsDevelopment(), binaryEvidenceEnabled = false },
    proposalExecution = new { registered = true, persistentStorageRequired = !app.Environment.IsDevelopment(), publicReadRequiresReleaseReview = true },
    privilegedActions = new
    {
        policyLoaded = true,
        phishingResistantStepUpRequired = privilegedActionPolicy.Invariants.PhishingResistantStepUpRequired,
        voiceOnlyPermitted = privilegedActionPolicy.Invariants.VoiceOnlyPermitted,
        providerReadbackRequired = privilegedActionPolicy.Invariants.ProviderReadbackRequired,
        breakGlassMaxTtlMinutes = privilegedActionPolicy.Invariants.BreakGlassMaxTtlMinutes
    },
    systemicAccess = new
    {
        policyLoaded = systemicState.PolicyLoaded,
        lastEvaluated = systemicState.LastSummary?.Evaluated,
        lastActionsApplied = systemicState.LastSummary?.ActionsApplied,
        lastFailures = systemicState.LastSummary?.Failures,
        lastCompletedAtUtc = systemicState.LastSummary?.CompletedAtUtc,
        lastError = systemicState.LastError
    },
    githubOrganizationMutation = new
    {
        configured = githubState.Configured,
        canonicalPolicyLoaded = githubState.CanonicalPolicyLoaded,
        lastReconciliationState = githubState.LastReconciliationState?.ToString(),
        lastReceiptId = githubState.LastReceiptId,
        lastError = githubState.LastError
    },
    productionAttentionAdmission = new
    {
        registered = attentionGate is not null,
        mode = "fail-closed"
    },
    agencySafety = new { policyLoaded = true, authoritativeDenialState = true, stickyThirdPartyDenial = agencySafetyPolicy.ThirdPartyAuthorizationDenialSticky },
    reciprocity = new
    {
        registered = true,
        mode = "synchronous-resource-admission",
        watcher = false,
        persistentData = !builder.Environment.IsDevelopment()
    },
    timestamp = DateTime.UtcNow
}));
app.Run();
