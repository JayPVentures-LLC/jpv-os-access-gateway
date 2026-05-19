
using JPVOS.Components;
using JPVOS.Services;


var builder = WebApplication.CreateBuilder(args);

// Stripe
Stripe.StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

// Configure entitlements service based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEntitlementService, InMemoryEntitlementService>();
}
else
{
    // Production: Configure persistent SQLite storage
    var dbPath = builder.Configuration["ENTITLEMENTS_DB_PATH"];
    
    // Default database paths for common deployment scenarios
    // Priority: Explicit config > Azure App Service > Container > Fallback
    if (string.IsNullOrWhiteSpace(dbPath))
    {
        // Azure App Service: Use /home directory for persistent storage
        // WEBSITE_INSTANCE_ID is set by Azure App Service runtime
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
        {
            dbPath = Path.Combine("/home", "entitlements.db");
        }
        // Docker/Container: Use /app/data directory if it exists or container environment is detected
        // This allows for explicit volume mounts in containerized deployments
        else if (Directory.Exists("/app/data") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        {
            dbPath = Path.Combine("/app/data", "entitlements.db");
        }
        // Fallback: Use application directory
        // Note: Not recommended for production as app directory may not persist across updates
        else
        {
            dbPath = Path.Combine(AppContext.BaseDirectory, "entitlements.db");
        }
    }

    // Register repository with logger support
    builder.Services.AddSingleton<IEntitlementRepository>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<SqliteEntitlementRepository>>();
        return new SqliteEntitlementRepository(dbPath, logger);
    });
    builder.Services.AddSingleton<IEntitlementService, PersistentEntitlementService>();
}

builder.Services.AddHttpClient();
builder.Services.AddSingleton<DiscordService>();
builder.Services.AddSingleton<WixCheckoutConfig>();



var app = builder.Build();
PeopleProtectionStartupGuard.Verify(app);

// Log database configuration after app is built
if (!app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Entitlements database configured for production deployment");
}

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

