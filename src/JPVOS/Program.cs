using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;
using Stripe;

using JPVOS.Components;
using JPVOS.Services;
using JPVOS.Services.SystemicAccess;
using JPVOS.Infrastructure.Stripe;

var builder = WebApplication.CreateBuilder(args);

var systemicAccessPolicyPath = Path.Combine(
    builder.Environment.ContentRootPath,
    ".jpv",
    "governance",
    "systemic-access-hygiene.json");
var systemicAccessPolicy = SystemicAccessPolicyLoader.LoadAndValidate(systemicAccessPolicyPath);

StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-JPV.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login?denied=1";
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FounderOnly", policy => policy.RequireRole("Founder"));
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("FounderLogin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEntitlementService, InMemoryEntitlementService>();
}
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

builder.Services.AddSingleton(systemicAccessPolicy);
builder.Services.AddSingleton<SystemicAccessClassifier>();
builder.Services.AddSingleton<SystemicAccessRuntimeState>();
builder.Services.AddSingleton(sp => new SystemicAccessAuditStore(
    Path.Combine(AppContext.BaseDirectory, "audit", "systemic-access-receipts.jsonl")));
builder.Services.AddSingleton<SystemicAccessReconciler>();
builder.Services.AddHostedService<SystemicAccessReconciliationService>();

var app = builder.Build();
PeopleProtectionStartupGuard.Verify(app);
app.Services.GetRequiredService<SystemicAccessRuntimeState>().MarkPolicyLoaded();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapControllers();
app.MapGet("/health", (IConfiguration config, SystemicAccessRuntimeState systemicState) => Results.Ok(new
{
    status = systemicState.LastError is null ? "healthy" : "degraded",
    identity = new
    {
        founderProvisioned = !string.IsNullOrWhiteSpace(config["JPV_FOUNDER_ID"]) &&
                             !string.IsNullOrWhiteSpace(config["JPV_FOUNDER_ACCESS_KEY_SHA256"]),
        session = "cookie",
        founderProfile = "/profile",
        founderWorkspace = "/workspace"
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
    timestamp = DateTime.UtcNow
}));

app.Run();
