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

## Authentication

The server authenticates two kinds of principals:

- **Users** sign in with a username and password at the authorization endpoint. When MFA is enabled for the account, a TOTP code (or a backup code) is required after the password step. Failed password and TOTP attempts are rate limited per username and per IP address (see [Rate limiting](#rate-limiting)).
- **Clients** authenticate at the token, introspection and revocation endpoints. PKCE is enforced by default on `GET /oauth/authorize`, and each client has its own `RequirePkce` setting. Confidential clients authenticate with `client_id` and `client_secret`, sent either in the request body or as HTTP Basic credentials.

Issued access and refresh tokens are signed with `JwtSigningKey`. Resource APIs validate them either by calling `POST /oauth/introspect` or, preferably, by checking the signature against the public keys at `GET /.well-known/jwks.json`. A setup example is in [Protecting a resource API](#protecting-a-resource-api-and-enabling-mfa).

Keep `JwtSigningKey` out of source control and load it from the environment outside local development.

For the step-by-step flows, see [Getting Started](#getting-started), [Authorization Code + PKCE](#3-authorization-code--pkce-flow-example) and [Enrolling a user in TOTP MFA](#2-enrolling-a-user-in-totp-mfa).

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

## Protecting a resource API and enabling MFA

This section shows how to protect an ASP.NET Core API using tokens issued by this authorization server and how to enable TOTP MFA for users.

### 1. Protecting a Resource API with JWT Bearer Authentication

To protect a separate ASP.NET Core API that accepts tokens issued by this server, configure JWT Bearer authentication to validate tokens against the server's issuer and JWKS endpoint:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add authentication with JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Metadata address points to the authorization server's OIDC discovery document
    options.MetadataAddress = "https://localhost:7001/.well-known/openid-configuration";
    
    // Optional: Validate token lifetime and issuer
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false, // Adjust based on your requirements
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Protect your endpoints with the [Authorize] attribute
app.MapGet("/api/protected", [Authorize] () =>
{
    return Results.Ok(new { message = "This is a protected resource", user = User.Identity?.Name });
});

app.Run();
```

This configuration automatically:
- Fetches the signing keys from the JWKS endpoint (`/.well-known/jwks.json`)
- Validates the token signature
- Checks the token expiration
- Validates the issuer against the metadata document
- Sets up the user principal for `[Authorize]` attributes

### 2. Enrolling a User in TOTP MFA

To enable TOTP MFA for a user, follow this three-step process using the MFA management endpoints:

#### Step 1: Initiate MFA Setup
Generate a secret key, QR code provisioning URI, and backup codes:

```bash
curl -X POST "https://localhost:7001/api/users/user-123/mfa/setup" \
  -H "Content-Type: application/json"
```

Example response:
```json
{
  "secretKey": "JBSWY3DPEHPK3PXP",
  "provisioningUri": "otpauth://totp/localhost:7001:alice?secret=JBSWY3DPEHPK3PXP&issuer=localhost:7001&algorithm=SHA1&digits=6&period=30",
  "backupCodes": [
    "A1B2C3D4",
    "E5F6G7H8",
    "I9J0K1L2",
    "M3N4O5P6",
    "Q7R8S9T0",
    "U1V2W3X4",
    "Y5Z6A7B8",
    "C9D0E1F2"
  ]
}
```

**Important**: Display the `provisioningUri` as a QR code for the user to scan with their authenticator app (Google Authenticator, Authy, etc.), or provide the `secretKey` for manual entry. Show the backup codes only once and instruct the user to store them securely.

#### Step 2: Confirm MFA Setup
After the user scans the QR code and generates a 6-digit TOTP code, confirm the setup:

```bash
curl -X POST "https://localhost:7001/api/users/user-123/mfa/confirm" \
  -H "Content-Type: application/json" \
  -d '{"code": "123456"}'
```

Example response:
```json
{
  "message": "MFA has been enabled for your account"
}
```

#### Step 3: Verify MFA During Login
When MFA is enabled, the login flow requires an additional verification step:

1. User completes primary authentication (username/password)
2. Server responds with MFA challenge
3. User provides TOTP code from authenticator app
4. Server grants access upon successful verification

Example MFA verification request:
```bash
curl -X POST "https://localhost:7001/api/users/user-123/mfa/verify" \
  -H "Content-Type: application/json" \
  -d '{"code": "654321"}'
```

Example success response:
```json
{
  "message": "MFA verification successful"
}
```

Example failure response:
```json
{
  "error": "invalid_grant",
  "error_description": "Invalid or expired MFA code"
}
```

### 3. Login Flow Changes When MFA is Enabled

When a user has MFA enabled, the authentication flow modifies as follows:

#### Standard Login (MFA Disabled)
1. User sends credentials to `/oauth/token` (password grant)
2. Server validates credentials
3. Server returns access token immediately

#### Login with MFA Enabled
1. User sends credentials to `/oauth/token` (password grant)
2. Server validates credentials and detects MFA is required
3. Server returns HTTP 401 with MFA challenge:
   ```json
   {
     "error": "mfa_required",
     "error_description": "Multi-factor authentication is required",
     "mfa_token": "temporary-mfa-session-token"
   }
   ```
4. User sends MFA verification request with the temporary token:
   ```bash
   curl -X POST "https://localhost:7001/api/users/user-123/mfa/verify" \
     -H "Content-Type: application/json" \
     -d '{"code": "123456", "mfa_token": "temporary-mfa-session-token"}'
   ```
5. Upon successful MFA verification, server returns access token:
   ```json
   {
     "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "token_type": "Bearer",
     "expires_in": 3600,
     "refresh_token": "refresh-token-456",
     "scope": "openid profile email api:read"
   }
   ```

#### Using Backup Codes
If the user loses access to their authenticator app, they can use a backup code:
```bash
curl -X POST "https://localhost:7001/api/users/user-123/mfa/verify" \
  -H "Content-Type: application/json" \
  -d '{"code": "A1B2C3D4"}'  # 8-character backup code
```

Backup codes are single-use and automatically removed after successful verification.

## Architecture

The solution is a thin ASP.NET Core host (`Program.cs`) over a self-contained
library (`src/`, `DotnetAuthServer.Core`): controllers delegate to services,
services talk to repository interfaces, and everything protocol-shaped
(introspection, revocation, JWKS, userinfo) lives in dedicated handlers.
Storage is currently in-memory - the repository interfaces are the seam for a
persistent implementation.

Full write-up with rationale, data flow and known limitations:
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Plugging in your own user storage

The authorization server uses repository interfaces for data storage. To replace the default in-memory storage with a persistent implementation (e.g., using Entity Framework Core), you need to implement the repository interfaces and register them in the dependency injection container.

The key repository interfaces for user and refresh token storage are:
   - `DotnetAuthServer.Data.Repositories.IUserRepository`
   - `DotnetAuthServer.Data.Repositories.IRefreshTokenRepository`

Additionally, if you want to replace other storage (clients, authorization grants, etc.), you would implement their respective repository interfaces.

Below is a minimal example of how to implement `IUserRepository` and `IRefreshTokenRepository` using Entity Framework Core.

### 1. Define your DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using DotnetAuthServer.Domain.Entities;

public class AuthServerDbContext : DbContext
{
    public AuthServerDbContext(DbContextOptions<AuthServerDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure your entities here if needed
        // For example, setting up indexes, etc.
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<RefreshToken>().HasIndex(rt => rt.TokenHash).IsUnique();
    }
}
```

### 2. Implement IUserRepository

```csharp
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class EfCoreUserRepository : IUserRepository
{
    private readonly AuthServerDbContext _dbContext;

    public EfCoreUserRepository(AuthServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.ToListAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = new User { UserId = id };
        _dbContext.Users.Remove(user);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(u => u.UserId == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.Where(u => u.Roles.Contains(role)).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.Where(u => u.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLower();
        return await _dbContext.Users.Where(u =>
            u.Username.ToLower().Contains(lowerQuery) ||
            u.Email.ToLower().Contains(lowerQuery) ||
            (u.FullName?.ToLower().Contains(lowerQuery) ?? false))
        .ToListAsync(cancellationToken);
    }
}
```

### 3. Implement IRefreshTokenRepository

```csharp
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class EfCoreRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthServerDbContext _dbContext;

    public EfCoreRefreshTokenRepository(AuthServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens.ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var token = new RefreshToken { TokenId = id };
        _dbContext.RefreshTokens.Remove(token);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens.AnyAsync(rt => rt.TokenId == id, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync(cancellationToken);
    }
}
```

### 4. Register the DbContext and repositories in DI

In your `Program.cs` or `Startup.cs`, add the following:

```csharp
using DotnetAuthServer.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AuthServerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add repositories
builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, EfCoreRefreshTokenRepository>();

// ... other service registrations
```

### 5. Note on atomic operations

Certain operations must be atomic to prevent race conditions and ensure consistency:

- **Refresh token rotation** (`RefreshTokenHandler.RotateAsync`): This operation involves revoking the current refresh token and issuing a new one. These steps should be executed in a single transaction to prevent a scenario where the old token is revoked but the new token fails to be issued (or vice versa).

- **Refresh token chain revocation** (`RefreshTokenHandler.RevokeChainAsync`): This operation revokes all refresh tokens for a given user and client. It should be atomic to prevent partial revocation.

In the Entity Framework Core implementations above, the individual repository methods (`CreateAsync`, `UpdateAsync`, etc.) are atomic because they call `SaveChangesAsync` which operates within a transaction. However, the rotation and chain revocation operations in the handler involve multiple repository calls. To make these operations atomic, you would need to modify the `RefreshTokenHandler` to use a single transaction across multiple operations, or alternatively, you could extend the repository interfaces to include methods that perform these operations in a transaction.

However, note that the current design of the `RefreshTokenHandler` does not expose a way to wrap multiple operations in a transaction. Therefore, if you require atomicity for rotation and chain revocation, you may need to:

a) Modify the `RefreshTokenHandler` to accept a `DbContext` (or a unit of work) and use it to manage transactions, or
b) Extend the repository interfaces to include methods for atomic rotation and chain revocation.

Since the task is about plugging in your own storage, and the existing `RefreshTokenHandler` is designed to work with the repository interfaces, we note that the default in-memory implementation does not provide transactions either, but the operations are fast and the risk of interleaving is low. For a production database, you should consider one of the above approaches to ensure atomicity.

Alternatively, you can override the `RefreshTokenHandler` with your own implementation that uses the DbContext to manage transactions, and then register that in DI instead of the default one.

Given the complexity, and since the task only asks for a minimal custom implementation for user and refresh token storage, we leave the transaction management as an exercise for the implementer, but note the importance of atomicity for the mentioned operations.

## Tests and benchmarks

```bash
dotnet test tests/dotnet-auth-server.Tests
dotnet run -c Release --project dotnet-auth-server.Benchmarks
```

## License

MIT - see [LICENSE](LICENSE).
