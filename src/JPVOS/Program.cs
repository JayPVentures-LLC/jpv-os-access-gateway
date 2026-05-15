
using JPVOS.Components;
using JPVOS.Services;


var builder = WebApplication.CreateBuilder(args);

// Stripe
Stripe.StripeConfiguration.ApiKey = builder.Configuration["STRIPE_SECRET_KEY"];

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddSingleton<EntitlementService>();
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

app.UseStaticFiles();
app.UseAntiforgery();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API endpoints
app.MapControllers();

// Health check endpoint for Azure monitoring
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

