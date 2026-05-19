# JPV-OS Access Gateway - Disc Storage Configuration Guide

## Overview

The JPV-OS Access Gateway uses SQLite-based persistent storage for entitlement records in production environments. This guide covers configuration, deployment scenarios, troubleshooting, and best practices for disc storage.

## Storage Architecture

### Development Environment
- **Storage Type:** In-memory (non-persistent)
- **Use Case:** Local development and testing
- **Configuration:** Automatic (no setup required)
- **Data Loss:** Yes, data cleared on restart

### Production Environment
- **Storage Type:** SQLite database file
- **Use Case:** Live deployments with persistent entitlement records
- **Configuration:** Automatic with environment-aware defaults
- **Data Loss:** No, persists across restarts
- **Scalability:** Single-instance deployments; larger deployments should migrate to managed databases

## Automatic Path Detection

The application automatically detects and configures the database path based on the deployment environment:

### Priority Order

1. **Explicit Configuration (Highest Priority)**
   - Environment variable: `ENTITLEMENTS_DB_PATH`
   - Example: `ENTITLEMENTS_DB_PATH=/var/lib/jpv-os/entitlements.db`
   - Use this when you have specific storage requirements

2. **Azure App Service**
   - Auto-detected via `WEBSITE_INSTANCE_ID` environment variable
   - Database path: `/home/entitlements.db`
   - Advantages: Persists across restarts, scaling operations, and slot swaps
   - Recommended for Azure App Service deployments

3. **Container Deployments (Docker)**
   - Auto-detected via `/app/data` directory or `DOTNET_RUNNING_IN_CONTAINER` environment variable
   - Database path: `/app/data/entitlements.db`
   - Requires volume mount for persistence
   - Advantages: Isolated per container, portable across hosts

4. **Fallback (Lowest Priority)**
   - Application binary directory: `{AppContext.BaseDirectory}/entitlements.db`
   - **Not recommended for production** - may not persist across updates
   - Used only if no other environment is detected

## Deployment Scenarios

### Scenario 1: Azure App Service

**Setup:**
```bash
# Set environment variables in Azure Portal or via Azure CLI
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name myAppService \
  --settings ASPNETCORE_ENVIRONMENT=Production \
  STRIPE_SECRET_KEY=sk_live_... \
  DISCORD_CLIENT_ID=... \
  # ... other required variables
```

**Database Location:** `/home/entitlements.db`
- Automatically detected and configured
- Persists across restarts
- No additional configuration needed

**Backup Strategy:**
```bash
# Backup the database
az webapp deployment slot swap \
  --resource-group myResourceGroup \
  --name myAppService

# Or use Azure Storage to backup /home directory
az storage blob upload \
  --container-name backups \
  --file /path/to/backup/entitlements.db \
  --name entitlements-$(date +%s).db
```

### Scenario 2: Docker Container

**Multi-Stage Dockerfile:**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet build JPVOS.sln -c Release
RUN dotnet publish src/JPVOS/JPVOS.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=builder --chown=app:app /app/publish .
RUN mkdir -p /app/data && chown -R app:app /app/data
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
CMD ["dotnet", "JPVOS.dll"]
```

**Simplified Dockerfile (Using Pre-Built Artifacts):**
```dockerfile
# Pre-build with: dotnet publish src/JPVOS/JPVOS.csproj -c Release -o ./publish
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --chown=app:app publish .
RUN mkdir -p /app/data && chown -R app:app /app/data
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
CMD ["dotnet", "JPVOS.dll"]
```

**Docker Run:**
```bash
# Create volume for persistence
docker volume create jpv-os-data

# Run container with volume mount
docker run -d \
  --name jpv-os \
  -p 8080:8080 \
  -v jpv-os-data:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e STRIPE_SECRET_KEY=sk_live_... \
  -e DISCORD_CLIENT_ID=... \
  jpv-os:latest
```

**Database Location:** `/app/data/entitlements.db`
- Must be mounted as a Docker volume
- Persists when container is stopped/restarted
- Can be backed up independently

**Backup Strategy:**
```bash
# Backup the volume
docker run --rm \
  -v jpv-os-data:/data \
  -v $(pwd):/backup \
  ubuntu tar czf /backup/jpv-os-backup.tar.gz -C /data .

