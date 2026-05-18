# JPV-OS Access Gateway - Deployment Checklist

**Status**: Ready for Deployment

---

## Pre-Deployment Tasks

### Repository Verification
- [x] All `publish/` artifacts removed from git
- [x] `publish.zip` removed from git
- [x] `.gitignore` updated with `publish/`, `*.PublishProfile`, `*.pubxml`
- [x] No secrets committed to repository
- [x] Build succeeds: `dotnet build JPVOS.sln -c Release` (0 warnings, 0 errors)
- [x] All launch curation checks pass

### Code Quality
- [x] StripeWebhookController: No nullable warnings, proper null handling
- [x] DiscordOAuthController: No nullable warnings, proper exception throws
- [x] No banned public-facing terms (division, master, control)
- [x] Owner-approved copy present in all required pages
- [x] Navigation structure valid (4 primary + 1 CTA)

### Routes Available
- [x] `/` (Home page)
- [x] `/pricing` (Pricing page)
- [x] `/access` (Access routing page)
- [x] `/ecosystem` (Ecosystem page)
- [x] `/login` (Login page)
- [x] `/api/health` (Health endpoint)

### Visual Polish
- [x] Homepage hero centered with glow effect
- [x] Metric cards styled in 4-column grid
- [x] Access cards styled in 6-lane routing system
- [x] Footer readable with proper spacing
- [x] Mobile layout responsive
- [x] No raw unstyled HTML

### Security Configuration
- [x] `appsettings.Production.json` contains only logging config
- [x] `appsettings.Production.json` in `.gitignore`
- [x] No Stripe/Discord secrets in client-side code
- [x] No Stripe/Discord secrets in `.razor` or `.js` files
- [x] All secrets marked as server-side environment variables

---

## Azure App Service Configuration

### Before Deployment

1. **Create Resource Group** (if not exists)
   ```bash
   az group create --name jpvos-rg --location eastus
   ```

2. **Create App Service Plan**
   ```bash
   az appservice plan create \
     --name jpvos-plan \
     --resource-group jpvos-rg \
     --sku B2 \
     --is-linux
   ```

3. **Create Web App**
   ```bash
   az webapp create \
     --resource-group jpvos-rg \
     --plan jpvos-plan \
     --name jpvos-gateway \
     --runtime "dotnet:8.0"
   ```

4. **Configure Application Settings**
   ```bash
   az webapp config appsettings set \
     --resource-group jpvos-rg \
     --name jpvos-gateway \
     --settings \
       STRIPE_SECRET_KEY="sk_live_..." \
       STRIPE_WEBHOOK_SECRET="whsec_..." \
       STRIPE_PRICE_ENTERPRISE_ANNUAL="price_..." \
       STRIPE_PRICE_CUSTOM_IMPLEMENTATION="price_..." \
       DISCORD_CLIENT_ID="..." \
       DISCORD_CLIENT_SECRET="..." \
       DISCORD_BOT_TOKEN="..." \
       DISCORD_GUILD_ID="..." \
       DISCORD_ROLE_CUSTOM="..." \
       DISCORD_REDIRECT_URI="https://yourdomain.com/api/discord/oauth/callback"
   ```

5. **Enable HTTPS Only**
   ```bash
   az webapp update \
     --resource-group jpvos-rg \
     --name jpvos-gateway \
     --https-only
   ```

6. **Configure Always On**
   ```bash
   az webapp config set \
     --resource-group jpvos-rg \
     --name jpvos-gateway \
     --always-on true
   ```

### Checklist
- [ ] Resource group created
- [ ] App Service plan created
- [ ] Web app created and running
- [ ] All environment variables configured
- [ ] HTTPS only enabled
- [ ] Always On enabled
- [ ] Custom domain configured (next step)

---

## Cloudflare Configuration

### DNS Setup

