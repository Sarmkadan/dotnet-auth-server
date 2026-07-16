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

## WebAuthnPublicKeyParam

The `WebAuthnPublicKeyParam` record describes a public-key credential algorithm offered during a WebAuthn registration ceremony. It specifies the credential type and the COSE algorithm identifier that the authenticator must support.



```csharp
using DotnetAuthServer.Services;

// Create a WebAuthnPublicKeyParam for ES256 (ECDSA with P-256 curve)
var es256Param = new WebAuthnPublicKeyParam(
    Type: "public-key",
    Alg: -7  // ES256 algorithm identifier
);

// Create a WebAuthnPublicKeyParam for RS256 (RSASSA-PKCS1-v1_5)
var rs256Param = new WebAuthnPublicKeyParam(
    Type: "public-key",
    Alg: -257  // RS256 algorithm identifier
);

// These parameters are used when generating registration options
var registrationOptions = await webAuthnService.GenerateRegistrationOptionsAsync(
    userId: "user-123",
    username: "johndoe",
    displayName: "John Doe"
);

// The PubKeyCredParams collection contains the supported algorithms
foreach (var param in registrationOptions.PubKeyCredParams)
{
    Console.WriteLine($"Supported algorithm: Type={param.Type}, Alg={param.Alg}");
}
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

## ITotpCredentialRepository

The `ITotpCredentialRepository` interface provides data access operations for managing Time-based One-Time Password (TOTP) credentials used for multi-factor authentication in the authorization server. It extends the base `IRepository<TotpCredential, string>` interface and adds TOTP-specific operations for retrieving and managing credentials by user ID, enabling efficient lookups and deletions of user-associated TOTP credentials.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a service or controller
public class MfaManagementService
{
    private readonly ITotpCredentialRepository _totpRepository;

    public MfaManagementService(ITotpCredentialRepository totpRepository)
    {
        _totpRepository = totpRepository;
    }

    public async Task ManageTotpCredentialsExample()
    {
        // Create a new TOTP credential for a user
        var newTotpCredential = new TotpCredential
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "user-123",
            SecretKey = "JBSWY3DPEHPK3PXP",
            Issuer = "MyAuthServer",
            AccountName = "john@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _totpRepository.CreateAsync(newTotpCredential);

        // Get a TOTP credential by ID
        var existingCredential = await _totpRepository.GetByIdAsync(newTotpCredential.Id);

        // Get all TOTP credentials
        var allCredentials = await _totpRepository.GetAllAsync();

        // Get TOTP credential by user ID
        var userTotp = await _totpRepository.GetByUserIdAsync("user-123");

        // Check if TOTP credential exists
        var exists = await _totpRepository.ExistsAsync(newTotpCredential.Id);

        // Update a TOTP credential
        if (existingCredential != null)
        {
            existingCredential.UpdatedAt = DateTime.UtcNow;
            await _totpRepository.UpdateAsync(existingCredential);
        }

        // Delete a TOTP credential
        await _totpRepository.DeleteAsync(existingCredential!);

        // Delete by ID
        await _totpRepository.DeleteByIdAsync(newTotpCredential.Id);

        // Delete TOTP credential by user ID
        await _totpRepository.DeleteByUserIdAsync("user-123");
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

## ClientService

The `ClientService` provides OAuth 2.0 client registration and management functionality for the authorization server. It handles client registration, configuration updates, secret rotation, and activation/deactivation of clients. This service is used by the dynamic client registration endpoint and administrative client management operations.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;

// Setup in Program.cs
builder.Services.AddScoped<ClientService>();

// Example usage in a controller or service
public class ClientManagementController
{
    private readonly ClientService _clientService;

    public ClientManagementController(ClientService clientService)
    {
        _clientService = clientService;
    }

    public async Task<IActionResult> RegisterConfidentialClient()
    {
        // Register a new confidential client
        var client = await _clientService.RegisterClientAsync(
            clientName: "My Web Application",
            isConfidential: true,
            redirectUris: new List<string> { "https://client.example.com/callback" },
            allowedGrantTypes: new List<string> { "authorization_code", "refresh_token" },
            allowedScopes: new List<string> { "openid", "profile", "email", "api:read" }
        );

        return CreatedAtAction(nameof(GetClient), new { id = client.ClientId }, client);
    }

    public async Task<IActionResult> RegisterPublicClient()
    {
        // Register a new public client (no client secret)
        var client = await _clientService.RegisterClientAsync(
            clientName: "Single Page Application",
            isConfidential: false,
            redirectUris: new List<string> { "https://app.example.com/callback" },
            allowedGrantTypes: new List<string> { "authorization_code" },
            allowedScopes: new List<string> { "openid", "profile" }
        );

        return Ok(client);
    }

    public async Task<IActionResult> UpdateClientConfiguration(string clientId)
    {
        // Get the existing client
        var client = await _clientRepository.GetByIdAsync(clientId);
        
        if (client == null)
        {
            return NotFound();
        }

        // Update client configuration
        var updatedClient = await _clientService.UpdateClientAsync(
            client: client,
            clientName: "Updated Client Name",
            redirectUris: new List<string> { "https://client.example.com/new-callback" },
            allowedScopes: new List<string> { "openid", "profile", "email", "api:read", "api:write" },
            corsOrigins: new List<string> { "https://client.example.com" }
        );

        return Ok(updatedClient);
    }

    public async Task<IActionResult> RotateClientSecret(string clientId)
    {
        // Get the client
        var client = await _clientRepository.GetByIdAsync(clientId);
        
        if (client == null || !client.IsConfidential)
        {
            return BadRequest("Client not found or not confidential");
        }

        // Rotate the client secret
        var newSecret = await _clientService.RotateClientSecretAsync(client);

        return Ok(new { message = "Client secret rotated successfully", newSecret });
    }

    public async Task<IActionResult> DeactivateClient(string clientId)
    {
        // Get the client
        var client = await _clientRepository.GetByIdAsync(clientId);
        
        if (client == null)
        {
            return NotFound();
        }

        // Deactivate the client
        await _clientService.DeactivateClientAsync(client);

        return Ok(new { message = "Client deactivated successfully" });
    }

    public async Task<IActionResult> ReactivateClient(string clientId)
    {
        // Get the client
        var client = await _clientRepository.GetByIdAsync(clientId);
        
        if (client == null)
        {
            return NotFound();
        }

        // Reactivate the client
        await _clientService.ReactivateClientAsync(client);

        return Ok(new { message = "Client reactivated successfully" });
    }

    public async Task<IActionResult> ValidateClientSecret(string clientId, string providedSecret)
    {
        // Get the client
        var client = await _clientRepository.GetByIdAsync(clientId);
        
        if (client == null)
        {
            return NotFound();
        }

        // Validate the client secret
        var isValid = _clientService.ValidateClientSecret(client, providedSecret);

        return Ok(new { isValid });
    }
}
```

