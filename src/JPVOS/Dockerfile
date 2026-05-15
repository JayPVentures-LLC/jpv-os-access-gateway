# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY JPVOS.csproj ./
RUN dotnet restore JPVOS.csproj

# Copy remaining source
COPY . .

# Build and publish (--no-restore since restore already ran)
RUN dotnet publish JPVOS.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Use non-root user for security
# The aspnet:8.0 image includes user 'app' with UID 1654
USER 1654

# Copy published app
COPY --from=build /app/publish .

# Expose port (container platforms typically assign their own port)
EXPOSE 8080

# Configure ASP.NET Core to listen on all interfaces
# Default to port 8080, but allow override via PORT env var at runtime
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

# Note: Container platforms (Render, Railway, Fly.io) provide their own health checks.
# The /health endpoint is available for platform health probes.
# Platforms that set PORT env var should also set ASPNETCORE_HTTP_PORTS=$PORT

ENTRYPOINT ["dotnet", "JPVOS.dll"]
