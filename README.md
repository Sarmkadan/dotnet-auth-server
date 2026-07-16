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

## IScopeRepository

The `IScopeRepository` interface provides data access operations for managing OAuth 2.0 and OpenID Connect scopes in the authorization server. It extends the base `IRepository<Scope, string>` interface and adds scope-specific operations for retrieving scopes by their unique identifier, filtering active scopes, and searching scopes by name or description. This repository serves as the data access layer for scope management operations.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;

// Example usage in a service or controller
public class ScopeManagementService
{
    private readonly IScopeRepository _scopeRepository;

    public ScopeManagementService(IScopeRepository scopeRepository)
    {
        _scopeRepository = scopeRepository;
    }

    public async Task ManageScopesExample()
    {
        // Create a new scope
        var newScope = new Scope
        {
            ScopeId = "api:read",
            DisplayName = "Read API Access",
            Description = "Allows reading data from the API",
            IsOpenIdScope = false,
            IsActive = true
        };
        await _scopeRepository.CreateAsync(newScope);

        // Get a scope by ID
        var existingScope = await _scopeRepository.GetByIdAsync("api:read");

        // Get a scope by scope ID (case-insensitive)
        var scopeByScopeId = await _scopeRepository.GetByScopeIdAsync("api:read");

        // Get all active scopes
        var activeScopes = await _scopeRepository.GetActiveScopesAsync();

        // Search scopes by query
        var searchResults = await _scopeRepository.SearchAsync("api");

        // Check if scope exists
        var exists = await _scopeRepository.ExistsAsync("api:read");

        // Update a scope
        if (existingScope != null)
        {
            existingScope.Description = "Updated description for read access";
            await _scopeRepository.UpdateAsync(existingScope);
        }

        // Delete a scope
        await _scopeRepository.DeleteAsync(existingScope!);
        
        // Delete by ID
        await _scopeRepository.DeleteByIdAsync("api:read");
    }
}
```

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

## MfaSetupResponse

The `MfaSetupResponse` class represents the response returned when initiating TOTP multi-factor authentication enrollment. It contains the secret key, provisioning URI for QR code generation, and a set of backup codes that the user should store securely for account recovery.

```csharp
using DotnetAuthServer.Domain.Models;

// Example of handling MfaSetupResponse from an MFA enrollment endpoint
var mfaSetupResponse = await _mfaService.InitiateMfaSetupAsync(userId);

// Display the secret key and QR code URI to the user (only once!)
Console.WriteLine($"Secret Key: {mfaSetupResponse.SecretKey}");
Console.WriteLine($"Scan this URI with your authenticator app: {mfaSetupResponse.ProvisioningUri}");

