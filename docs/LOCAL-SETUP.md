# Local Development Setup - JPV-OS Access Gateway

## Prerequisites

- **.NET 8 SDK** (or later) - [Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- **Git** - [Download](https://git-scm.com/)
- **A code editor** (Visual Studio Code, Visual Studio, or Rider)
- **Windows, macOS, or Linux** - No WSL required

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/JayPVentures-LLC/jpv-os-access-gateway.git
cd jpv-os-access-gateway
```

### 2. Restore Dependencies

```bash
dotnet restore JPVOS.sln
```

### 3. Build the Project

```bash
dotnet build JPVOS.sln -c Release
```

Expected output: `Build succeeded. 0 Warning(s)`

### 4. Run the Development Server

#### Using PowerShell (Windows)
```powershell
cd src/JPVOS
pwsh ./Run-BlazorApp.ps1
```

#### Using Bash (macOS/Linux)
```bash
cd src/JPVOS
dotnet run
```

#### Using Visual Studio
1. Open `JPVOS.sln`
2. Right-click "JPVOS" project → Set as Startup Project
3. Press F5 or click Debug

#### Using VS Code
1. Open the folder in VS Code
2. Install C# extension (if not already installed)
3. Open Terminal
4. Run: `dotnet run --project src/JPVOS/JPVOS.csproj`

The application will start at: `https://localhost:7232` (or similar)

## Project Structure

```
jpv-os-access-gateway/
├── src/
│   └── JPVOS/                          # Main Blazor Web App
│       ├── Components/                 # Razor components
│       │   ├── Pages/                  # Page components (routable)
│       │   ├── Layout/                 # Layout components
│       │   ├── App.razor               # Root component
│       │   └── ... (other components)
│       ├── Pages/                      # Legacy page routing
│       ├── Api/                        # API controllers
│       ├── Services/                   # Business logic services
│       ├── Models/                     # Data models
│       ├── wwwroot/                    # Static files
│       │   ├── css/                    # Stylesheets
│       │   ├── js/                     # JavaScript
│       │   ├── assets/                 # Images, icons, logos
│       │   └── app.css                 # Global styles
│       ├── Program.cs                  # Application entry point
│       ├── JPVOS.csproj                # Project configuration
│       └── appsettings.json            # Configuration settings
├── docs/                               # Documentation
│   ├── DESIGN-SYSTEM.md                # Design guidelines
│   ├── UI-COMPONENT-STRATEGY.md        # Component architecture
│   └── LOCAL-SETUP.md                  # This file
├── scripts/                            # Automation scripts
│   └── verify-ui.ps1                   # Build verification
├── JPVOS.sln                           # Solution file
└── README.md                           # Project overview
```

## Development Workflow

### Adding a New Page

1. Create a Razor file in `src/JPVOS/Components/Pages/`
   ```razor
   @page "/my-page"
   @page "/alternate-route"
   
   <div class="page-container">
       <h1>My Page Title</h1>
       <!-- Content here -->
   </div>
   
   @code {
       // Component logic here
   }
   ```

2. Add navigation link in `SiteHeader.razor`

3. Build and test: `dotnet build` and visit `https://localhost:7232/my-page`

### Adding a New Component

1. Create a Razor file in `src/JPVOS/Components/`
   ```razor
   @if (IsVisible)
   {
       <div class="my-component">
           <!-- Content here -->
       </div>
   }
   
   @code {
       [Parameter] public bool IsVisible { get; set; } = true;
       [Parameter] public RenderFragment? ChildContent { get; set; }
       [Parameter] public EventCallback OnAction { get; set; }
   }
   ```

2. Import in `Components/_Imports.razor` (if needed)

3. Use in other components:
   ```razor
   <MyComponent IsVisible="true" OnAction="HandleAction" />
   ```

### Styling

- **Global Styles**: Add to `wwwroot/app.css`
- **Component Styles**: Inline `<style>` blocks in `.razor` files (component-scoped)
- **CSS Variables**: Use design system colors defined in `app.css`

Example:
```css
.my-component {
    color: var(--color-silver);
    background: var(--color-navy);
    border: 1px solid var(--color-electric-blue);
}
```

### API Integration

API endpoints are in `src/JPVOS/Api/`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    [HttpGet("endpoint")]
    public IActionResult GetData()
    {
        return Ok(new { data = "value" });
    }
}
```

Call from Blazor:
```csharp
private async Task FetchData()
{
    var response = await Http.GetFromJsonAsync<MyData>("/api/my/endpoint");
}
```

## Configuration

### Stripe Integration (Optional)

Set environment variables:
```bash
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_PRICE_ID_COMMUNITY=price_...
STRIPE_PRICE_ID_VIP=price_...
```

Or in `appsettings.Development.json`:
```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

### Discord Integration (Optional)

Set environment variables:
```bash
DISCORD_CLIENT_ID=...
DISCORD_CLIENT_SECRET=...
DISCORD_BOT_TOKEN=...
```

## Troubleshooting

### Build Fails
```bash
# Clear build cache
dotnet clean JPVOS.sln

# Restore packages
dotnet restore JPVOS.sln

# Try again
dotnet build JPVOS.sln -c Release
```

### Port Already in Use
The default port is 7232. To use a different port:
```bash
dotnet run --project src/JPVOS/JPVOS.csproj -- --urls="https://localhost:7299"
```

### Blazor Hot Reload Not Working
- Ensure you're running in Debug mode
- Check that file changes are being saved
- Restart the development server if needed

### Package Restore Issues
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore
dotnet restore JPVOS.sln --force
```

## Verification

Run the verification script to check the build and codebase:

```powershell
# Windows/PowerShell
pwsh -ExecutionPolicy Bypass -File scripts/verify-ui.ps1

# Or on the command line
dotnet build JPVOS.sln -c Release
```

Expected output:
- Build succeeds without errors
- No public-facing banned terms detected
- All pages compile and route correctly

## Next Steps

1. **Review Design System**: Read `docs/DESIGN-SYSTEM.md`
2. **Understand Components**: Read `docs/UI-COMPONENT-STRATEGY.md`
3. **Check Current Pages**: Visit the running site and explore
4. **Make Changes**: Edit components and see hot reload in action
5. **Test Everything**: Build in Release mode before committing

## Getting Help

- **Build Errors**: Check the error message and file path indicated
- **Component Issues**: Verify parameter names match Razor syntax
- **Routing**: Ensure `@page` directives are correct
- **Styling**: Check browser DevTools for CSS conflicts
- **API Errors**: Check browser Network tab and server logs

## Additional Resources

- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Razor Syntax Reference](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor)
- [CSS Grid Guide](https://css-tricks.com/snippets/css/complete-guide-grid/)

---

Happy coding! 🚀