## AuthorizationService

The `AuthorizationService` handles core authorization logic for the OAuth 2.0/OpenID Connect authorization server. It validates authorization requests, creates authorization grants, manages user consent, enforces PKCE validation, and cleans up expired grants. This service coordinates the authorization flow between clients, users, and the token issuance process.

```csharp
using DotnetAuthServer.Services;
using DotnetAuthServer.Domain.Models;

// Setup in Program.cs
builder.Services.AddScoped<AuthorizationService>();

// Example usage in an authorization controller
public class AuthorizationController
{
    private readonly AuthorizationService _authService;
    private readonly ITokenService _tokenService;

    public AuthorizationController(AuthorizationService authService, ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    public async Task<IActionResult> ValidateAndAuthorize(AuthorizationRequest authRequest)
    {
        // Validate the authorization request
        var validationResult = await _authService.ValidateAuthorizationRequestAsync(authRequest);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Error);
        }

        // Check if consent is required
        var consentRequired = await _authService.GetConsentPromptAsync(
            authRequest.ClientId,
            authRequest.UserId,
            authRequest.GetRequestedScopes()
        );

        if (consentRequired.RequiresConsent)
        {
            // Store consent decision and show consent page to user
            return View("Consent", consentRequired);
        }

        // Create authorization grant
        var grant = await _authService.CreateAuthorizationGrantAsync(
            authRequest.ClientId,
            authRequest.UserId,
            authRequest.GetRequestedScopes(),
            authRequest.CodeChallenge,
            authRequest.CodeChallengeMethod,
            authRequest.Nonce,
            authRequest.MaxAge
        );

        // Redirect to client with authorization code
        return Redirect($"{authRequest.RedirectUri}?code={grant.AuthorizationCode}");
    }

    public async Task<IActionResult> ValidatePkce(string clientId, string codeVerifier)
    {
        // Validate PKCE code verifier
        var isValid = await _authService.ValidatePkceCodeVerifier(clientId, codeVerifier);
        
        if (!isValid)
        {
            return BadRequest("Invalid PKCE code verifier");
        }

        return Ok(new { message = "PKCE validation successful" });
    }

    public async Task<IActionResult> CleanupExpiredGrants()
    {
        // Cleanup expired authorization grants
        await _authService.CleanupExpiredGrantsAsync();
        
        return Ok(new { message = "Expired grants cleaned up successfully" });
    }
}
```

