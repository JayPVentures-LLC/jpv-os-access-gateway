
using JPVOS.Components;
using JPVOS.Services;


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

// Add security headers
app.UseSecurityHeaders();

app.UseStaticFiles();
app.UseAntiforgery();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API endpoints
app.MapControllers();

// Health check endpoint for Azure monitoring
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

/// <summary>
/// Security headers middleware extension that adds baseline web security headers.
/// </summary>
static class SecurityHeadersExtensions
{
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Content-Security-Policy: Prevents XSS attacks while allowing Blazor functionality
            // - default-src 'self': Restrict to same origin by default
            // - script-src 'self' 'unsafe-inline' 'unsafe-eval': Allow inline scripts for Blazor
            // - style-src 'self' 'unsafe-inline': Allow inline styles for Blazor
            // - img-src 'self' data: https:: Allow images from self, data URIs, and HTTPS
            context.Response.Headers.Add(
                "Content-Security-Policy",
                "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'self';"
            );

            // X-Content-Type-Options: Prevents MIME-sniffing attacks
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

            // Referrer-Policy: Controls what referrer information is shared with external sites
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            // Permissions-Policy: Controls which browser features and APIs can be used
            // Disables: accelerometer, ambient light sensor, autoplay, camera, geolocation, gyroscope, magnetometer, microphone, payment, usb, vr, xr, etc.
            context.Response.Headers.Add(
                "Permissions-Policy",
                "accelerometer=(), ambient-light-sensor=(), autoplay=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=(), vr=(), xr-spatial-tracking=()"
            );

            // X-Frame-Options: Prevents clickjacking attacks by controlling if page can be framed
            context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

            // X-XSS-Protection: Legacy XSS protection header (defense in depth)
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

            // Strict-Transport-Security is already set via UseHsts() in production
            // Additional security headers
            context.Response.Headers.Add("X-Content-Security-Policy", "default-src 'self'");

            await next();
        });

        return app;
    }
}

