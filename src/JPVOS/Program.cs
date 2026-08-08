using Microsoft.AspNetCore.Authentication.Cookies;
using Stripe;

using JPVOS.Components;
using JPVOS.Services;
using JPVOS.Infrastructure.Stripe;

var builder = WebApplication.CreateBuilder(args);

// Stripe
StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-JPV.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
}
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DiscordService>();

// Governed Stripe checkout path. Pricing loader fails closed on any map that
// does not match the active JPV-OS canonical pricing authority.
builder.Services.AddSingleton<StripePricingLoader>();
builder.Services.AddSingleton<StripeCheckoutService>();

builder.Services.AddSingleton<StripeWebhookEventStore>();
builder.Services.AddSingleton<StripeSubscriptionAuditStore>();
builder.Services.AddSingleton<JPVOS.Infrastructure.Discord.DiscordRoleSyncAuditStore>();

var app = builder.Build();
PeopleProtectionStartupGuard.Verify(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