## UserManagementService

The `UserManagementService` provides administrative CRUD operations over user accounts. It is designed for privileged, server-side callers such as admin APIs and background jobs, while end-user self-service operations are handled by the `UserService`. This service includes methods for creating, reading, updating, and deleting user accounts, as well as managing user roles, locking/unlocking accounts, and searching users.

```csharp
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Services;

// Setup in Program.cs
builder.Services.AddScoped<UserManagementService>();

// Example usage in an admin controller or background service
public class UserAdminController
{
    private readonly UserManagementService _userManagement;

    public UserAdminController(UserManagementService userManagement)
    {
        _userManagement = userManagement;
    }

    public async Task<IActionResult> CreateAdminUser()
    {
        // Create a new admin user
        var createRequest = new CreateUserRequest
        {
            Username = "admin",
            Email = "admin@example.com",
            Password = "SecureAdmin123!",
            FullName = "Administrator",
            Roles = new List<string> { "admin", "user" }
        };

        var createdUser = await _userManagement.CreateUserAsync(createRequest);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.UserId }, createdUser);
    }

    public async Task<IActionResult> GetAllUsers()
    {
        // Get all registered users
        var allUsers = await _userManagement.GetAllUsersAsync();
        return Ok(allUsers);
    }

    public async Task<IActionResult> GetUserDetails(string userId)
    {
        // Get a specific user by ID
        var user = await _userManagement.GetUserByIdAsync(userId);
        return Ok(user);
    }

    public async Task<IActionResult> SearchUsers(string query)
    {
        // Search users by username, email, or full name
        var results = await _userManagement.SearchUsersAsync(query);
        return Ok(results);
    }

    public async Task<IActionResult> UpdateUserProfile(string userId)
    {
        // Update user profile fields
        var updateRequest = new UpdateUserRequest
        {
            FullName = "Updated Name",
            IsActive = true,
            Attributes = new Dictionary<string, object>
            {
                { "department", "engineering" },
                { "max_sessions", 5 }
            }
        };

        var updatedUser = await _userManagement.UpdateUserAsync(userId, updateRequest);
        return Ok(updatedUser);
    }

    public async Task<IActionResult> AssignRoleToUser(string userId, string role)
    {
        // Assign a role to a user
        await _userManagement.AssignRoleAsync(userId, role);
        return Ok();
    }

    public async Task<IActionResult> RemoveRoleFromUser(string userId, string role)
    {
        // Remove a role from a user
        await _userManagement.RemoveRoleAsync(userId, role);
        return Ok();
    }

    public async Task<IActionResult> LockUserAccount(string userId, int hours = 24)
    {
        // Lock a user account for security reasons
        await _userManagement.LockUserAsync(userId, TimeSpan.FromHours(hours));
        return Ok();
    }

    public async Task<IActionResult> UnlockUserAccount(string userId)
    {
        // Unlock a user account
        await _userManagement.UnlockUserAsync(userId);
        return Ok();
    }

    public async Task<IActionResult> DeleteUser(string userId)
    {
        // Permanently delete a user account and revoke all tokens
        await _userManagement.DeleteUserAsync(userId);
        return NoContent();
    }
}
```

