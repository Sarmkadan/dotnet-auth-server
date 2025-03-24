[![Build](https://github.com/sarmkadan/dotnet-auth-server/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/dotnet-auth-server/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

# dotnet-auth-server

**A production-ready OAuth2/OpenID Connect authorization server for .NET 10** with full support for PKCE, refresh token rotation, granular consent management, and advanced access control (RBAC/ABAC).

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Features](#features)
4. [Installation](#installation)
5. [Configuration](#configuration)
6. [Usage Examples](#usage-examples)
7. [API Reference](#api-reference)
8. [Security](#security)
9. [Troubleshooting](#troubleshooting)
10. [Contributing](#contributing)

---

## Overview

**dotnet-auth-server** is a minimal yet feature-rich authorization server implementing OAuth2 (RFC 6749) and OpenID Connect (OIDC 1.0) specifications. Built with .NET 10 and designed for security-first development, it provides:

- **Zero-trust security**: Mandatory PKCE for all clients, automatic refresh token rotation
- **Standards-compliant**: Full RFC 6749, RFC 6750, OIDC 1.0 Core support
- **Developer-friendly**: Swagger UI, extensive examples, clear error responses
- **Production-ready**: Comprehensive logging, audit trails, rate limiting
- **Flexible**: RBAC, ABAC, custom claims, scope-driven authorization
- **Easy to extend**: Clean service-based architecture with dependency injection

Perfect for:
- Multi-tenant SaaS platforms
- Microservice authentication infrastructure
- API gateway authorization layers
- Enterprise SSO implementations
- Security research and testing

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     HTTP Clients / SPAs                      │
└────┬────────────────────────────────────────────────────────┘
     │
     │ (1) Authorization Request
     ▼
┌─────────────────────────────────────────────────────────────┐
│           Authorization Endpoint (/oauth/authorize)          │
│  • Validates PKCE parameters                                │
│  • Manages user consent flow                                │
│  • Enforces scope restrictions                              │
└────┬────────────────────────────────────────────────────────┘
     │
     │ (2) Authorization Code (short-lived)
     ▼
┌─────────────────────────────────────────────────────────────┐
│             Token Endpoint (/oauth/token)                    │
│  • Validates authorization code + PKCE verifier             │
│  • Issues JWT access tokens (15m default)                   │
│  • Issues refresh tokens (30d default)                      │
│  • Enforces refresh token rotation                          │
└────┬────────────────────────────────────────────────────────┘
     │
     │ (3) Access Token + Refresh Token (JWT format)
     ▼
┌─────────────────────────────────────────────────────────────┐
│                 Resource Server (Your API)                   │
│  • Validates JWT signature & expiration                     │
│  • Introspects token via /oauth/token/introspect           │
│  • Enforces scope & role-based access                       │
└─────────────────────────────────────────────────────────────┘

Database Layer (In-Memory or SQL - configurable):
┌──────────────────────────────────────────────────────────┐
│ Users │ Clients │ Scopes │ Consents │ RefreshTokens      │
│ AuthGrants │ AuditLogs │ Sessions                        │
└──────────────────────────────────────────────────────────┘
```

**Data Flow for Authorization Code Flow + PKCE:**

1. Client generates `code_verifier` (43-128 chars) and `code_challenge` (S256)
2. Client redirects user to `/oauth/authorize?code_challenge=...`
3. User authenticates (username/password) and grants consent to scopes
4. Server issues short-lived authorization code
5. Client exchanges code + `code_verifier` for tokens
6. Server validates PKCE, issues JWT access token + refresh token
7. Client uses access token to call resource server
8. On expiration, client uses refresh token to get new token pair
9. Old refresh token is invalidated (rotation)

---

## Features

### OAuth2 & OpenID Connect

- ✅ **Authorization Code Flow** with PKCE (RFC 7636) - **mandatory for all clients**
- ✅ **Refresh Token Grant** with automatic rotation
- ✅ **Client Credentials Flow** for machine-to-machine auth
- ✅ **Resource Owner Password Credentials** (for legacy/internal clients)
- ✅ **Token Introspection** (RFC 7662) - validate and inspect tokens
- ✅ **Token Revocation** (RFC 7009) - explicitly revoke tokens
- ✅ **Device Authorization Flow** - for IoT and headless devices
- ✅ **JWKS (JSON Web Key Set)** endpoint for public key distribution
- ✅ **Discovery Endpoints** - OpenID Connect metadata

### Security

| Feature | Implementation | Standard |
|---------|----------------|----------|
| **PKCE** | S256 (SHA-256) mandatory | RFC 7636 |
| **Refresh Token Rotation** | New token on each use, old tracked | OAuth2 Best Practices |
| **Password Hashing** | PBKDF2-SHA256 with salt | NIST guidelines |
| **Account Lockout** | 5 failed attempts → 15min lockout | Industry standard |
| **Token Signing** | RS256 (RSA) or HS256 (configurable) | JWT RFC 7518 |
| **Session Security** | HttpOnly, Secure, SameSite cookies | OWASP |
| **Rate Limiting** | Per-IP per-endpoint throttling | DDoS mitigation |
| **CORS** | Configurable origins | XSS protection |

### User & Access Management

- **User Registration & Authentication**
  - Email + password registration
  - Email/username validation
  - Optional email verification
  - Account lockout after N failed login attempts
  - Password expiration (configurable)

- **Role-Based Access Control (RBAC)**
  ```json
  {
    "sub": "user123",
    "roles": ["admin", "editor"],
    "scopes": ["openid", "profile", "api:write"]
  }
  ```

- **Attribute-Based Access Control (ABAC)**
  - Custom claim enrichment
  - Dynamic claim mapping based on context
  - Scope-to-attribute policies
  - Attribute validation middleware

### Consent Management

- **Granular Scope-Based Consent**
  - User explicitly consents to each scope
  - Consent history tracking
  - Session-scoped vs. persistent consent
  - One-time consent option

- **Consent Revocation**
  - User can revoke consent at any time
  - Affects token refresh operations
  - Audit trail of all revocations

### Scope Management

- **OpenID Connect Standard Scopes**
  - `openid` - User identification
  - `profile` - Name, given_name, family_name, etc.
  - `email` - Email address and verified status
  - `phone` - Phone number
  - `address` - Mailing address

- **Custom Scopes**
  - Define custom scopes per your API
  - Map scopes to roles/permissions
  - Add custom claims per scope
  - Nested scope hierarchies (e.g., `api:read:users`)

- **Scope Validation**
  - Client can only request registered scopes
  - User consent required for sensitive scopes
  - Automatic claim injection based on scope

### Client Management

- **Client Types**
  - **Public**: SPAs, native mobile apps (PKCE mandatory)
  - **Confidential**: Backend services (client secret + PKCE)

- **Client Features**
  - Dynamic client secret rotation
  - Multiple redirect URI registration
  - Grant type restrictions per client
  - Scope restrictions per client
  - Client-specific claim mappings

---

## Installation

### Prerequisites

- **.NET 10.0 SDK** or later ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Visual Studio 2025**, **VS Code**, or compatible IDE
- **Git** for cloning the repository

### Option 1: Clone & Build (Development)

```bash
# Clone repository
git clone https://github.com/Sarmkadan/dotnet-auth-server.git
cd dotnet-auth-server

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run (development server will start at https://localhost:7001)
dotnet run
```

### Option 2: Docker Deployment (Production)

```bash
# Build Docker image
docker build -t dotnet-auth-server:latest .

# Run container with environment variables
docker run -d \
  --name auth-server \
  -p 5001:8080 \
  -e "AuthServer__IssuerUrl=https://auth.example.com" \
  -e "AuthServer__JwtSigningKey=your-secret-key-256-bits-min" \
  dotnet-auth-server:latest
```

### Option 3: Docker Compose (Full Stack)

```bash
# Start with database, cache, and monitoring
docker-compose up -d

# View logs
docker-compose logs -f auth-server
```

### Option 4: Publish as Self-Contained

```bash
# Publish as standalone executable (no .NET runtime required on target)
dotnet publish -c Release -r linux-x64 --self-contained

# Deploy the executable to production
cd bin/Release/net10.0/linux-x64/publish
./DotnetAuthServer
```

---

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AuthServer": {
    "IssuerUrl": "https://auth.example.com",
    "JwtSigningKey": "your-256-bit-secret-key-minimum-length-required",
    "JwtSigningAlgorithm": "HS256",
    "AccessTokenLifetimeSeconds": 3600,
    "RefreshTokenLifetimeSeconds": 2592000,
    "AuthorizationCodeLifetimeSeconds": 300,
    "RequirePkceForAllClients": true,
    "MaxFailedLoginAttempts": 5,
    "AccountLockoutDurationMinutes": 15,
    "EnableAuditLogging": true,
    "EnableRateLimiting": true,
    "RateLimitPerMinute": 60
  },
  "Cache": {
    "Type": "Memory",
    "AbsoluteExpirationMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": ["https://localhost:3000", "https://myapp.com"],
    "AllowCredentials": true
  }
}
```

### Environment Variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `AuthServer__IssuerUrl` | - | OIDC issuer URL (required) |
| `AuthServer__JwtSigningKey` | - | JWT signing key (256+ bits, required) |
| `AuthServer__AccessTokenLifetimeSeconds` | 3600 | Access token TTL (1 hour) |
| `AuthServer__RefreshTokenLifetimeSeconds` | 2592000 | Refresh token TTL (30 days) |
| `AuthServer__RequirePkceForAllClients` | true | Enforce PKCE for all clients |
| `AuthServer__EnableAuditLogging` | true | Enable audit trail |
| `ASPNETCORE_ENVIRONMENT` | Production | Execution environment |
| `ASPNETCORE_URLS` | https://0.0.0.0:8080 | Binding URL |

### Scope Configuration

Define scopes in `appsettings.json`:

```json
{
  "Scopes": [
    {
      "Name": "openid",
      "Description": "OpenID Connect scope",
      "IsRequired": true,
      "RequiresConsent": false,
      "Claims": ["sub", "iss", "aud"]
    },
    {
      "Name": "profile",
      "Description": "User profile information",
      "RequiresConsent": true,
      "Claims": ["name", "given_name", "family_name", "picture"]
    },
    {
      "Name": "api:write",
      "Description": "Write access to API",
      "RequiresConsent": true,
      "RequiredRoles": ["editor", "admin"]
    }
  ]
}
```

---

## Usage Examples

All examples use realistic scenarios and production patterns.

### Example 1: Browser-Based SPA with Authorization Code + PKCE

```bash
#!/bin/bash
# OAuth2 Authorization Code Flow for Single Page Application

AUTH_SERVER="https://auth.example.com"
CLIENT_ID="my-spa-app"
REDIRECT_URI="https://myapp.com/callback"
SCOPES="openid profile email api:write"

# Step 1: Generate PKCE parameters (43-128 characters)
CODE_VERIFIER=$(openssl rand -base64 48 | tr -d '\n=+/' | cut -c1-128)
echo "Code Verifier: $CODE_VERIFIER"

# Calculate S256 code challenge
CODE_CHALLENGE=$(echo -n "$CODE_VERIFIER" | \
  openssl dgst -sha256 -binary | \
  openssl enc -base64 | tr -d '=\n' | tr '+/' '-_')
echo "Code Challenge: $CODE_CHALLENGE"

# Step 2: Redirect user to authorization endpoint
AUTH_URL="$AUTH_SERVER/oauth/authorize?client_id=$CLIENT_ID&response_type=code&redirect_uri=$(urlencode $REDIRECT_URI)&scope=$(urlencode $SCOPES)&code_challenge=$CODE_CHALLENGE&code_challenge_method=S256&state=random123&nonce=nonce123"
echo "Open this URL in browser: $AUTH_URL"

# Step 3: User authenticates and consents, then redirected to callback with authorization code
# Browser redirects to: https://myapp.com/callback?code=xyz123&state=random123

# Step 4: Backend exchanges code for tokens
AUTH_CODE="xyz123"  # From authorization endpoint

curl -X POST "$AUTH_SERVER/oauth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "client_id=$CLIENT_ID" \
  -d "redirect_uri=$(urlencode $REDIRECT_URI)" \
  -d "code=$AUTH_CODE" \
  -d "code_verifier=$CODE_VERIFIER"

# Response:
# {
#   "access_token": "eyJhbGciOiJIUzI1NiIs...",
#   "token_type": "Bearer",
#   "expires_in": 3600,
#   "refresh_token": "refresh_xyz789",
#   "scope": "openid profile email api:write",
#   "id_token": "eyJhbGciOiJIUzI1NiIs..." (OIDC)
# }
```

### Example 2: Native Mobile App with Refresh Token Rotation

```csharp
// Using HttpClient from native mobile app (iOS/Android)

public class AuthService
{
    private readonly HttpClient _httpClient;
    private const string AuthServerUrl = "https://auth.example.com";
    private const string ClientId = "mobile-app";
    
    public async Task<TokenResponse> GetTokenAsync(string codeVerifier, string authCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, 
            $"{AuthServerUrl}/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", ClientId },
                { "code", authCode },
                { "code_verifier", codeVerifier },
                { "redirect_uri", "myapp://callback" }
            })
        };

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(json);
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        // Old refresh token is invalidated server-side (rotation)
        // New refresh token will be issued for next refresh
        
        var request = new HttpRequestMessage(HttpMethod.Post, 
            $"{AuthServerUrl}/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", ClientId },
                { "refresh_token", refreshToken }
            })
        };

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(json);
    }

    public async Task RevokeTokenAsync(string token, string tokenType = "access_token")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, 
            $"{AuthServerUrl}/oauth/token/revoke")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", token },
                { "token_type_hint", tokenType }
            })
        };

        await _httpClient.SendAsync(request);
    }
}
```

### Example 3: Machine-to-Machine Authentication (Client Credentials)

```bash
#!/bin/bash
# Service-to-Service authentication without user involvement

SERVICE_ID="data-processor-service"
SERVICE_SECRET="secret_xyz123abc"
AUTH_SERVER="https://auth.example.com"

# Request access token directly (no user consent needed)
curl -X POST "$AUTH_SERVER/oauth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=$SERVICE_ID" \
  -d "client_secret=$SERVICE_SECRET" \
  -d "scope=api:read api:write"

# Response includes only access token (no refresh token for client credentials)
# {
#   "access_token": "eyJhbGciOiJIUzI1NiIs...",
#   "token_type": "Bearer",
#   "expires_in": 3600,
#   "scope": "api:read api:write"
# }

# Use in API call
ACCESS_TOKEN=$(curl ... | jq -r '.access_token')

curl -X GET "https://api.example.com/data/users" \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

### Example 4: Token Introspection (Validate Token Server-Side)

```csharp
// In your Resource Server / API Gateway

public class TokenValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public async Task<TokenInfo> IntrospectTokenAsync(string token)
    {
        // Check cache first (avoid repeated introspection)
        if (_cache.TryGetValue($"token:{token}", out TokenInfo cached))
            return cached;

        var request = new HttpRequestMessage(HttpMethod.Post, 
            "https://auth.example.com/oauth/token/introspect")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", token }
            })
        };

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var tokenInfo = JsonSerializer.Deserialize<TokenInfo>(json);

        // Cache for remaining TTL
        _cache.Set($"token:{token}", tokenInfo, 
            TimeSpan.FromSeconds(tokenInfo.ExpiresIn ?? 300));

        return tokenInfo;
    }

    public class TokenInfo
    {
        public bool Active { get; set; }
        public string? Scope { get; set; }
        public string? ClientId { get; set; }
        public string? Subject { get; set; }
        public long IssuedAt { get; set; }
        public long ExpiresIn { get; set; }
        public Dictionary<string, object>? Claims { get; set; }
    }
}
```

### Example 5: Custom Claims & ABAC

```csharp
// Attribute-Based Access Control - enriching tokens with custom attributes

public class ClaimsEnrichmentService
{
    public async Task<IEnumerable<Claim>> EnrichClaimsAsync(
        User user, 
        Client client, 
        IEnumerable<string> requestedScopes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
        };

        // Add role claims (RBAC)
        foreach (var role in user.Roles)
            claims.Add(new(ClaimTypes.Role, role));

        // Add custom attributes (ABAC)
        if (requestedScopes.Contains("api:write"))
        {
            // Only admins can have write scope
            if (user.Roles.Contains("admin"))
                claims.Add(new("department", user.Department));
            
            claims.Add(new("can_edit", "true"));
        }

        if (requestedScopes.Contains("profile"))
        {
            claims.Add(new("phone_number", user.PhoneNumber));
            claims.Add(new("company", user.Company));
        }

        // Context-based claims (time-based access)
        var now = DateTime.UtcNow;
        if (now.Hour >= 9 && now.Hour < 17)
            claims.Add(new("business_hours", "true"));

        return await Task.FromResult(claims);
    }
}
```

### Example 6: Consent Flow UI Integration

```html
<!-- HTML form for user consent (shown after authentication) -->
<form method="POST" action="/oauth/authorize">
  <h1>Grant Access</h1>
  <p>{{ app_name }} is requesting access to your account</p>

  <fieldset>
    <legend>Requested Permissions:</legend>
    
    <label>
      <input type="checkbox" name="scopes" value="openid" checked disabled>
      <strong>Identify you</strong> - Required to sign in
    </label>

    <label>
      <input type="checkbox" name="scopes" value="profile" checked>
      <strong>Profile Information</strong> - Name, picture, etc.
    </label>

    <label>
      <input type="checkbox" name="scopes" value="email" checked>
      <strong>Email Address</strong> - Your email address
    </label>

    <label>
      <input type="checkbox" name="scopes" value="api:write">
      <strong>Modify Your Data</strong> - Write access to your resources
    </label>
  </fieldset>

  <label>
    <input type="checkbox" name="remember_consent" value="true">
    Remember this consent for future sign-ins
  </label>

  <button type="submit">Authorize</button>
  <button type="button" onclick="history.back()">Cancel</button>

  <input type="hidden" name="client_id" value="{{ client_id }}">
  <input type="hidden" name="code_challenge" value="{{ code_challenge }}">
  <input type="hidden" name="state" value="{{ state }}">
</form>
```

### Example 7: Rate Limiting & Protection

```csharp
// Middleware automatically enforces rate limiting on sensitive endpoints

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value;
        
        // Rate limit token endpoint heavily
        if (path == "/oauth/token")
        {
            var key = $"rl:token:{clientIp}";
            if (!_cache.TryGetValue(key, out int requestCount))
                requestCount = 0;

            if (requestCount >= 10) // 10 requests per minute
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Try again later.");
                return;
            }

            _cache.Set(key, requestCount + 1, TimeSpan.FromMinutes(1));
        }

        await _next(context);
    }
}
```

### Example 8: Integration with ASP.NET API

```csharp
// Your Resource Server protecting endpoints with auth server tokens

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ResourceController : ControllerBase
{
    [HttpGet("profile")]
    [Authorize(Roles = "user")]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        
        return Ok(new { userId, email });
    }

    [HttpPost("data")]
    [Authorize(Roles = "admin")]
    [Authorize("api:write")] // Scope-based authorization
    public IActionResult CreateData([FromBody] DataRequest request)
    {
        var scopes = User.FindFirst("scope")?.Value?.Split(' ') ?? Array.Empty<string>();
        
        if (!scopes.Contains("api:write"))
            return Forbid("Missing required scope: api:write");

        return Created("", new { id = "data123" });
    }
}

// Startup configuration
services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://auth.example.com";
        options.Audience = "my-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
        };
    });
