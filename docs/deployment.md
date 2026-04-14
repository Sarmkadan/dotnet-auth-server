# Deployment Guide

Production deployment strategies for dotnet-auth-server.

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Environment Configuration](#environment-configuration)
3. [Docker Deployment](#docker-deployment)
4. [Kubernetes Deployment](#kubernetes-deployment)
5. [Azure Deployment](#azure-deployment)
6. [Performance Tuning](#performance-tuning)
7. [Monitoring & Logging](#monitoring--logging)
8. [Backup & Recovery](#backup--recovery)

---

## Pre-Deployment Checklist

Before deploying to production:

- [ ] Generate strong JWT signing key (256+ bits)
- [ ] Set up secrets manager (Azure Key Vault, HashiCorp Vault)
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Enable audit logging
- [ ] Set up monitoring and alerting
- [ ] Plan for database replication/failover
- [ ] Load test with expected traffic
- [ ] Set rate limits appropriately
- [ ] Configure CORS allowed origins
- [ ] Document all client credentials
- [ ] Plan incident response procedures
- [ ] Set up log aggregation (ELK, Splunk, etc.)

---

## Environment Configuration

### Production appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff zzz"
    }
  },
  "AuthServer": {
    "IssuerUrl": "https://auth.example.com",
    "JwtSigningKey": "${JWT_SIGNING_KEY}",
    "JwtSigningAlgorithm": "RS256",
    "AccessTokenLifetimeSeconds": 3600,
    "RefreshTokenLifetimeSeconds": 2592000,
    "AuthorizationCodeLifetimeSeconds": 300,
    "RequirePkceForAllClients": true,
    "MaxFailedLoginAttempts": 5,
    "AccountLockoutDurationMinutes": 15,
    "EnableAuditLogging": true,
    "EnableRateLimiting": true,
    "RateLimitPerMinute": 30
  },
  "Cache": {
    "Type": "Distributed",
    "AbsoluteExpirationMinutes": 60,
    "SlidingExpirationMinutes": 20
  },
  "Cors": {
    "AllowedOrigins": [
      "https://myapp.com",
      "https://admin.myapp.com"
    ],
    "AllowCredentials": true,
    "AllowedMethods": ["POST", "GET", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization"],
    "ExposeHeaders": ["Content-Length"]
  },
  "Database": {
    "Type": "SqlServer",
    "ConnectionString": "${DATABASE_CONNECTION_STRING}",
    "CommandTimeout": 30
  }
}
```

### Environment Variables

```bash
# Security
export JWT_SIGNING_KEY="very-long-random-256-bit-key"
export DATABASE_CONNECTION_STRING="Server=db.example.com;Database=auth;User=sa;Password=..."

# URLs
export ASPNETCORE_URLS="https://0.0.0.0:8080"
export AuthServer__IssuerUrl="https://auth.example.com"

# Logging
export ASPNETCORE_ENVIRONMENT="Production"
export Logging__LogLevel__Default="Information"

# Rate Limiting
export AuthServer__RateLimitPerMinute="60"

# CORS
export Cors__AllowedOrigins__0="https://myapp.com"
```

---

## Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

COPY ["dotnet-auth-server.csproj", "./"]
RUN dotnet restore "dotnet-auth-server.csproj"

COPY . .
RUN dotnet build "dotnet-auth-server.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish "dotnet-auth-server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=https://0.0.0.0:8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD dotnet-trace collect --duration 1ms || exit 1

ENTRYPOINT ["dotnet", "DotnetAuthServer.dll"]
```

### Docker Compose (Full Stack)

```yaml
version: '3.8'

services:
  auth-server:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5001:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      AuthServer__IssuerUrl: https://auth.localhost:5001
      AuthServer__JwtSigningKey: ${JWT_SIGNING_KEY}
      Database__ConnectionString: "Server=postgres;Database=authdb;User=auth_user;Password=${DB_PASSWORD};"
    depends_on:
      - postgres
      - redis
    volumes:
      - ./logs:/app/logs
    networks:
      - auth-network
    restart: unless-stopped

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: authdb
      POSTGRES_USER: auth_user
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - auth-network
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - auth-network
    restart: unless-stopped

  adminer:
    image: adminer
    ports:
      - "8081:8080"
    networks:
      - auth-network
    restart: unless-stopped

volumes:
  postgres_data:
  redis_data:

networks:
  auth-network:
    driver: bridge
```

### Build and Run

```bash
# Build image
docker build -t dotnet-auth-server:latest .

# Run container
docker run -d \
  --name auth-server \
  -p 5001:8080 \
  -e "AuthServer__IssuerUrl=https://auth.example.com" \
  -e "AuthServer__JwtSigningKey=your-secret-key" \
  dotnet-auth-server:latest

# View logs
docker logs -f auth-server

# Stop container
docker stop auth-server
```

---

## Kubernetes Deployment

### Kubernetes Manifest

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: auth-server-config
  namespace: auth
data:
  appsettings.json: |
    {
      "AuthServer": {
        "IssuerUrl": "https://auth.example.com",
        "AccessTokenLifetimeSeconds": 3600
      }
    }

---
apiVersion: v1
kind: Secret
metadata:
  name: auth-server-secrets
  namespace: auth
type: Opaque
data:
  jwt-signing-key: YOUR_BASE64_ENCODED_KEY
  db-connection-string: YOUR_BASE64_ENCODED_STRING

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: auth-server
  namespace: auth
spec:
  replicas: 3
  selector:
    matchLabels:
      app: auth-server
  template:
    metadata:
      labels:
        app: auth-server
    spec:
      containers:
      - name: auth-server
        image: dotnet-auth-server:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 8080
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: Production
        - name: AuthServer__JwtSigningKey
          valueFrom:
            secretKeyRef:
              name: auth-server-secrets
              key: jwt-signing-key
        - name: Database__ConnectionString
          valueFrom:
            secretKeyRef:
              name: auth-server-secrets
              key: db-connection-string
        resources:
          requests:
            cpu: 250m
            memory: 512Mi
          limits:
            cpu: 500m
            memory: 1Gi
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5

---
apiVersion: v1
kind: Service
metadata:
  name: auth-server
  namespace: auth
spec:
  selector:
    app: auth-server
  ports:
  - port: 443
    targetPort: 8080
    protocol: TCP
  type: LoadBalancer

---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: auth-server-hpa
  namespace: auth
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: auth-server
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### Deploy to Kubernetes

```bash
# Create namespace
kubectl create namespace auth

# Create secrets
kubectl create secret generic auth-server-secrets \
  --from-literal=jwt-signing-key='your-key' \
  --from-literal=db-connection-string='Server=...' \
  -n auth

# Deploy
kubectl apply -f k8s-deployment.yaml

# Check rollout
kubectl rollout status deployment/auth-server -n auth

# View logs
kubectl logs -f deployment/auth-server -n auth
```

---

## Azure Deployment

### App Service Deployment

```bash
# Create resource group
az group create --name AuthServerRG --location eastus

# Create App Service plan
az appservice plan create \
  --name AuthServerPlan \
  --resource-group AuthServerRG \
  --sku B2 \
  --is-linux

# Create App Service
az webapp create \
  --name auth-server \
  --resource-group AuthServerRG \
  --plan AuthServerPlan \
  --runtime "DOTNETCORE|10.0"

# Set environment variables
az webapp config appsettings set \
  --name auth-server \
  --resource-group AuthServerRG \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    AuthServer__IssuerUrl=https://auth-server.azurewebsites.net \
    AuthServer__JwtSigningKey=@Microsoft.KeyVault(SecretUri=https://authkv.vault.azure.net/secrets/jwt-key/)

# Deploy from GitHub
az webapp deployment source config-zip \
  --name auth-server \
  --resource-group AuthServerRG \
  --src publish.zip
```

---

## Performance Tuning

### Database Optimization

```sql
-- Create indexes for common queries
CREATE INDEX idx_user_username ON Users(Username);
CREATE INDEX idx_client_id ON Clients(ClientId);
CREATE INDEX idx_token_userid ON RefreshTokens(UserId);
CREATE INDEX idx_consent_userid_clientid ON Consents(UserId, ClientId);

-- Create connection pool
-- (Configure in appsettings.json)
```

### Caching Strategy

```csharp
// Cache token validation results
_memoryCache.Set(
    $"token_validation:{token}",
    tokenInfo,
    TimeSpan.FromMinutes(1)
);

// Cache scope definitions
_memoryCache.Set(
    "scopes:all",
    scopes,
    TimeSpan.FromHours(1)
);

// Cache client registrations
_memoryCache.Set(
    $"client:{clientId}",
    client,
    TimeSpan.FromHours(2)
);
```

### Connection Pooling

```json
{
  "Database": {
    "ConnectionString": "Server=...; Max Pool Size=20; Min Pool Size=5;"
  }
}
```

---

## Monitoring & Logging

### Application Insights

```csharp
services.AddApplicationInsightsTelemetry();

// Track custom events
_telemetryClient.TrackEvent("TokenIssued", new Dictionary<string, string>
{
    { "ClientId", clientId },
    { "UserId", userId },
    { "Scopes", scope }
});
```

### Prometheus Metrics

```csharp
// Expose metrics endpoint at /metrics
services.AddPrometheusMetrics();

// Custom metrics
var tokenCounter = new Counter("tokens_issued_total", "Total tokens issued");
tokenCounter.Inc();
```

### Log Aggregation (ELK Stack)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "Elasticsearch": {
      "Enabled": true,
      "Url": "https://elasticsearch.example.com:9200"
    }
  }
}
```

### Alerting Rules

```yaml
# Prometheus alert rules
groups:
- name: auth-server
  rules:
  - alert: HighTokenFailureRate
    expr: rate(tokens_failed_total[5m]) > 0.05
    annotations:
      summary: "Token generation failure rate > 5%"

  - alert: HighRateLimitExceeded
    expr: increase(rate_limit_exceeded_total[5m]) > 100
    annotations:
      summary: "High rate limit violations"
```

---

## Backup & Recovery

### Database Backup

```bash
# PostgreSQL backup
pg_dump -h db.example.com -U auth_user authdb | gzip > backup.sql.gz

# SQL Server backup
sqlcmd -S server.database.windows.net -U user -Q \
  "BACKUP DATABASE authdb TO DISK = 'backup.bak'"

# Restore
psql -h db.example.com -U auth_user authdb < backup.sql.gz
```

### State Recovery

```csharp
// Implement state recovery in services
public class TokenRecoveryService
{
    public async Task RecoverExpiredTokensAsync()
    {
        // Find orphaned tokens
        var orphaned = await _tokenRepository
            .GetExpiredTokensAsync();
        
        // Clean up
        foreach (var token in orphaned)
            await _tokenRepository.DeleteAsync(token.Id);
    }
}
```

---

For more information, see [Getting Started](./getting-started.md) and [Architecture](./architecture.md).