## UserService

The `UserService` handles user authentication and self-service management operations. It provides methods for authenticating users, creating new accounts, updating profiles, changing passwords, and managing user roles. This service is designed for end-user operations and integrates with the authorization server's authentication flows.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<UserService>();

// Example usage in a controller or service
public class UserController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> RegisterUser()
    {
        // Register a new user account
        var newUser = await _userService.CreateUserAsync(
            username: "johndoe",
            email: "john@example.com",
            password: "SecurePassword123!",
            fullName: "John Doe"
        );

        return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, newUser);
    }

    public async Task<IActionResult> AuthenticateUser()
    {
        // Authenticate an existing user
        var user = await _userService.AuthenticateAsync(
            username: "johndoe",
            password: "SecurePassword123!"
        );

        return Ok(new { user.UserId, user.Username, user.Email });
    }

    public async Task<IActionResult> UpdateProfile(string userId)
    {
        // Get the user entity
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound();
        }

        // Update user profile
        var updatedUser = await _userService.UpdateUserAsync(
            user: user,
            fullName: "John Doe Updated",
            attributes: new Dictionary<string, object>
            {
                { "department", "engineering" },
                { "max_sessions", 5 }
            }
        );

        return Ok(updatedUser);
    }

    public async Task<IActionResult> ChangePassword(string userId)
    {
        // Get the user entity
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound();
        }

        // Change user password
        await _userService.ChangePasswordAsync(
            user: user,
            currentPassword: "SecurePassword123!",
            newPassword: "NewSecurePassword456!"
        );

        return Ok(new { message = "Password changed successfully" });
    }

    public async Task<IActionResult> AssignRole(string userId)
    {
        // Get the user entity
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound();
        }

        // Assign a role to the user
        await _userService.AssignRoleAsync(user, "premium");

        return Ok(new { message = "Role assigned successfully" });
    }

    public async Task<IActionResult> RemoveRole(string userId)
    {
        // Get the user entity
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound();
        }

        // Remove a role from the user
        await _userService.RemoveRoleAsync(user, "premium");

        return Ok(new { message = "Role removed successfully" });
    }
}
```

## TotpService

The `TotpService` implements TOTP (RFC 6238) multi-factor authentication for the authorization server. It handles enrollment initiation with secret generation and QR code provisioning URIs, setup confirmation via code verification, ongoing code verification with configurable time-step windows, and backup code redemption. The service also provides status queries and MFA disablement.

```csharp
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Services;

// Setup in Program.cs
builder.Services.AddScoped<TotpService>();

// Example usage in a controller or service
public class MfaController
{
    private readonly TotpService _totpService;

    public MfaController(TotpService totpService)
    {
        _totpService = totpService;
    }

    public async Task<IActionResult> InitiateTotpSetup(string userId, string username)
    {
        // Begin enrollment - generates secret, backup codes, and provisioning URI
        var setupResponse = await _totpService.InitiateSetupAsync(userId, username);

        // Display the secret key and QR code URI to the user (only once!)
        Console.WriteLine($"Secret Key: {setupResponse.SecretKey}");
        Console.WriteLine($"Scan this URI with your authenticator app: {setupResponse.ProvisioningUri}");

        // Show backup codes for offline storage (display only once!)
        Console.WriteLine("Backup codes (store these securely):");
        foreach (var code in setupResponse.BackupCodes)
        {
            Console.WriteLine($"  {code}");
        }

        return Ok(new { setupResponse.SecretKey, setupResponse.ProvisioningUri });
    }

