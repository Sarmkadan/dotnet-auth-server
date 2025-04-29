# Migration Guide: v1.0.0 to v2.0.0

This document outlines breaking changes and migration steps for upgrading from dotnet-auth-server v1.0.0 to v2.0.0.

---

## Overview

v2.0.0 introduces **production-grade Docker support**, **simplified deployment**, and **improved security defaults**. Most code changes are backward compatible, but configuration and deployment approaches have changed significantly.

---

## Breaking Changes

### 1. Docker Base Image Update

**v1.0.0:** `mcr.microsoft.com/dotnet/aspnet:10.0`  
**v2.0.0:** `mcr.microsoft.com/dotnet/aspnet:10.0` (no change, but build process refined)

The Dockerfile now uses a strict multi-stage build with dedicated `builder` and `publish` stages for optimized image size and faster builds.

**Migration:** No action required. Rebuild images with `docker-compose build --no-cache`.

---

### 2. Health Check Endpoint

**v1.0.0:** `/health` endpoint required manual implementation  
**v2.0.0:** HEALTHCHECK directive in Dockerfile uses `curl -f https://localhost:8080/health`

**Action Required:**
- Ensure your application exposes a `/health` endpoint that returns HTTP 200
- Example implementation in `Program.cs`:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();
```

---

### 3. Non-Root User Enforcement

**v1.0.0:** Docker container ran as `root`  
**v2.0.0:** Container runs as `appuser` (UID 1001)

**Action Required:**
- Verify all file operations in your container use directories writable by UID 1001
- If using shared volumes, ensure correct ownership: `chown -R 1001:1001 /path/to/volume`
- Logs directory ownership is set automatically during build

---

### 4. Environment Variable Changes

| Old Format | New Format | Notes |
|-----------|-----------|-------|
| `ASPNETCORE_URLS` | `ASPNETCORE_URLS=https://0.0.0.0:8080` | Explicitly enforced HTTPS binding |
| `ASPNETCORE_ENVIRONMENT` | `ASPNETCORE_ENVIRONMENT=Production` | Default set in Dockerfile |
| (new) | `JWT_SIGNING_KEY` | Required for docker-compose .env |
| (new) | `DB_PASSWORD` | Required for docker-compose .env |

**Action Required:**
- Create `.env` file in repo root for docker-compose:

```env
JWT_SIGNING_KEY=your-256-bit-secret-key-minimum-length-required-for-production
DB_PASSWORD=your-secure-postgres-password
PGADMIN_PASSWORD=your-pgadmin-password
GRAFANA_PASSWORD=your-grafana-password
GRAFANA_USER=admin
```

**Never commit `.env` to version control.**

---

### 5. Docker Compose Port Bindings

**v1.0.0:** Application port exposed as `5000:8080`  
**v2.0.0:** Application port exposed as `5001:8080`

**Action Required:**
- Update client configurations pointing to `localhost:5000` to use `localhost:5001`
- Adjust firewall rules and reverse proxy configurations accordingly

---

### 6. Added Services in docker-compose.yml

v2.0.0 includes optional services for production observability:

- **Prometheus** (port 9090): Metrics scraper
- **Grafana** (port 3000): Visualization dashboards
- **pgAdmin** (port 5050): Database administration UI
- **Adminer** (port 8081): Lightweight database manager

**Action Required:**
- These are optional but recommended for monitoring
- Disable if not needed by commenting out service blocks
- Ensure network policies allow these ports in production

---

## Database Migration

No schema changes in v2.0.0 - existing databases are fully compatible.

### Steps:

1. **Backup existing PostgreSQL database:**
   ```bash
   docker exec dotnet-auth-db pg_dump -U auth_user authdb > backup_v1.sql
   ```

2. **Start v2.0.0 services:**
   ```bash
   docker-compose up -d
   ```

3. **Verify application health:**
   ```bash
   curl https://localhost:5001/health
   ```

---

## Configuration Migration

### Before (v1.0.0 - Manual Deployment)

```bash
export JWT_SIGNING_KEY="..."
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --no-build
```

### After (v2.0.0 - Docker Compose)

```bash
# Create .env file
echo 'JWT_SIGNING_KEY=...' > .env
echo 'DB_PASSWORD=...' >> .env

# Start full stack
docker-compose up -d
```

---

## Security Improvements in v2.0.0

1. **Non-root container execution** - Reduces attack surface
2. **Explicit HTTPS binding** - No accidental HTTP exposure
3. **Separate build/runtime stages** - SDK removed from runtime image
4. **Health check built-in** - Kubernetes-ready probes
5. **User creation in Dockerfile** - Immutable UID across runs

---

## Troubleshooting

### Health Check Fails

**Error:** `docker ps` shows `(unhealthy)` status

**Solution:**
- Verify `/health` endpoint exists and returns 200 OK
- Check application logs: `docker logs dotnet-auth-server`
- Increase `--start-period` in HEALTHCHECK if startup is slow

### Port Conflicts

**Error:** `docker-compose up` fails with "port already in use"

**Solution:**
- Change port mappings in docker-compose.yml, e.g., `5002:8080`
- Or: Stop existing containers: `docker-compose down`

### Permission Denied on Logs

**Error:** `Cannot write to /app/logs: Permission denied`

**Solution:**
```bash
# Create logs directory with correct ownership
mkdir -p ./logs
chown 1001:1001 ./logs
docker-compose up -d
```

### TLS Certificate Validation

**Error:** `curl: (60) SSL certificate problem`

**Solution for local development:**
```bash
curl -k https://localhost:5001/health
# or
curl --insecure https://localhost:5001/health
```

For production, use a proper certificate or reverse proxy (Caddy, Nginx with Let's Encrypt).

---

## Performance Considerations

- **Multi-stage build** reduces final image size by ~60% vs single-stage
- **Health checks** incur minimal overhead (default 30s interval)
- **Redis + PostgreSQL** in compose can consume 500MB+ RAM total - adjust for constrained environments

---

## Rollback

If you need to revert to v1.0.0:

```bash
git checkout v1.0.0
docker-compose down -v  # Remove volumes to reset
docker-compose build
docker-compose up -d
```

---

## Next Steps

1. Read [deployment.md](deployment.md) for production deployment patterns
2. Review [architecture.md](architecture.md) for service internals
3. Check [faq.md](faq.md) for common questions

---

## Support

For issues during migration, refer to:
- GitHub Issues: https://github.com/sarmkadan/dotnet-auth-server/issues
- SECURITY.md: Security vulnerability disclosure