1. **Add CNAME Record**
   - Subdomain: `jpvos` (for `jpvos.yourdomain.com`) or `@` (for `yourdomain.com`)
   - Target: `jpvos-gateway.azurewebsites.net`
   - TTL: Automatic
   - Proxy Status: Proxied (orange cloud)

2. **SSL/TLS Settings**
   - Mode: Full (Strict)
   - Always Use HTTPS: On
   - Minimum TLS Version: 1.3
   - Opportunistic Encryption: On
   - Automatic HTTPS Rewrites: On

3. **Cache Rules**
   - API routes: Bypass cache
   - Static assets: Cache 24 hours
   - HTML pages: Cache 1 hour

4. **Page Rules**
   - Pattern: `yourdomain.com/api/*`
   - Setting: Cache Level = Bypass

### Checklist
- [ ] CNAME record added to Cloudflare
- [ ] DNS propagation verified (`nslookup yourdomain.com`)
- [ ] SSL certificate provisioned
- [ ] SSL mode set to Full (Strict)
- [ ] Cache rules configured
- [ ] HTTPS redirect enabled

---

## Stripe Configuration

### Webhook Endpoint

1. **In Stripe Dashboard** (Dashboard > Developers > Webhooks)
   - Endpoint URL: `https://yourdomain.com/api/stripe/webhook`
   - Signing Secret: Copy and save for Azure configuration
   - Events to receive:
     - [ ] `checkout.session.completed`
     - [ ] `invoice.paid`
     - [ ] `invoice.payment_failed`
     - [ ] `customer.subscription.updated`
     - [ ] `customer.subscription.deleted`

2. **Test Webhook**
   ```bash
   stripe trigger checkout.session.completed \
     --api-key sk_test_...
   ```

3. **Verify in Logs**
   - Check Azure Application Insights for webhook events
   - Verify 200 OK responses logged

### Checklist
- [ ] Webhook endpoint registered
- [ ] Webhook secret saved to Azure App Service
- [ ] Required events selected
- [ ] Test event sent and processed
- [ ] StripeWebhookController logs show receipt

---

## Discord Configuration

### OAuth Application

1. **In Discord Developer Portal** (discord.com/developers/applications)
   - Application Name: `JPV-OS Access Gateway`
   - OAuth2 > Redirect URLs: `https://yourdomain.com/api/discord/oauth/callback`
   - Copy Client ID and Client Secret

2. **Configure Bot Role Assignments**
   - Go to Server Settings > Roles
   - Create role (e.g., `jpvos-custom`)
   - Note role ID
   - Assign role to bot in "Member Roles"

3. **Grant Permissions**
   - `manage:guild_expressions` (for emoji)
   - `manage:roles` (for role assignment)
   - `read:members` (for member info)

### Checklist
- [ ] OAuth application created
- [ ] Redirect URI configured
- [ ] Client ID saved to Azure
- [ ] Client Secret saved to Azure
- [ ] Bot token saved to Azure
- [ ] Guild ID saved to Azure
- [ ] Role ID saved to Azure
- [ ] Bot has required permissions

---

## Testing Procedures

### Pre-Deployment Testing (Local)

1. **Build Test**
   ```bash
   dotnet build JPVOS.sln -c Release
   ```
   Expected: Build succeeds, 0 warnings, 0 errors

2. **Route Test**
   ```bash
   dotnet run
   curl http://localhost:5111/
   curl http://localhost:5111/pricing
   curl http://localhost:5111/access
   curl http://localhost:5111/ecosystem
   curl http://localhost:5111/login
   curl http://localhost:5111/api/health
   ```
   Expected: All routes return HTTP 200

3. **Health Check**
   ```bash
   curl http://localhost:5111/api/health | jq .
   ```
   Expected: JSON response with status, app, runtime, utc

### Post-Deployment Testing

1. **DNS Resolution**
   ```bash
   nslookup yourdomain.com
   dig yourdomain.com
   ```
   Expected: Resolves to Cloudflare IP