    public async Task<IActionResult> ConfirmSetup(string userId, string code)
    {
        // Confirm the setup by verifying the user's TOTP code
        await _totpService.ConfirmSetupAsync(userId, code);

        return Ok(new { message = "TOTP MFA setup confirmed and enabled" });
    }

    public async Task<IActionResult> VerifyTotpCode(string userId, string code)
    {
        // Verify a TOTP code or backup code
        var isValid = await _totpService.VerifyAsync(userId, code);

        if (isValid)
        {
            return Ok(new { message = "MFA verification successful" });
        }

        return Unauthorized(new { message = "Invalid MFA code" });
    }

    public async Task<IActionResult> GetMfaStatus(string userId)
    {
        // Check if MFA is enabled and get usage statistics
        var status = await _totpService.GetStatusAsync(userId);

        return Ok(new {
            status.IsEnabled,
            status.EnabledAt,
            status.LastUsedAt,
            status.BackupCodesRemaining
        });
    }

    public async Task<IActionResult> DisableMfa(string userId)
    {
        // Disable and remove TOTP MFA for the user
        await _totpService.DisableMfaAsync(userId);

        return Ok(new { message = "TOTP MFA disabled successfully" });
    }

    public void ValidateTotpCodeManually()
    {
        // You can also manually verify codes using the static methods
        var secretKey = "JBSWY3DPEHPK3PXP";
        var code = "123456";
        var isValid = TotpService.VerifyTotpCode(secretKey, code);
        Console.WriteLine(isValid ? "Code is valid" : "Code is invalid");

        // Generate provisioning URI for QR code generation
        var provisioningUri = TotpService.BuildProvisioningUri(
            secretKey, 
            "user@example.com", 
            "MyAuthServer"
        );
        Console.WriteLine(provisioningUri);

        // Base32 encoding/decoding utilities
        var bytes = TotpService.DecodeBase32(secretKey);
        var encoded = TotpService.EncodeBase32(bytes);
    }
}
```

## ConsentRepository

The `ConsentRepository` class provides an in-memory implementation of the `IConsentRepository` interface for managing OAuth 2.0 user consent decisions in the authorization server. It stores user approvals or denials of requested scopes for specific OAuth clients, enabling the authorization server to enforce scope-based access control and remember consent decisions across sessions. This repository serves as the data access layer for consent management operations.

```csharp
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;

// Example usage in a service or controller
public class ConsentManagementService
{
    private readonly ConsentRepository _consentRepository;

    public ConsentManagementService()
    {
        _consentRepository = new ConsentRepository();
    }

