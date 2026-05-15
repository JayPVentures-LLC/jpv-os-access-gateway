# Security Headers

## Overview

JPV-OS Access Gateway implements a baseline of web security headers to protect against common web vulnerabilities while maintaining compatibility with Blazor interactive components.

All security headers are applied globally to HTTP responses through a middleware extension in the application pipeline.

## Implemented Headers

### Content-Security-Policy (CSP)

**Policy:**
```
default-src 'self'; 
script-src 'self' 'unsafe-inline' 'unsafe-eval'; 
style-src 'self' 'unsafe-inline'; 
img-src 'self' data: https:; 
font-src 'self'; 
connect-src 'self'; 
frame-ancestors 'self';
```

**Purpose:** Prevents Cross-Site Scripting (XSS) attacks by controlling which resources can be loaded.

**Blazor Compatibility:** 
- Uses `'unsafe-inline'` and `'unsafe-eval'` for scripts and styles to support Blazor's server-side rendering and interactive components
- Allows data URIs for images to support embedded resources
- HTTPS images allowed for external resources

**Directive Breakdown:**
- `default-src 'self'` - Restrict to same origin by default
- `script-src 'self' 'unsafe-inline' 'unsafe-eval'` - Allow inline scripts and eval for Blazor
- `style-src 'self' 'unsafe-inline'` - Allow inline styles for Blazor component styling
- `img-src 'self' data: https:` - Allow images from self, data URIs, and HTTPS
- `font-src 'self'` - Fonts from same origin only
- `connect-src 'self'` - API calls to same origin only
- `frame-ancestors 'self'` - Allow framing only from same origin

### X-Content-Type-Options

**Value:** `nosniff`

**Purpose:** Prevents MIME-type sniffing attacks. Instructs browsers to honor the declared content type and not attempt to determine the type by inspecting the content.

### Referrer-Policy

**Value:** `strict-origin-when-cross-origin`

**Purpose:** Controls what referrer information is sent with requests to external sites.

**Behavior:**
- Full URL is sent for same-origin requests
- Only origin (protocol, host, port) is sent for cross-origin requests
- No referrer is sent for less-secure (HTTP) destinations

### Permissions-Policy

**Value:**
```
accelerometer=(), 
ambient-light-sensor=(), 
autoplay=(), 
camera=(), 
geolocation=(), 
gyroscope=(), 
magnetometer=(), 
microphone=(), 
payment=(), 
usb=(), 
vr=(), 
xr-spatial-tracking=()
```

**Purpose:** Controls which browser features and APIs can be used within the application. Denies all listed features by default to improve privacy and security.

**Disabled Features:**
- Motion/orientation sensors (accelerometer, gyroscope, magnetometer)
- Environmental sensors (ambient light sensor)
- Media capture (camera, microphone)
- Geolocation
- Payment request API
- USB API
- XR/VR capabilities (vr, xr-spatial-tracking)
- Autoplay

### X-Frame-Options

**Value:** `SAMEORIGIN`

**Purpose:** Prevents clickjacking attacks by controlling whether the page can be framed.

**Behavior:** The page can only be framed by pages from the same origin.

### X-XSS-Protection

**Value:** `1; mode=block`

**Purpose:** Legacy XSS protection header (primarily for older browsers). Modern browsers rely on CSP, but this provides defense in depth.

## Implementation Details

### Location
Security headers are implemented in `src/JPVOS/Program.cs` as a middleware extension method `UseSecurityHeaders()`.

### Application Point
The middleware is registered in the HTTP request pipeline after routing configuration but before Razor components are mapped:

```csharp
app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseStaticFiles();
```

### Scope
Security headers are applied to **all HTTP responses** in the application, including:
- Razor component pages
- API endpoints
- Static files
- Health check endpoints

## Development Mode

In development mode (`app.Environment.IsDevelopment()`), the security headers are still applied. The HTTPS redirect is disabled for easier local development, but the headers are not conditional on the environment.

## Blazor Compatibility

These headers have been carefully configured to maintain full Blazor compatibility:

1. **Inline Scripts & Styles:** Blazor requires inline script execution and style injection. CSP allows these with `'unsafe-inline'` and `'unsafe-eval'` for script-src.

2. **Eval Support:** Blazor may use eval in certain scenarios, so `'unsafe-eval'` is included in script-src.

3. **Frame Ancestors:** Set to `'self'` to allow internal framing if needed by Blazor components.

4. **No Breaking Restrictions:** The policy doesn't restrict WebSocket connections (included in connect-src 'self') or shadow DOM usage required by Blazor.

### Testing After Updates

If you need to modify these headers:

1. Run `dotnet build JPVOS.sln -c Release` to verify compilation
2. Test Blazor interactive components in a browser
3. Check browser console for CSP violations
4. Verify network requests are not blocked

## Future Improvements

Potential enhancements for future iterations:

1. **Stricter CSP in Production:** Remove `'unsafe-eval'` if Blazor functionality allows after testing
2. **CSP Nonce Implementation:** Use CSP nonces for inline scripts instead of `'unsafe-inline'`
3. **Configuration-Based Headers:** Move header values to appsettings.json for environment-specific policies
4. **Subresource Integrity (SRI):** Add SRI validation for external CDN resources if used
5. **Report-Only Mode:** Implement CSP report-only mode with violation logging for monitoring

## References

- [MDN: Content-Security-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy)
- [MDN: X-Content-Type-Options](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options)
- [MDN: Referrer-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy)
- [MDN: Permissions-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Permissions-Policy)
- [MDN: X-Frame-Options](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options)
- [OWASP: Secure Headers Project](https://owasp.org/www-project-secure-headers/)
- [Blazor Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/)
