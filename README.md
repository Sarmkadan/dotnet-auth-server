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

## AuthorizationRequest

The `AuthorizationRequest` class represents an OAuth 2.0 / OpenID Connect authorization request containing all parameters sent by a client during the authorization flow. It encapsulates standard OAuth parameters like `client_id`, `response_type`, `redirect_uri`, `scope`, and OpenID Connect extensions such as `nonce`, `code_challenge`, and `prompt`. The class provides helper methods to validate requests, parse scopes, and check for PKCE or OpenID Connect compliance.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a new authorization request for the authorization code flow with PKCE
var authRequest = new AuthorizationRequest
{
    ClientId = "web-client",
    ResponseType = "code",
    RedirectUri = "https://client.example.com/callback",
    Scope = "openid profile email api:read",
    State = Guid.NewGuid().ToString(),
    Nonce = Guid.NewGuid().ToString(),
    CodeChallenge = "E9Melhoa2OwvFrEMTJks93UTHBbYu3_KJDijOhbwNY",
    CodeChallengeMethod = "S256",
    Display = "page",
    Prompt = "consent",
    MaxAge = 3600,
    UiLocales = "en-US",
    AcrValues = "urn:mace:incommon:iap:silver",
    LoginHint = "user@example.com",
    CustomParameters = new Dictionary<string, string>
    {
        { "custom_param", "value" },
        { "another_param", "123" }
    }
};

// Validate the request
if (authRequest.IsValid())
{
    Console.WriteLine("Authorization request is valid");
}

// Check if PKCE is required
if (authRequest.HasPkce())
{
    Console.WriteLine("PKCE is required for this request");
}

// Check if this is an OpenID Connect request
if (authRequest.IsOpenIdRequest())
{
    Console.WriteLine("This is an OpenID Connect request");
}

// Get requested scopes as a list
var requestedScopes = authRequest.GetRequestedScopes();
foreach (var scope in requestedScopes)
{
    Console.WriteLine($"Requested scope: {scope}");
}
```

## TokenRequest

The `TokenRequest` class represents an OAuth 2.0 token request containing all parameters required to obtain access tokens, refresh tokens, or exchange tokens. It supports multiple grant types including authorization code, refresh token, client credentials, password grant, and token exchange (RFC 8693). The class provides validation methods to ensure required parameters are present for each grant type.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a token request for the authorization code grant type
var tokenRequest1 = new TokenRequest
{
    GrantType = "authorization_code",
    ClientId = "web-client",
    ClientSecret = "secret",
    Code = "auth-code-123",
    RedirectUri = "https://client.example.com/callback",
    CodeVerifier = "code-verifier-456"
};

// Validate the request for authorization_code grant type
if (tokenRequest1.IsValidForGrantType("authorization_code"))
{
    Console.WriteLine("Authorization code token request is valid");
}

// Create a token request for the password grant type
var tokenRequest2 = new TokenRequest
{
    GrantType = "password",
    ClientId = "confidential-client",
    ClientSecret = "secret",
    Username = "user@example.com",
    Password = "password123",
    Scope = "api:read api:write",
    IpAddress = "192.168.1.100"
};

// Validate the request for password grant type
if (tokenRequest2.IsValidForGrantType("password"))
{
    Console.WriteLine("Password grant token request is valid");
}

// Create a token request for the refresh token grant type
var tokenRequest3 = new TokenRequest
{
    GrantType = "refresh_token",
    ClientId = "web-client",
    ClientSecret = "secret",
    RefreshToken = "refresh-token-789",
    Scope = "api:read"
};

// Validate the request for refresh_token grant type
if (tokenRequest3.IsValidForGrantType("refresh_token"))
{
    Console.WriteLine("Refresh token request is valid");
}

// Create a token request for the client credentials grant type
var tokenRequest4 = new TokenRequest
{
    GrantType = "client_credentials",
    ClientId = "service-account",
    ClientSecret = "secret",
    Scope = "api:read api:write"
};

// Validate the request for client_credentials grant type
if (tokenRequest4.IsValidForGrantType("client_credentials"))
{
    Console.WriteLine("Client credentials token request is valid");
}

// Create a token request for token exchange (RFC 8693)
var tokenRequest5 = new TokenRequest
{
    GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
    ClientId = "exchange-client",
    SubjectToken = "subject-token-abc",
    SubjectTokenType = "urn:ietf:params:oauth:token-type:access_token",
    ActorToken = "actor-token-def",
    ActorTokenType = "urn:ietf:params:oauth:token-type:jwt",
    RequestedTokenType = "urn:ietf:params:oauth:token-type:refresh_token",
    Scope = "api:read"
};

// Validate the request for token exchange
if (tokenRequest5.IsValidForGrantType("urn:ietf:params:oauth:grant-type:token-exchange"))
{
    Console.WriteLine("Token exchange request is valid");
}

// Validate a generic token request
var tokenRequest6 = new TokenRequest
{
    GrantType = "password",
    ClientId = "client-id"
};

if (tokenRequest6.IsValid())
{
    Console.WriteLine("Token request is valid");
}
```