    public async Task ManageConsentsExample()
    {
        // Create a new consent record
        var newConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user-123",
            ClientId = "web-client",
            GrantedScopes = "openid profile email api:read",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90)
        };
        await _consentRepository.CreateAsync(newConsent);

        // Get a consent by ID
        var existingConsent = await _consentRepository.GetByIdAsync(newConsent.ConsentId);

        // Get all consents
        var allConsents = await _consentRepository.GetAllAsync();

        // Get consent for specific user and client
        var userClientConsent = await _consentRepository.GetByUserAndClientAsync("user-123", "web-client");

        // Get all consents for a user
        var userConsents = await _consentRepository.GetByUserIdAsync("user-123");

        // Get all consents for a client
        var clientConsents = await _consentRepository.GetByClientIdAsync("web-client");

        // Check if consent exists
        var exists = await _consentRepository.ExistsAsync(newConsent.ConsentId);

        // Update a consent
        if (existingConsent != null)
        {
            existingConsent.Grant("openid profile email api:read api:write", "192.168.1.100", "Mozilla/5.0");
            await _consentRepository.UpdateAsync(existingConsent);
        }

        // Revoke a consent
        await _consentRepository.DeleteAsync(existingConsent!);

        // Revoke all consents for a user
        var revokedCount = await _consentRepository.RevokeUserConsentsAsync("user-123");
    }
}
```

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;

// Example usage in a service or controller
public class ConsentManagementService
{
    private readonly IConsentRepository _consentRepository;

    public ConsentManagementService(IConsentRepository consentRepository)
    {
        _consentRepository = consentRepository;
    }

    public async Task ManageConsentsExample()
    {
        // Create a new consent record
        var newConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user-123",
            ClientId = "web-client",
            GrantedScopes = "openid profile email api:read",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(90) // Persistent consent
        };
        await _consentRepository.CreateAsync(newConsent);

        // Get a consent by ID
        var existingConsent = await _consentRepository.GetByIdAsync("consent-123");

        // Get all consents
        var allConsents = await _consentRepository.GetAllAsync();

        // Get consent for specific user and client
        var userClientConsent = await _consentRepository.GetByUserAndClientAsync("user-123", "web-client");

        // Get all consents for a user
        var userConsents = await _consentRepository.GetByUserIdAsync("user-123");

        // Get all consents for a client
        var clientConsents = await _consentRepository.GetByClientIdAsync("web-client");

        // Check if consent exists
        var exists = await _consentRepository.ExistsAsync("consent-123");

        // Update a consent
        if (existingConsent != null)
        {
            existingConsent.Grant("openid profile email api:read api:write", "192.168.1.100", "Mozilla/5.0");
            await _consentRepository.UpdateAsync(existingConsent);
        }

        // Revoke a consent
        await _consentRepository.DeleteAsync(existingConsent!);

        // Revoke all consents for a user
        await _consentRepository.RevokeAllUserConsentsAsync("user-123");
    }
}
```

```csharp
using DotnetAuthServer.Domain.Entities;

// Define a consent for API access
var consent = new Consent
{
    ConsentId = Guid.NewGuid().ToString(),
    UserId = "user-123",
    ClientId = "mobile-app",
    GrantedScopes = "openid profile email",
    IsRevoked = false,
    RevokedAt = null,
    RevokedReason = null,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddDays(30)
};

// Grant additional scopes
consent.Grant("api:read api:write", "192.168.1.100", "MobileApp/1.2.3");

// Check if consent is valid and approved
if (consent.IsValidAndApproved())
{
    Console.WriteLine("Consent is valid and approved");
}

// Get all granted scopes
var grantedScopes = consent.GetGrantedScopes();
foreach (var scope in grantedScopes)
{
    Console.WriteLine($"Granted scope: {scope}");
}

// Revoke consent
consent.Revoke("User requested revocation");

// Check if consent is revoked
if (consent.IsRevoked)
{
    Console.WriteLine("Consent has been revoked");
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

## SessionStateService

The `SessionStateService` manages OAuth2 session states during authorization flows, storing temporary state needed for multi-step flows like the authorization code flow. It uses in-memory storage suitable for single-server deployments, with methods for creating, retrieving, updating, and completing sessions. Sessions automatically expire after 10 minutes to prevent replay attacks.

```csharp
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<SessionStateService>();

// Example usage in an authorization controller
public class AuthorizationController
{
    private readonly SessionStateService _sessionService;