2. **HTTPS Verification**
   ```bash
   curl -I https://yourdomain.com/
   openssl s_client -connect yourdomain.com:443
   ```
   Expected: HTTP 200, valid SSL certificate

3. **Route Testing**
   ```bash
   curl https://yourdomain.com/
   curl https://yourdomain.com/pricing
   curl https://yourdomain.com/access
   curl https://yourdomain.com/ecosystem
   curl https://yourdomain.com/login
   curl https://yourdomain.com/api/health
   ```
   Expected: All routes return HTTP 200

4. **Health Endpoint**
   ```bash
   curl https://yourdomain.com/api/health | jq .
   ```
   Expected: JSON response with status, app, runtime, utc

5. **Stripe Webhook Test**
   - Send test event from Stripe Dashboard
   - Verify in Azure Application Insights

6. **Discord OAuth Test**
   - Navigate to `/api/discord/oauth/connect?state=test-user-id`
   - Should redirect to Discord authorize page
   - After accepting, should redirect to `/access`

### Checklist
- [ ] Pre-deployment local build test passed
- [ ] All pre-deployment routes accessible
- [ ] DNS resolves to Cloudflare
- [ ] HTTPS working with valid certificate
- [ ] Post-deployment routes all accessible
- [ ] Health endpoint responding correctly
- [ ] Stripe webhook receiving events
- [ ] Discord OAuth flow working

---

## Go-Live Approval

### Sign-Off Checklist

- [ ] All repository hygiene tasks complete
- [ ] All Azure configuration complete
- [ ] All Cloudflare configuration complete
- [ ] All Stripe configuration complete
- [ ] All Discord configuration complete
- [ ] All pre-deployment tests passed
- [ ] All post-deployment tests passed
- [ ] Monitoring and alerting configured
- [ ] Backup strategy confirmed
- [ ] Rollback plan documented
- [ ] Team notified of go-live
- [ ] Support contact information shared

### Go-Live Approval Sign-Off

**Date**: ___________

**Approved By**: ___________

**Title**: ___________

**Notes**: 

---

## Post-Launch Monitoring

### First Week
- [ ] Monitor error logs hourly
- [ ] Check webhook success rates
- [ ] Monitor page load times
- [ ] Track OAuth success/failure rates
- [ ] Review Application Insights for anomalies

### Ongoing (Weekly)
- [ ] Review error trends
- [ ] Monitor resource utilization
- [ ] Check SSL certificate expiration (set reminder for renewal)
- [ ] Review Stripe webhook logs
- [ ] Review Discord role assignments

### Monthly
- [ ] Full system health review
- [ ] Security audit of logs
- [ ] Performance optimization review
- [ ] Backup verification
- [ ] Update dependencies (if needed)

---

## Rollback Plan

If issues occur post-launch:

1. **Immediate** (within 1 hour)
   ```bash
   # Revert to previous deployment
   az webapp deployment slot swap \
     --resource-group jpvos-rg \
     --name jpvos-gateway \
     --slot staging
   ```

2. **Short-term** (within 1 day)
   - Analyze logs in Application Insights
   - Fix identified issues
   - Deploy fixed version

3. **Long-term** (within 1 week)
   - Post-mortem on incident
   - Update runbooks
   - Improve monitoring

---

## Support Contacts

- **Azure Support**: Microsoft support portal
- **Stripe Support**: stripe.com/contact
- **Discord Support**: discord.com/developers
- **Cloudflare Support**: Cloudflare dashboard support
- **Internal Team**: [contact info]

---

## Related Documentation

- `docs/LAUNCH-COMPLETION-GUIDE.md` - Comprehensive launch guide
- `docs/AZURE-PROVISIONING-GUIDE.md` - Azure setup instructions
- `docs/WIX-CHECKOUT-ROUTING.md` - Checkout flow documentation
- `src/JPVOS/PEOPLE-PROTECTION-NON-NEGOTIABLE.md` - Security policy
