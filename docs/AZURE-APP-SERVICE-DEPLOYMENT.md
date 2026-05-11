# Azure App Service Deployment Plan

## Target

Deploy WebApplication1 as an ASP.NET Core web app.

## Runtime

- Framework: .NET 10
- Project: WebApplication1/WebApplication1.csproj
- Release artifact: webapplication1-release
- Hosting target: Azure App Service

## Required Azure Setup

- [ ] Azure subscription selected
- [ ] Resource group created
- [ ] App Service Plan created
- [ ] Web App created
- [ ] Runtime stack configured for .NET
- [ ] HTTPS enforced
- [ ] App settings configured
- [ ] Deployment credentials or publish profile configured
- [ ] GitHub Actions secret added
- [ ] Deployment workflow added
- [ ] Health endpoint added
- [ ] Custom domain configured
- [ ] Privacy policy URL configured
- [ ] Terms URL configured
- [ ] Support contact configured

## Release Gate

No production deployment is approved until build, release artifact, HTTPS, environment configuration, logging, privacy, terms, and rollback path are verified.