```

---

## API Reference

### Authorization Endpoint

```
GET /oauth/authorize
```

Initiates the authorization flow. Redirects user to login if not authenticated.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `client_id` | string | Yes | Client identifier |
| `response_type` | string | Yes | Must be `code` (authorization code flow) |
| `redirect_uri` | string | Yes | Where to redirect after authorization |
| `scope` | string | Yes | Space-separated list of requested scopes |
| `code_challenge` | string | Yes | PKCE code challenge (S256) |
| `code_challenge_method` | string | Yes | Must be `S256` |
| `state` | string | Yes | CSRF protection token |
| `nonce` | string | No | OIDC nonce for ID token validation |
| `prompt` | string | No | `login` - force re-authentication; `consent` - force consent |

**Example:**

```
https://auth.example.com/oauth/authorize?client_id=myapp&response_type=code&redirect_uri=https://myapp.com/callback&scope=openid+profile+email&code_challenge=E9Mrozoa2owuBmwOXwItzdsQXeGAg5nSstw_5MC6mAA&code_challenge_method=S256&state=random123
```

**Success Response:**

```
HTTP/302 Found
Location: https://myapp.com/callback?code=auth_xyz123&state=random123
```

---

### Token Endpoint

```
POST /oauth/token
```

Exchanges authorization code or refresh token for access token.

**Request Body (authorization_code):**

```
grant_type=authorization_code
client_id=myapp
redirect_uri=https://myapp.com/callback
code=auth_xyz123
code_verifier=E9Mrozoa2owuBmwOXwItzdsQXeGAg5nSstw_5MC6mAA
```

**Request Body (refresh_token):**

```
grant_type=refresh_token
client_id=myapp
refresh_token=refresh_xyz789
```

**Request Body (client_credentials):**

```
grant_type=client_credentials
client_id=service_id
client_secret=service_secret
scope=api:read+api:write
```

**Success Response (200 OK):**

```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "refresh_abc789xyz",
  "scope": "openid profile email",
  "id_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Error Response (400 Bad Request):**