# Restore from backup
docker run --rm \
  -v jpv-os-data:/data \
  -v $(pwd):/backup \
  ubuntu tar xzf /backup/jpv-os-backup.tar.gz -C /data
```

### Scenario 3: Docker Compose

**docker-compose.yml:**
```yaml
version: '3.9'
services:
  jpv-os:
    image: jpv-os:latest
    ports:
      - "8080:8080"
    volumes:
      - jpv-os-data:/app/data
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_HTTP_PORTS: 8080
      STRIPE_SECRET_KEY: ${STRIPE_SECRET_KEY}
      STRIPE_WEBHOOK_SECRET: ${STRIPE_WEBHOOK_SECRET}
      DISCORD_CLIENT_ID: ${DISCORD_CLIENT_ID}
      # ... other variables

volumes:
  jpv-os-data:
```

**Deploy:**
```bash
docker-compose up -d
```

### Scenario 4: Kubernetes

**Persistent Volume:**
```yaml
apiVersion: v1
kind: PersistentVolume
metadata:
  name: jpv-os-pv
spec:
  capacity:
    storage: 10Gi
  accessModes:
    - ReadWriteOnce
  storageClassName: standard
  hostPath:
    path: /data/jpv-os
```

**Persistent Volume Claim:**
```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: jpv-os-pvc
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: standard
  resources:
    requests:
      storage: 10Gi
```

**Deployment:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: jpv-os
spec:
  replicas: 1  # SQLite is single-instance only
  selector:
    matchLabels:
      app: jpv-os
  template:
    metadata:
      labels:
        app: jpv-os
    spec:
      containers:
      - name: jpv-os
        image: jpv-os:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_HTTP_PORTS
          value: "8080"
        - name: STRIPE_SECRET_KEY
          valueFrom:
            secretKeyRef:
              name: jpv-os-secrets
              key: stripe-secret-key
        # ... other environment variables
        volumeMounts:
        - name: data
          mountPath: /app/data
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: jpv-os-pvc
```

### Scenario 5: Custom Path Configuration

**For specialized requirements:**
```bash
export ENTITLEMENTS_DB_PATH=/mnt/data/jpv-os/entitlements.db

# Ensure directory exists and is writable
mkdir -p $(dirname $ENTITLEMENTS_DB_PATH)
chmod 755 $(dirname $ENTITLEMENTS_DB_PATH)

# Run application
ASPNETCORE_ENVIRONMENT=Production dotnet JPVOS.dll
```

## Database Features

### Automatic Initialization
- **Table Creation:** Schema is automatically created on first run
- **Directory Creation:** Missing directories are created with proper permissions
- **Error Validation:** Comprehensive checks for write permissions and I/O errors
- **Logging:** Detailed logs for all initialization steps

### Error Handling
The storage system provides detailed error messages for common issues:

```
Failed to create database directory: Permission denied
  → Solution: Check parent directory permissions

Failed to create entitlements database: Disk full
  → Solution: Free up disk space

Insufficient permissions to access entitlements database
  → Solution: Change file/directory ownership or permissions
```

### Data Integrity
- **Transactions:** All writes use proper transaction handling
- **Idempotency:** Duplicate webhook events are handled correctly
- **Foreign Keys:** Schema enforces data consistency
- **Backups:** Regular backups prevent data loss

## Monitoring and Maintenance

### Health Checks

**Application Health:**
```bash
curl http://localhost:8080/health
# Response: {"status":"healthy","timestamp":"2026-05-19T..."}
```

**Database Health:**
Monitor application logs for storage-related messages:
```
INFO: Entitlements database configured for production deployment
INFO: Entitlements database initialized successfully
```

### Size Management

**Monitor Database Size:**
```bash
# Linux/macOS
ls -lh /path/to/entitlements.db

# Docker
docker exec jpv-os ls -lh /app/data/entitlements.db

# Azure App Service
az webapp ssh --resource-group myRG --name myApp
# Then: ls -lh /home/entitlements.db
```

**Expected Growth:**
- New entitlements: ~1 KB per record
- Updates to existing: No additional disk space
- Example: 10,000 entitlements ≈ 1-2 MB database

### Backup Schedule

**Daily Backups (Recommended):**
```bash
# Automated backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/jpv-os"
mkdir -p $BACKUP_DIR
cp /app/data/entitlements.db $BACKUP_DIR/entitlements_$DATE.db

# Keep only last 30 days
find $BACKUP_DIR -name "entitlements_*.db" -mtime +30 -delete
```

