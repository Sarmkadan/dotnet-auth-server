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

## User

The `User` entity represents an account in the authorization system, storing identity information, authentication state, and authorization metadata. It supports role-based and attribute-based access control, account lockout policies, and login tracking.

```csharp
using DotnetAuthServer.Domain.Entities;

var user = new User
{
    UserId = Guid.NewGuid().ToString(),
    Username = "johndoe",
    Email = "john@example.com",
    FullName = "John Doe",
    PasswordHash = "$2a$11$NkI5dDkscjyLJ5Y7YQO2u...".ToString(), // bcrypt hash
    EmailVerified = true,
    IsActive = true,
    Roles = new List<string> { "user", "premium" },
    Attributes = new Dictionary<string, object>
    {
        { "department", "engineering" },
        { "max_sessions", 5 }
    },
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Usage examples:
if (user.IsValid())
{
    user.RecordSuccessfulLogin();
    
    if (user.IsLocked())
    {
        Console.WriteLine("Account is locked until: " + user.LockedUntil);
    }
    
    user.LockAccount(TimeSpan.FromHours(1));
    user.RecordFailedLogin(lockoutThreshold: 5, TimeSpan.FromMinutes(30));
}
```

## WebAuthnCredential

The `WebAuthnCredential` entity represents a WebAuthn/FIDO2 public-key credential
associated with a user account, enabling passwordless and phishing-resistant
authentication via platform authenticators.

```csharp
using DotnetAuthServer.Domain.Entities;

var credential = new WebAuthnCredential
{
    Id = Guid.NewGuid().ToString(),
    CredentialId = "cred-123",
    PublicKey = new byte[] { 1, 2, 3 },
    Algorithm = -7, // ES256
    UserId = "user-123",
    FriendlyName = "Security Key",
    AaGuid = "00000000-0000-0000-0000-000000000000",
    BackupEligible = true,
    BackedUp = false,
    IsActive = true
};
```

## Scope

The `Scope` entity defines OAuth 2.0 and OpenID Connect scopes that control access to protected resources and user data. Scopes determine which claims are included in ID tokens and access tokens, which roles can request them, and whether user consent is required. This type is used throughout the authorization flow to manage scope validation, token generation, and access control.

```csharp
using DotnetAuthServer.Domain.Entities;

// Define a custom scope for API access
var apiReadScope = new Scope
{
    ScopeId = "api:read",
    DisplayName = "Read API Access",
    Description = "Allows reading data from the API. Includes claims: name, email, and profile information.",
    IsRequired = false,
    RequiresConsent = true,
    IsOpenIdScope = false,
    IsActive = true,
    IdTokenClaims = new List<string> { "name", "email" },
    AccessTokenClaims = new List<string> { "name", "email", "scope" },
    AllowedRoles = new List<string> { "user", "admin", "api-consumer" },
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Validate the scope
if (apiReadScope.IsValid())
{
    Console.WriteLine("Scope is valid");
}

// Check if a user can access this scope
var userRoles = new List<string> { "user", "premium" };
if (apiReadScope.CanUserAccessScope(userRoles))
{
    Console.WriteLine("User has access to this scope");
}

// Get all claims that will be included in tokens
var allClaims = apiReadScope.GetAllClaims();
foreach (var claim in allClaims)
{
    Console.WriteLine($"Claim: {claim}");
}
```

## License

MIT - see [LICENSE](LICENSE).