```json
{
  "error": "invalid_grant",
  "error_description": "Authorization code expired or invalid"
}
```

---

### Token Introspection Endpoint

```
POST /oauth/token/introspect
```

Validates and returns information about a token.

**Request Body:**

```
token=eyJhbGciOiJIUzI1NiIs...
token_type_hint=access_token
```

**Success Response (200 OK):**

```json
{
  "active": true,
  "scope": "openid profile email api:write",
  "client_id": "myapp",
  "sub": "user123",
  "issued_at": 1704067200,
  "expires_in": 3000,
  "iss": "https://auth.example.com",
  "aud": "myapp"
}
```

---

### Token Revocation Endpoint

```
POST /oauth/token/revoke
```

Revokes an access or refresh token.

**Request Body:**

```
token=refresh_xyz789
token_type_hint=refresh_token
```

**Response (200 OK):**

```
Empty body
```

---

### OpenID Connect Discovery

```
GET /.well-known/openid-configuration
```

Returns OpenID Connect server metadata.

**Response (200 OK):**

```json
{
  "issuer": "https://auth.example.com",
  "authorization_endpoint": "https://auth.example.com/oauth/authorize",
  "token_endpoint": "https://auth.example.com/oauth/token",
  "introspection_endpoint": "https://auth.example.com/oauth/token/introspect",
  "revocation_endpoint": "https://auth.example.com/oauth/token/revoke",
  "userinfo_endpoint": "https://auth.example.com/oauth/userinfo",
  "jwks_uri": "https://auth.example.com/.well-known/jwks.json",
  "response_types_supported": ["code"],
  "grant_types_supported": ["authorization_code", "refresh_token", "client_credentials"],
  "scopes_supported": ["openid", "profile", "email", "phone", "address"],
  "token_endpoint_auth_methods_supported": ["client_secret_basic"],
  "code_challenge_methods_supported": ["S256"]
}
```

