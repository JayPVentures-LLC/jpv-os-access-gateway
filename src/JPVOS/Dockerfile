# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY JPVOS.csproj ./
RUN dotnet restore JPVOS.csproj

# Copy remaining source
COPY . .

# Build and publish
RUN dotnet publish JPVOS.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port (container platforms typically assign their own port)
EXPOSE 8080

# Configure ASP.NET Core to listen on all interfaces
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Note: Container platforms (Render, Railway, Fly.io) provide their own health checks.
# The /health endpoint is available at http://localhost:8080/health for platform health probes.

ENTRYPOINT ["dotnet", "JPVOS.dll"]