// Show backup codes for offline storage (display only once!)
Console.WriteLine("Backup codes (store these securely):");
foreach (var code in mfaSetupResponse.BackupCodes)
{
    Console.WriteLine($"  {code}");
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

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class is an ASP.NET Core middleware component that intercepts exceptions thrown during request processing and converts them into consistent, client-safe HTTP error responses. It prevents sensitive internal error details from leaking to clients while ensuring all errors follow a standardized format with machine-readable error codes, human-readable descriptions, and optional documentation URIs.

```csharp
using DotnetAuthServer.Middleware;
using DotnetAuthServer.Exceptions;

// Register in Program.cs
app.UseMiddleware<ErrorHandlingMiddleware>();

// Example usage in a controller that might throw exceptions
public async Task<IActionResult> GetProtectedResource()
{
    try
    {
        var resource = await _service.GetResourceAsync();
        return Ok(resource);
    }
    catch (AuthServerException ex) when (ex.StatusCode == 404)
    {
        // AuthServerException will be automatically converted to JSON by the middleware
        return NotFound(); // This will be handled by ErrorHandlingMiddleware
    }
}
```

## ApiResponse

The `ApiResponse<T>` and `ApiResponse` classes provide a standardized wrapper for API responses across all endpoints in the authorization server. They support both success and error responses with consistent metadata including success status, optional data payload, error messages, status codes, trace identifiers, and timestamps. These types are used throughout the application to ensure a uniform response format.

## ClientRegistrationRequest

The `ClientRegistrationRequest` class represents the payload for registering a new OAuth 2.0 client application per RFC 7591 §2. It contains all metadata required to create a client registration, including grant types, redirect URIs, response types, and other client configuration options. The class provides validation to ensure required fields are present based on the grant types being used.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a client registration request for a confidential client
var clientRequest = new ClientRegistrationRequest
{
    ClientName = "My Web Application",
    GrantTypes = new List<string> { "authorization_code", "refresh_token" },
    RedirectUris = new List<string> { "https://client.example.com/callback" },
    ResponseTypes = new List<string> { "code" },
    Scope = "openid profile email api:read",
    TokenEndpointAuthMethod = "client_secret_basic",
    LogoUri = "https://client.example.com/logo.png",
    PolicyUri = "https://client.example.com/policy",
    TosUri = "https://client.example.com/tos",
    Contacts = new List<string> { "admin@client.example.com" },
    ClientUri = "https://client.example.com"
};

// Validate the request
if (clientRequest.IsValid())
{
    Console.WriteLine("Client registration request is valid");
}

// Create a client registration request for a public client (no client secret)
var publicClientRequest = new ClientRegistrationRequest
{
    ClientName = "Single Page Application",
    GrantTypes = new List<string> { "authorization_code", "refresh_token" },
    RedirectUris = new List<string> { "https://app.example.com/callback" },
    ResponseTypes = new List<string> { "code" },
    Scope = "openid profile",
    TokenEndpointAuthMethod = "none"
};

// Validate the public client request
if (publicClientRequest.IsValid())
{
    Console.WriteLine("Public client registration request is valid");
}

// Create a client registration request with minimal required fields
var minimalRequest = new ClientRegistrationRequest
{
    ClientName = "Mobile App Client"
};

// Validate the minimal request
if (minimalRequest.IsValid())
{
    Console.WriteLine("Minimal client registration request is valid");
}
```

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

## TokenResponse

The `TokenResponse` class represents the OAuth 2.0 token response returned from the token endpoint (`POST /oauth/token`). It contains the access token, token type, expiration information, optional refresh token, and any additional claims or custom properties. This type is used throughout the authorization flow to return tokens to clients after successful authentication and authorization.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a token response after successful authentication
var tokenResponse = new TokenResponse
{
    AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
    TokenType = "Bearer",
    ExpiresIn = 3600,
    RefreshToken = "8xLOxK...",
    Scope = "openid profile email api:read api:write",
    IdToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYXVkIjoiYXBpLXNlcnZpY2UiLCJleHAiOjE1MTYyMzkwMjIsImlhdCI6MTUxNjIzOTAyMn0.Pa5eS4...",
    CustomProperties = new Dictionary<string, object>
    {
        { "user_id", "user-123" },
        { "client_id", "web-client" },
        { "amr", new List<string> { "pwd", "mfa" } }
    }
};

// Example usage in a token endpoint handler
public async Task<IActionResult> IssueToken(TokenRequest request)
{
    if (!request.IsValid())
    {
        return BadRequest("Invalid token request");
    }

    // Generate tokens based on the request
    var response = await _tokenService.GenerateTokensAsync(
        request.ClientId,
        request.GetSubject(),
        request.GetScopes()
    );

    return Ok(response);
}

// Access token response (without refresh token)
var accessTokenResponse = new TokenResponse
{
    AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    TokenType = "Bearer",
    ExpiresIn = 3600,
    Scope = "api:read"
};

// Refresh token response
var refreshTokenResponse = new TokenResponse
{
    AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.new...",
    TokenType = "Bearer",
    ExpiresIn = 3600,
    RefreshToken = "new-refresh-token-xyz",
    Scope = "api:read api:write"
};
```

## ConsentRequest

The `ConsentRequest` class represents a user consent decision during the OAuth 2.0 authorization flow. It captures the user's approval or denial of requested scopes, along with contextual information such as the client application, user identity, and request metadata. This type is used to persist consent decisions and enforce scope-based access control.

```csharp
using DotnetAuthServer.Domain.Models;

// Create a consent request for a user approving scopes
var consentRequest = new ConsentRequest
{
    UserId = "user-123",
    ClientId = "web-client",
    GrantedScopes = new List<string> { "openid", "profile", "email", "api:read" },
    Approved = true,
    RememberConsent = true,
    IpAddress = "192.168.1.100",
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
};

// Validate the consent request
if (consentRequest.IsValid())
{
    Console.WriteLine("Consent request is valid");
    Console.WriteLine($"Granted scopes: {consentRequest.GetScopesString()}");
}

// Create a consent request for a user denying scopes
var deniedConsentRequest = new ConsentRequest
{
    UserId = "user-456",
    ClientId = "mobile-app",
    GrantedScopes = new List<string>(),
    Approved = false,
    DenialReason = "User declined unnecessary permissions",
    IpAddress = "10.0.0.5",
    UserAgent = "MobileApp/1.2.3"
};

// Check if consent should be remembered
if (consentRequest.RememberConsent)
{
    Console.WriteLine("Consent will be remembered for future requests");
}

// Example usage in a consent endpoint handler
public async Task<IActionResult> HandleConsent(ConsentRequest consentRequest)
{
    if (!consentRequest.IsValid())
    {
        return BadRequest("Invalid consent request");
    }

    if (consentRequest.Approved)
    {
        // Store the consent decision
        await _consentRepository.SaveConsentAsync(consentRequest);
        
        // Generate tokens based on granted scopes
        var tokenResponse = await _tokenService.GenerateTokensAsync(
            consentRequest.UserId,
            consentRequest.ClientId,
            consentRequest.GetScopesString()
        );
        
        return Ok(tokenResponse);
    }
    else
    {
        return Forbid("Consent denied by user");
    }
}
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

## AuditLoggingService

The AuditLoggingService class provides methods for logging various security-related events, such as token issuance, authentication, authorization decisions, suspicious activity, and administrative actions.

Example usage:
```csharp
public AuditLoggingService auditLogger = new AuditLoggingService();

// Log token issuance
auditLogger.LogTokenIssuance();

// Log authentication attempt
auditLogger.LogAuthentication();

// Log authorization decision
auditLogger.LogAuthorizationDecision();

// Log suspicious activity
auditLogger.LogSuspiciousActivity();

// Log administrative action
auditLogger.LogAdministrativeAction();

// Retrieve recent audit log entries
var recentEntries = auditLogger.GetRecentEntries();
```

## ScopeValidationService

The `ScopeValidationService` class provides methods for validating, filtering, and managing OAuth 2.0 scopes. It validates requested scopes against registered scopes, checks for required scopes (like `openid` for OIDC requests), merges multiple scope lists, and filters scopes to ensure only allowed scopes are used.

```csharp
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<ScopeValidationService>();

// Example usage in a controller or service
public class TokenController
{
    private readonly ScopeValidationService _scopeValidation;
    
    public TokenController(ScopeValidationService scopeValidation)
    {
        _scopeValidation = scopeValidation;
    }
    
    public async Task<IActionResult> IssueToken(TokenRequest request)
    {
        // Validate requested scopes
        var validScopes = await _scopeValidation.ValidateScopesAsync(request.Scope);
        
        // Check if required scopes are present (e.g., "openid" for OIDC)
        var hasRequiredScopes = _scopeValidation.ContainsRequiredScopes(validScopes);
        
        // Get the minimum required scopes
        var requiredScopes = _scopeValidation.GetRequiredScopes(isOidc: true);
        
        // Merge multiple scope lists
        var mergedScopes = _scopeValidation.MergeScopes(
            new[] { "openid", "profile" },
            new[] { "email", "api:read" }
        );
        
        // Filter scopes to only include granted scopes
        var filteredScopes = _scopeValidation.FilterScopes(
            new[] { "openid", "profile", "email", "api:read", "api:write" },
            new[] { "openid", "profile", "api:read" }
        );
        
        return Ok(new { validScopes, hasRequiredScopes, requiredScopes, mergedScopes, filteredScopes });
    }
}
```

## UserSessionService

The `UserSessionService` manages the lifecycle of authenticated user sessions in the authorization server. Sessions are created automatically after successful token issuance and can be revoked individually or in bulk (e.g., on password change or account deletion). The service provides methods for session management, statistics, and cleanup of expired sessions.

```csharp
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<UserSessionService>();

// Example usage in a controller or service
public class SessionManagementController
{
    private readonly UserSessionService _sessionService;

    public SessionManagementController(UserSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<IActionResult> ManageSessions(string userId)
    {
        // Create a new session after successful authentication
        var session = await _sessionService.CreateSessionAsync(
            userId: userId,
            clientId: "web-client",
            grantedScopes: "openid profile email api:read",
            ipAddress: "192.168.1.100",
            userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
        );

        // Get all active sessions for a user
        var activeSessions = await _sessionService.GetActiveSessionsAsync(userId);

        // Get all sessions (including revoked and expired) for a user
        var allSessions = await _sessionService.GetAllSessionsAsync(userId);

        // Get all active sessions across all users (admin dashboard)
        var globalActiveSessions = await _sessionService.GetAllActiveSessionsAsync();

        // Revoke a single session
        await _sessionService.RevokeSessionAsync(session.SessionId, "User requested logout");

        // Revoke all sessions for a user (e.g., on password change)
        var revokedCount = await _sessionService.RevokeAllUserSessionsAsync(
            userId,
            "Password changed - all sessions invalidated"
        );

        // Record activity on a session (extends expiration)
        await _sessionService.TouchSessionAsync(session.SessionId);

        // Get session statistics
        var stats = await _sessionService.GetStatsAsync();
        Console.WriteLine($"Total: {stats.TotalSessions}, Active: {stats.ActiveSessions}, " +
                        $"Revoked: {stats.RevokedSessions}, Expired: {stats.ExpiredSessions}, " +
                        $"Unique Users: {stats.UniqueUsers}");

        // Cleanup expired sessions (typically run as background job)
        var cleanupCount = await _sessionService.CleanupExpiredSessionsAsync();

        return Ok(new { session, activeSessions, allSessions, stats });
    }
}
```

## ClientValidationService

The `ClientValidationService` provides comprehensive validation for OAuth 2.0 clients during authorization flows. It validates client credentials, redirect URIs, allowed scopes, and grant types while leveraging caching to reduce database queries and improve performance. The service handles both confidential and public clients, ensuring proper security checks are applied based on client type.

```csharp
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<ClientValidationService>();

// Example usage in a controller or service
public class TokenController
{
    private readonly ClientValidationService _clientValidation;
    private readonly ITokenService _tokenService;

    public TokenController(ClientValidationService clientValidation, ITokenService tokenService)
    {
        _clientValidation = clientValidation;
        _tokenService = tokenService;
    }

    public async Task<IActionResult> IssueToken(TokenRequest request)
    {
        // Validate client credentials
        var client = await _clientValidation.ValidateClientCredentialsAsync(
            request.ClientId, 
            request.ClientSecret,
            cancellationToken: HttpContext.RequestAborted
        );

        // Validate redirect URI
        await _clientValidation.ValidateRedirectUriAsync(
            request.ClientId,
            request.RedirectUri,
            HttpContext.RequestAborted
        );

        // Validate grant type
        await _clientValidation.ValidateGrantTypeAsync(
            request.ClientId,
            request.GrantType,
            HttpContext.RequestAborted
        );

        // Validate requested scopes
        await _clientValidation.ValidateScopesAsync(
            request.ClientId,
            request.GetScopes(),
            HttpContext.RequestAborted
        );

        // Generate tokens for valid request
        var tokenResponse = await _tokenService.GenerateTokensAsync(
            request.ClientId,
            request.GetSubject(),
            request.GetScopes()
        );

        return Ok(tokenResponse);
    }
}
```

## License

MIT - see [LICENSE](LICENSE).