---

### JWKS Endpoint

```
GET /.well-known/jwks.json
```

Returns public keys for JWT signature validation.

**Response (200 OK):**

```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "alg": "RS256",
      "kid": "key-1",
      "n": "xGOr-H...",
      "e": "AQAB"
    }
  ]
}
```

---

## Security

### PKCE (Proof Key for Public Clients)

**Mandatory for all clients** - prevents authorization code interception attacks.

```
1. Client generates random 43-128 char string: code_verifier
2. Client hashes verifier: code_challenge = BASE64URL(SHA256(code_verifier))
3. Client sends code_challenge in /oauth/authorize request
4. Server stores code_challenge with authorization code
5. Client exchanges code + code_verifier in token request
6. Server validates: SHA256(code_verifier) == stored_code_challenge
```

**Implementation in JavaScript:**

```javascript
async function generatePKCE() {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  const codeVerifier = btoa(String.fromCharCode(...array))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
  
  const encoder = new TextEncoder();
  const data = encoder.encode(codeVerifier);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  const codeChallenge = btoa(String.fromCharCode(...hashArray))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
  
  return { codeVerifier, codeChallenge };
}
```

### Refresh Token Rotation

Every refresh operation issues a **new refresh token and invalidates the old one**.

```
Benefits:
- Detects compromised tokens (reuse attempts fail)
- Limits token lifetime exposure
- Enables secure offline scenarios

Implementation:
1. Client uses refresh_token → server validates & refreshes
2. Server issues new access token + new refresh_token
3. Old refresh_token is marked as used/revoked
4. Future requests with old token are rejected
```