    public AuthorizationController(SessionStateService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<IActionResult> InitiateAuthorization(string clientId, string redirectUri, string scopes, string? nonce = null)
    {
        // Create a new session state
        var stateId = _sessionService.CreateSession(clientId, redirectUri, scopes, nonce);

        // Store stateId in a secure cookie or return to client
        Response.Cookies.Append("oauth_state", stateId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        // Redirect to authorization endpoint with state parameter
        var authorizationUrl = $"/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scopes}&state={stateId}";
        return Redirect(authorizationUrl);
    }

    public async Task<IActionResult> HandleAuthorizationCallback(string stateId, string code, string? userId = null)
    {
        // Retrieve the session state
        var session = _sessionService.GetSession(stateId);

        if (session == null)
        {
            return BadRequest("Invalid or expired session state");
        }

        // Update session with user information after authentication
        _sessionService.UpdateSession(stateId, userId: userId, grantedScopes: session.RequestedScopes);

        // Exchange authorization code for tokens
        var tokenResponse = await _tokenService.ExchangeCodeForTokensAsync(
            clientId: session.ClientId,
            code: code,
            redirectUri: session.RedirectUri,
            state: stateId
        );

        // Complete the session to prevent replay attacks
        _sessionService.CompleteSession(stateId);

        // Redirect to client with tokens
        return Redirect($"{session.RedirectUri}?code={tokenResponse.AccessToken}");
    }

    public async Task<IActionResult> CleanupSessions()
    {
        // Periodically cleanup expired sessions (e.g., as background job)
        var cleanupCount = _sessionService.CleanupExpiredSessions();
        return Ok($"Cleaned up {cleanupCount} expired sessions");
    }

    public IActionResult GetSessionStats()
    {
        // Get current session count for monitoring
        var activeSessions = _sessionService.GetActiveSessionCount();
        return Ok(new { ActiveSessions = activeSessions });
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

## PolicyEnforcementService

The `PolicyEnforcementService` provides role-based and attribute-based access control for the authorization server. It evaluates policies to determine if a user is allowed to perform actions or access resources. When Open Policy Agent (OPA) integration is enabled, policy decisions can be delegated to an external OPA REST API; otherwise, the service uses built-in policy evaluation.

```csharp
using DotnetAuthServer.Services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<PolicyEnforcementService>();

// Example usage in a controller or service
public class ResourceController
{
    private readonly PolicyEnforcementService _policyService;

    public ResourceController(PolicyEnforcementService policyService)
    {
        _policyService = policyService;
    }

    public IActionResult AccessAdminResource(ClaimsPrincipal user)
    {
        // Check if user satisfies the "AdminOnly" policy
        bool isAdmin = _policyService.EvaluatePolicy("AdminOnly", user);

        if (!isAdmin)
        {
            return Forbid();
        }

        return Ok("Access granted to admin resource");
    }

    public async Task<IActionResult> AccessSensitiveResourceAsync(ClaimsPrincipal user)
    {
        // Async evaluation for better performance
        bool hasAccess = await _policyService.EvaluatePolicyAsync("SensitiveDataAccess", user);

        if (!hasAccess)
        {
            return Forbid("Insufficient privileges");
        }

        return Ok("Access granted to sensitive resource");
    }

    public IActionResult AccessDepartmentResource(ClaimsPrincipal user)
    {
        // Create a custom policy dynamically
        var departmentPolicy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Attribute,
                    Attribute = "department",
                    Values = new List<string> { "engineering", "finance" },
                    Match = PolicyMatchMode.Any
                },
                new PolicyRule
                {
                    Type = PolicyRuleType.Role,
                    Values = new List<string> { "manager", "director" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };

        bool hasAccess = _policyService.EvaluatePolicy(departmentPolicy, user);

        if (hasAccess)
        {
            return Ok("Access granted to department resource");
        }

        return Forbid("User does not have required department or role");
    }
}
```

## SecretsService

The `SecretsService` provides cryptographically secure operations for generating, hashing, and verifying secrets in the authorization server. It uses PBKDF2 with SHA256 for secure hashing, constant-time comparison to prevent timing attacks, and cryptographically secure random number generation for all secret values. This service is essential for securely handling client secrets, API keys, user passwords, and tokens.

```csharp
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<SecretsService>();

// Example usage in a controller or service
public class SecretManagementController
{
    private readonly SecretsService _secretsService;

    public SecretManagementController(SecretsService secretsService)
    {
        _secretsService = secretsService;
    }

    public IActionResult GenerateClientSecret()
    {
        // Generate a secure client secret for OAuth 2.0 client registration
        var clientSecret = _secretsService.GenerateSecureSecret(length: 48);
        Console.WriteLine($"Generated client secret: {SecretsService.MaskSecret(clientSecret)}");
        
        return Ok(new { secret = SecretsService.MaskSecret(clientSecret) });
    }

    public IActionResult HashAndStorePassword(string plainPassword)
    {
        // Hash a user password for secure storage
        var hash = _secretsService.HashSecret(plainPassword, iterations: 15000);
        Console.WriteLine($"Password hash stored: {hash}");
        
        // Store hash.Salt, hash.Hash, hash.Iterations, hash.Algorithm in database
        return Ok(new { 
            algorithm = hash.Algorithm,
            iterations = hash.Iterations,
            salt = hash.Salt // Store this securely!
        });
    }

    public IActionResult VerifyUserPassword(string plainPassword, SecretsService.SecretHash storedHash)
    {
        // Verify a user's password against stored hash
        bool isValid = _secretsService.VerifySecret(plainPassword, storedHash);
        
        return Ok(new { isValid });
    }

    public IActionResult GenerateApiToken()
    {
        // Generate a secure API token for service-to-service authentication
        var apiToken = _secretsService.GenerateToken(length: 64);
        Console.WriteLine($"Generated API token: {SecretsService.MaskSecret(apiToken)}");
        
        return Ok(new { token = SecretsService.MaskSecret(apiToken) });
    }

    public IActionResult MaskSensitiveData()
    {
        // Mask secrets for logging/display purposes
        var clientSecret = "s3cr3tP@ssw0rd123456789";
        var masked = SecretsService.MaskSecret(clientSecret);
        
        Console.WriteLine($"Original: {clientSecret}");
        Console.WriteLine($"Masked: {masked}"); // Shows: s3c***789
        
        return Ok(new { maskedSecret = masked });
    }
}
```

## License

MIT - see [LICENSE](LICENSE).

## PkceValidationService

The `PkceValidationService` implements RFC 7636 (PKCE - Proof Key for Code Exchange) to protect authorization code flows from interception attacks. It generates cryptographically secure code verifiers and challenges, validates code verifier/challenge pairs, and determines when PKCE is required based on client type and configuration.

```csharp
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.AddScoped<PkceValidationService>();

// Example usage in a token endpoint handler
public class TokenController
{
    private readonly PkceValidationService _pkceValidation;

    public TokenController(PkceValidationService pkceValidation)
    {
        _pkceValidation = pkceValidation;
    }

    public async Task<IActionResult> ExchangeAuthorizationCode(TokenRequest request)
    {
        // Generate code verifier and challenge during authorization request
        var codeVerifier = _pkceValidation.GenerateCodeVerifier();
        var codeChallenge = _pkceValidation.GenerateCodeChallenge(codeVerifier, "S256");
        
        // Store verifier and challenge temporarily (in session/state)
        _sessionService.StorePkceValues(request.State, codeVerifier, codeChallenge);

        // Later, during token exchange, validate the code verifier
        var isValid = _pkceValidation.ValidateCodeVerifier(
            request.CodeVerifier, 
            storedCodeChallenge,
            "S256"
        );

        if (!isValid)
        {
            return BadRequest("Invalid code verifier");
        }

        // Check if PKCE is required for this client type
        var isConfidentialClient = true; // or false for public clients
        var pkceRequired = _pkceValidation.IsPkceRequired(isConfidentialClient);

        // Validate challenge format
        var isValidChallenge = _pkceValidation.IsValidChallenge(codeChallenge, "S256");

        return Ok(new { success = true, requiresPkce = pkceRequired });
    }
}
```
