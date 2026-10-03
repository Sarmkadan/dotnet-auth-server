# dotnet-auth-server

A lightweight, extensible OAuth 2.0 / OpenID Connect authorization server for .NET
(net10.0). Authorization code + PKCE, client credentials, refresh token and password
grants, token introspection/revocation, dynamic client registration, consent,
TOTP MFA, session management - with an in-memory storage layer designed to be
swapped for a real database.

## Quick start

```bash
dotnet run --project dotnet-auth-server.csproj
```

## Getting Started

### 1. Minimal Program.cs

```csharp
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Middleware;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.AddOptions<DotnetAuthServerOptions>()
    .Bind(builder.Configuration.GetSection("DotnetAuthServer"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register repositories
builder.Services.AddSingleton<IClientRepository, ClientRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IAuthorizationGrantRepository, AuthorizationGrantRepository>();
builder.Services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<DotnetAuthServer.Services.IConsentRepository, DotnetAuthServer.Services.ConsentRepository>();
builder.Services.AddSingleton<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddSingleton<ITotpCredentialRepository, TotpCredentialRepository>();

// Register services
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ClientValidationService>();
builder.Services.AddScoped<ScopeValidationService>();
builder.Services.AddScoped<PkceValidationService>();
builder.Services.AddScoped<ConsentService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<TotpService>();

// Register handlers
builder.Services.AddScoped<TokenIntrospectionHandler>();
builder.Services.AddScoped<TokenRevocationHandler>();
builder.Services.AddScoped<UserinfoHandler>();
builder.Services.AddScoped<JwksHandler>();

// Register middleware
builder.Services.AddSingleton<ErrorHandlingMiddleware>();
builder.Services.AddSingleton<RateLimitingMiddleware>();
builder.Services.AddSingleton<LoginRateLimiter>();
builder.Services.AddSingleton<TotpRateLimiter>();

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

// Map OAuth2 endpoints
app.MapControllers();

// OAuth2 metadata endpoint
app.MapGet("/.well-known/oauth-authorization-server", () =>
{
    var authServerOptions = app.Services.GetRequiredService<AuthServerOptions>();
    return Results.Json(new
    {
        issuer = authServerOptions.IssuerUrl,
        authorization_endpoint = $"{authServerOptions.IssuerUrl}/oauth/authorize",
        token_endpoint = $"{authServerOptions.IssuerUrl}/oauth/token",
        revocation_endpoint = $"{authServerOptions.IssuerUrl}/oauth/revoke",
        introspection_endpoint = $"{authServerOptions.IssuerUrl}/oauth/introspect",
        scopes_supported = authServerOptions.SupportedScopes,
        grant_types_supported = authServerOptions.SupportedGrantTypes
    });
});

// OIDC metadata endpoint
app.MapGet("/.well-known/openid-configuration", () =>
{
    var authServerOptions = app.Services.GetRequiredService<AuthServerOptions>();
    return Results.Json(new
    {
        issuer = authServerOptions.IssuerUrl,
        authorization_endpoint = $"{authServerOptions.IssuerUrl}/oauth/authorize",
        token_endpoint = $"{authServerOptions.IssuerUrl}/oauth/token",
        userinfo_endpoint = $"{authServerOptions.IssuerUrl}/oauth/userinfo",
        jwks_uri = $"{authServerOptions.IssuerUrl}/.well-known/jwks.json",
        scopes_supported = authServerOptions.SupportedScopes
    });
});

// JWKS endpoint
app.MapGet("/.well-known/jwks.json", async (JwksHandler jwksHandler, CancellationToken cancellationToken) =>
{
    var jwks = await jwksHandler.GetJwksAsync(cancellationToken);
    return Results.Json(jwks);
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

### 2. Example appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "DotnetAuthServer": {
    "AuthServer": {
      "IssuerUrl": "https://localhost:7001",
      "JwtSigningKey": "your-256-bit-or-longer-secret-key-should-be-set-from-environment",
      "JwtAlgorithm": "HS256",
      "AccessTokenLifetimeSeconds": 3600,
      "RefreshTokenLifetimeSeconds": 2592000,
      "AuthorizationCodeLifetimeSeconds": 300,
      "RequirePkceForAllClients": true,
      "ClockSkewToleranceSeconds": 300,
      "UseInMemoryDatabase": true,
      "FailedLoginAttemptThreshold": 5,
      "AccountLockoutDurationMinutes": 15,
      "RequireUserConsent": true,
      "SupportedScopes": ["openid", "profile", "email", "api:read", "api:write"],
      "SupportedGrantTypes": ["authorization_code", "refresh_token", "client_credentials", "password"]
    }
  }
}
```

### 3. Authorization Code + PKCE Flow Example

#### Register a client

```bash
curl -X POST "https://localhost:7001/register" \
  -H "Content-Type: application/json" \
  -d '{
    "client_name": "My Test Client",
    "grant_types": ["authorization_code", "refresh_token"],
    "redirect_uris": ["https://localhost:5001/callback"],
    "response_types": ["code"],
    "scope": "openid profile email api:read",
    "token_endpoint_auth_method": "client_secret_basic"
  }'
```