### Password Security

- **Algorithm**: PBKDF2-SHA256 with 100,000 iterations (NIST 800-132)
- **Salt**: 32 bytes (256 bits) random per user
- **Verifier**: Constant-time comparison (timing attack protection)
- **Storage**: Hashed only, never plaintext

### JWT Token Claims

```json
{
  "sub": "user123",
  "iss": "https://auth.example.com",
  "aud": "myapp",
  "scope": "openid profile email",
  "roles": ["user"],
  "email": "user@example.com",
  "email_verified": true,
  "given_name": "John",
  "family_name": "Doe",
  "picture": "https://gravatar.com/...",
  "iat": 1704067200,
  "exp": 1704070800,
  "nbf": 1704067200
}
```

### HTTPS / TLS

**Production**: Always use HTTPS (TLS 1.2+)
**Development**: Localhost allows HTTP for testing

### Session Security

- **Cookies**: HttpOnly, Secure, SameSite=Strict
- **CSRF Protection**: State parameter mandatory
- **Session Timeout**: Configurable (default 30 minutes)

### Audit Logging

All sensitive operations are logged:
- User authentication attempts (success/failure)
- Token issuance and revocation
- Consent grants and revocations
- Client secret rotations
- Access denials (scope, role violations)

### Rate Limiting

