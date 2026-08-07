using Stripe;

using JPVOS.Components;
using JPVOS.Services;
using JPVOS.Infrastructure.Stripe;


var builder = WebApplication.CreateBuilder(args);

// Stripe
Stripe.StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
builder.Services.AddSingleton<WixCheckoutConfig>();

// Governed Stripe checkout path. Pricing loader fails closed on any map that
// does not match the active JPV-OS canonical pricing authority.
builder.Services.AddSingleton<StripePricingLoader>();
builder.Services.AddSingleton<StripeCheckoutService>();

builder.Services.AddSingleton<StripeWebhookEventStore>();
builder.Services.AddSingleton<StripeSubscriptionAuditStore>();
builder.Services.AddSingleton<JPVOS.Infrastructure.Discord.DiscordRoleSyncAuditStore>();

var app = builder.Build();
PeopleProtectionStartupGuard.Verify(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API endpoints
app.MapControllers();

// Health check endpoint for Azure monitoring
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