## CreateUserRequest

The `CreateUserRequest` class represents the payload for creating a new user account in the authorization system. It contains the essential user registration information including credentials, contact details, and initial role assignments. All properties are required except for `FullName` and `Roles`, which are optional.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a new user registration request
var createUserRequest = new CreateUserRequest
{
    Username = "johndoe",
    Email = "john.doe@example.com",
    Password = "SecurePassword123!",
    FullName = "John Doe",
    Roles = new List<string> { "user", "premium" }
};

// Usage in a controller or service
public async Task<IActionResult> RegisterUser(CreateUserRequest request)
{
    // Validate the request
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    // Create the user account
    var user = await _userService.CreateUserAsync(request);
    
    return CreatedAtAction(
        nameof(GetUser),
        new { id = user.UserId },
        ApiResponse<UserResponse>.SuccessResponse(
            user,
            "User created successfully"
        )
    );
}
```

## ApiResponse

The `ApiResponse<T>` and `ApiResponse` classes provide a standardized wrapper for API responses across all endpoints in the authorization server. They support both success and error responses with consistent metadata including success status, optional data payload, error messages, status codes, trace identifiers, and timestamps. These types are used throughout the application to ensure a uniform response format.

## ClientRegistrationResponse

The `ClientRegistrationResponse` class represents the response returned from the OAuth 2.0 Dynamic Client Registration endpoint (RFC 7591). It contains the registered client metadata including identifiers, credentials, grant types, redirect URIs, and other registration details that the client must persist for future authentication requests.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a client registration response for a confidential client
var clientResponse = new ClientRegistrationResponse
{
    ClientId = "web-client-123",
    ClientSecret = "s3cr3tP@ssw0rd",
    ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    ClientSecretExpiresAt = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds(),
    ClientName = "My Web Application",
    GrantTypes = new List<string> { "authorization_code", "refresh_token" },
    RedirectUris = new List<string> { "https://client.example.com/callback" },
    ResponseTypes = new List<string> { "code" },
    Scope = "openid profile email api:read",
    TokenEndpointAuthMethod = "client_secret_basic",
    LogoUri = "https://client.example.com/logo.png",
    PolicyUri = "https://client.example.com/policy",
    TosUri = "https://client.example.com/tos",
    Contacts = new List<string> { "admin@client.example.com" }
};

// Example for a public client (no client_secret)
var publicClientResponse = new ClientRegistrationResponse
{
    ClientId = "spa-client-456",
    ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    ClientName = "Single Page Application",
    GrantTypes = new List<string> { "authorization_code", "refresh_token" },
    RedirectUris = new List<string> { "https://app.example.com/callback" },
    ResponseTypes = new List<string> { "code" },
    Scope = "openid profile",
    TokenEndpointAuthMethod = "none"
};
```

```csharp
using DotnetAuthServer.Domain.Models;

// Generic success response with data
var userResponse = ApiResponse<User>.SuccessResponse(
    new User { Username = "johndoe", Email = "john@example.com" },
    "User retrieved successfully"
);

// Error response
var errorResponse = ApiResponse<TokenResponse>.ErrorResponse(
    "Invalid client credentials",
    "Authentication failed",
    401
);

// Non-generic success response
var successResponse = ApiResponse.SuccessResponse("Operation completed successfully");

// Non-generic error response
var notFoundResponse = ApiResponse.ErrorResponse(
    "User not found",
    "The requested user does not exist",
    404
);
```

## License

MIT - see [LICENSE](LICENSE).