Protects against brute force and DoS:

| Endpoint | Limit | Duration |
|----------|-------|----------|
| `/oauth/token` | 10 req | 1 min |
| `/oauth/authorize` | 20 req | 5 min |
| User login | 5 failures | 15 min lockout |

---

## Troubleshooting

### Issue: "code_challenge_method must be S256"

**Cause**: Client not providing PKCE parameters

**Solution**:
```bash
# Always include PKCE parameters
code_challenge="E9Mrozoa2owuBmwOXwItzdsQXeGAg5nSstw_5MC6mAA"
code_challenge_method="S256"

# In authorization URL
?code_challenge=$code_challenge&code_challenge_method=$code_challenge_method
```

---

### Issue: "invalid_grant - Authorization code expired"

**Cause**: Authorization code older than 5 minutes (default TTL)

**Solution**:
- Codes are short-lived by design (300 seconds)
- Immediately exchange for tokens after user consents
- User must complete flow within the time window

---

### Issue: "invalid_scope - Scope not allowed for client"

**Cause**: Client not registered for requested scope

**Solution**:
```json
{
  "Clients": [
    {
      "ClientId": "myapp",
      "AllowedScopes": ["openid", "profile", "email"]
    }
  ]
}
```

---

### Issue: Refresh token not returned

**Cause**: Requesting wrong grant type or missing required parameters