**Cloud Backups:**
- **Azure:** Use Azure Backup or blob storage snapshots
- **AWS:** Use EBS snapshots or S3 for backup storage
- **Google Cloud:** Use Cloud SQL backups or Persistent Disk snapshots

## Migration Paths

### From In-Memory to SQLite (Development → Production)

No code changes needed. The application automatically switches based on environment:

```bash
# Development: Uses in-memory storage
dotnet run

# Production: Uses SQLite storage
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

### From SQLite to Managed Database

For deployments requiring scale or compliance:

1. **Azure SQL Database:**
   - Create new repository implementation: `SqlServerEntitlementRepository`
   - Update `Program.cs` to register new repository
   - Migrate data using export/import tools

2. **PostgreSQL:**
   - Create new repository implementation: `PostgresEntitlementRepository`
   - Use Dapper for database access (same pattern as SQLite)
   - Full data portability

## Troubleshooting

### Database Initialization Failure

**Symptom:** `Failed to initialize entitlements database`

**Check 1: Environment Detection**
```bash
# Verify ASPNETCORE_ENVIRONMENT is set to Production
echo $ASPNETCORE_ENVIRONMENT  # Should output: Production
```

**Check 2: Directory Permissions**
```bash
# For /home (Azure App Service)
ls -ld /home  # Should show write permission for app user

# For /app/data (Container)
ls -ld /app/data  # Should show write permission for app user
```

**Check 3: Disk Space**
```bash
df -h  # Verify sufficient free space (>100 MB recommended)
```

**Check 4: File Permissions**
```bash
# If database file exists
ls -la /path/to/entitlements.db
chmod 644 /path/to/entitlements.db
```

### Database Lock Errors

**Symptom:** `database is locked`

**Cause:** Multiple processes accessing database simultaneously
- SQLite doesn't support concurrent writes

**Solutions:**
- Ensure only one application instance is running
- For scale-out, migrate to PostgreSQL or SQL Server
- Check for background processes: `lsof /app/data/entitlements.db`

### Corruption

**Symptom:** Unrecoverable database errors

**Recovery:**
1. Stop application
2. Restore from recent backup
3. Restart application
4. Verify health endpoint returns 200 OK

```bash
# Verify database integrity (SQLite)
sqlite3 /app/data/entitlements.db "PRAGMA integrity_check;"
# Should output: ok
```

## Performance Considerations

### Query Performance
- **Point Lookups:** ~1ms (indexed by StripeCustomerId, StripeSubscriptionId)
- **Full Scans:** <10ms for typical deployments (< 100k records)
- **Write Operations:** ~2-5ms per transaction

### Storage Efficiency
- **Per-Record Size:** ~1 KB
- **Indexes:** Additional ~500 bytes per record
- **Total Overhead:** ~1.5 KB per entitlement

### Scaling Limits
- **SQLite:** Practical limit ~100,000 active entitlements
- **Beyond this:** Migrate to PostgreSQL or SQL Server
- **At Scale:** Implement read replicas and connection pooling

## Best Practices

1. **Always set `ASPNETCORE_ENVIRONMENT=Production` in production**
   - Enables persistent storage automatically
   - Required for database initialization

2. **Use explicit `ENTITLEMENTS_DB_PATH` for custom deployments**
   - Clearer configuration intent
   - Easier to audit and change

3. **Implement regular backup routines**
   - Daily backup minimum
   - Test restoration procedures
   - Keep 30-day retention

4. **Monitor database growth**
   - Set alerts for disk usage
   - Plan for database archival after 1-2 years

5. **Use volumes/persistent storage in containers**
   - Never rely on container filesystem for data
   - Volume mount ensures data survives container restarts

6. **Validate storage on startup**
   - Application logs initialization status
   - Health check endpoint confirms database is accessible
   - Monitor for permission or I/O errors

## Support

For issues related to disc storage:

1. Check application logs for detailed error messages
2. Verify environment variables are set correctly
3. Ensure directory permissions allow read/write
4. Check disk space availability
5. Review [DEPLOYMENT.md](./DEPLOYMENT.md) for platform-specific guidance

For help, create an issue in the repository with:
- Error logs (sanitized of secrets)
- Deployment environment details
- Steps to reproduce