Example response:
```json
{
  "client_id": "test-client",
  "client_secret": "generated-client-secret",
  "client_id_issued_at": 1625097600,
  "client_secret_expires_at": 1656633600,
  "client_name": "My Test Client",
  "grant_types": ["authorization_code", "refresh_token"],
  "redirect_uris": ["https://localhost:5001/callback"],
  "response_types": ["code"],
  "scope": "openid profile email api:read",
  "token_endpoint_auth_method": "client_secret_basic"
}
```

#### Authorization Request

```bash
# Generate code verifier and challenge
CODE_VERIFIER=$(openssl rand -base64 32 | tr '+/' '-_' | tr -d '=')
CODE_CHALLENGE=$(echo -n $CODE_VERIFIER | openssl dgst -sha256 -binary | openssl enc -base64 | tr '+/' '-_' | tr -d '=')

# Make authorization request
curl -v "https://localhost:7001/oauth/authorize?response_type=code&client_id=test-client&redirect_uri=https://localhost:5001/callback&scope=openid%20profile%20email%20api:read&state=12345&code_challenge=$CODE_CHALLENGE&code_challenge_method=S256"
```

Example redirect response:
```
HTTP/1.1 302 Found
Location: https://localhost:5001/callback?code=auth-code-123&state=12345
```

#### Token Exchange

```bash
curl -X POST "https://localhost:7001/oauth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&code=auth-code-123&redirect_uri=https://localhost:5001/callback&client_id=test-client&client_secret=generated-client-secret&code_verifier=$CODE_VERIFIER"
```

Example response:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "refresh-token-456",
  "scope": "openid profile email api:read"
}
```

#### Refresh Token Exchange

```bash
curl -X POST "https://localhost:7001/oauth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=refresh-token-456&client_id=test-client&client_secret=generated-client-secret"
```

Example response:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "new-refresh-token-789",
  "scope": "openid profile email api:read"
}
```

## Rate limiting

`src/Middleware/RateLimitingMiddleware.cs` applies an in-memory token-bucket limit to requests whose paths start with one of the configured sensitive endpoints. Buckets are keyed by the first 20 characters of the `Authorization` header when present, then by the `client_id` query parameter, and finally by the remote IP address. A rejected request receives HTTP 429, a `Retry-After: 60` header, and a `rate_limit_exceeded` JSON error.

`src/Middleware/RateLimitingOptions.cs` defines these defaults:

- `RequestsPerMinute`: `60`, the token refill rate per client.
- `BurstSize`: `10`, the maximum token-bucket capacity.
- `SensitiveEndpoints`: `/oauth/token`, `/oauth/authorize`, `/oauth/introspect`, and `/oauth/revoke` (matched case-insensitively).

`src/Security/LoginRateLimiter.cs` separately tracks failed password-login attempts in sliding windows for usernames and IP addresses, clears a username's attempts after a successful login, and includes a global circuit breaker for aggregate failures. Its thresholds and window length come from `AuthServerOptions`.

`src/Security/TotpRateLimiter.cs` tracks TOTP verification attempts per user in a sliding window and rejects users who reach the configured threshold with HTTP 429 and retry timing in the error message. Both successful and failed TOTP attempts are recorded; its threshold and window length also come from `AuthServerOptions`.

Swagger UI is available at `/swagger` in development. Discovery documents live at
`/.well-known/oauth-authorization-server` and `/.well-known/openid-configuration`.

Configuration is bound from the `DotnetAuthServer` section - see
`appsettings.example.json` for the full shape. Set `JwtSigningKey` from the
environment in anything but local development.

## Endpoints

| Route | Purpose |
|---|---|
| `GET /oauth/authorize` | Authorization code flow (PKCE enforced by default) |
| `POST /oauth/token` | Token endpoint (authorization_code, refresh_token, client_credentials, password) |
| `POST /oauth/introspect` | RFC 7662 token introspection |
| `POST /oauth/revoke` | RFC 7009 token revocation |
| `POST /register` | RFC 7591 dynamic client registration |
| `GET /.well-known/jwks.json` | Signing key set |
| `api/users`, `api/sessions`, `api/users/{id}/mfa` | Management APIs |
| `GET /health` | Health check |

## Architecture

The solution is a thin ASP.NET Core host (`Program.cs`) over a self-contained
library (`src/`, `DotnetAuthServer.Core`): controllers delegate to services,
services talk to repository interfaces, and everything protocol-shaped
(introspection, revocation, JWKS, userinfo) lives in dedicated handlers.
Storage is currently in-memory - the repository interfaces are the seam for a
persistent implementation.

Full write-up with rationale, data flow and known limitations:
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Tests and benchmarks

```bash
dotnet test tests/dotnet-auth-server.Tests
dotnet run -c Release --project dotnet-auth-server.Benchmarks
```

## License

MIT - see [LICENSE](LICENSE).