**Solution**:
- Use `grant_type=authorization_code` with authorization code
- Ensure `redirect_uri` matches registered URI
- Verify PKCE parameters match

---

### Issue: "Too many requests" (429)

**Cause**: Rate limiting triggered

**Solution**:
- Wait before retrying (default: 1 minute for token endpoint)
- Implement exponential backoff: 1s → 2s → 4s → 8s
- Consider adjusting `RateLimitPerMinute` in config for your load

---

### Issue: Token validation fails in resource server

**Cause**: Issuer mismatch or signature validation failure

**Solution**:
```csharp
// Resource server must validate against same issuer
.AddJwtBearer(options =>
{
    options.Authority = "https://auth.example.com"; // Must match
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "https://auth.example.com",
        ValidateIssuerSigningKey = true
    };
});
```

---

### Issue: CORS errors in browser

**Cause**: Frontend origin not in allowed list

**Solution**:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:3000",
      "https://myapp.com"
    ],
    "AllowCredentials": true
  }
}
```

---

## Contributing

**We welcome contributions!** Please follow these guidelines:

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make changes with comprehensive commit messages
4. Test thoroughly (unit + integration tests)
5. Submit a pull request

### Code Standards

- **All .cs files** must include the copyright header
- **Methods** must have explanatory comments
- **Code style**: Follow C# conventions (PascalCase for public members)
- **Null safety**: Use nullable reference types (`#nullable enable`)
- **Security first**: Never trust user input; validate at boundaries

### Testing

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName"

# With coverage
dotnet test /p:CollectCoverage=true
```

### Documentation

- Update README.md for user-facing changes
- Update API docs for endpoint changes
- Add examples in `/examples` for new features
- Update CHANGELOG.md

### Commit Messages

```
[TYPE] Brief description

Detailed explanation if needed.

- Specific change 1
- Specific change 2

Fixes #123
```

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `security`

---

## License

MIT License - Copyright © 2026 Vladyslav Zaiets

See [LICENSE](LICENSE) file for details.

---

## Roadmap

- ☑️ Phase 1: Core OAuth2/OIDC implementation
- ☑️ Phase 2: Advanced features (consent, ABAC, audit)
- ☑️ Phase 3: Documentation & examples (current)
- 📋 Phase 4: Persistence layer (SQL Server, PostgreSQL)
- 📋 Phase 5: Admin dashboard & management API
- 📋 Phase 6: High-availability setup (clustering, caching)

---

## Support

- 📖 **Documentation**: See `/docs` directory
- 💬 **Issues**: GitHub Issues
- 🔗 **Examples**: See `/examples` directory
- 📧 **Contact**: rutova2@gmail.com

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
