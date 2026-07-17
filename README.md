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

## CacheOptions

The `CacheOptions` class provides configuration for the authorization server's caching layer. It controls cache backend selection, expiration times, and size limits for various cached data types including clients, users, scopes, authorization grants, and JWKS keys. This configuration is essential for optimizing performance and managing memory usage in production environments.

```csharp
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));

// Example usage in a service
public class CacheConfigurationService
{
    private readonly CacheOptions _cacheOptions;

    public CacheConfigurationService(IOptions<CacheOptions> cacheOptions)
    {
        _cacheOptions = cacheOptions.Value;
    }

    public void ConfigureCacheSettings()
    {
        // Configure memory cache settings
        var memoryCacheOptions = new CacheOptions
        {
            Enabled = true,
            Backend = "Memory",
            DefaultExpirationSeconds = 3600, // 1 hour
            MaxEntries = 10000,
            ExpirationScanIntervalSeconds = 300, // 5 minutes
            ItemExpirations = new CacheItemExpirations
            {
                ClientSeconds = 3600,    // 1 hour for client info
                UserSeconds = 1800,       // 30 minutes for user info
                ScopeSeconds = 7200,      // 2 hours for scope definitions
                GrantSeconds = 300,       // 5 minutes for authorization grants
                JwksSeconds = 86400       // 24 hours for JWKS keys
            }
        };

        // Configure Redis cache settings
        var redisCacheOptions = new CacheOptions
        {
            Enabled = true,
            Backend = "Redis",
            ConnectionString = "localhost:6379,password=secret",
            DefaultExpirationSeconds = 3600,
            MaxEntries = 100000,
            ItemExpirations = new CacheItemExpirations
            {
                ClientSeconds = 7200,    // 2 hours for client info
                UserSeconds = 3600,       // 1 hour for user info
                ScopeSeconds = 14400,     // 4 hours for scope definitions
                GrantSeconds = 600,       // 10 minutes for authorization grants
                JwksSeconds = 86400       // 24 hours for JWKS keys
            }
        };
    }
}
```

## MemoryCacheService

The `MemoryCacheService` provides an in-memory caching implementation using `ConcurrentDictionary` for thread-safe operations. It's designed for single-server deployments and offers automatic expiration checking, pattern-based cache removal, and atomic get-or-set operations to prevent thundering herd problems.

```csharp
using DotnetAuthServer.Caching;
using Microsoft.Extensions.DependencyInjection;

// Register in Program.cs
builder.Services.AddSingleton<MemoryCacheService>();

// Example usage in a service or controller
public class DataService
{
private readonly MemoryCacheService _cache;

public DataService(MemoryCacheService cache)
{
_cache = cache;
}

public async Task<User?> GetUserAsync(string userId)
{
// Try to get from cache first
var cachedUser = await _cache.GetAsync<User>($"user:{userId}");
if (cachedUser is not null)
{
return cachedUser;
}

// Cache miss - fetch from database
var user = await _userRepository.GetByIdAsync(userId);

// Cache the result for 30 minutes
if (user is not null)
{
await _cache.SetAsync($"user:{userId}", user, TimeSpan.FromMinutes(30));
}

return user;
}

public async Task<Product[]> GetPopularProductsAsync()
{
// Get or set with factory function - prevents thundering herd
var products = await _cache.GetOrSetAsync(
key: "popular_products",
factory: async _ => await _productRepository.GetPopularAsync(),
// Cache for 1 hour
expiration: TimeSpan.FromHours(1)
);

return products ?? Array.Empty<Product>();
}

public async Task InvalidateUserCacheAsync(string userId)
{
// Remove specific user cache
await _cache.RemoveAsync($"user:{userId}");

// Remove all sessions for this user
await _cache.RemoveByPatternAsync($"user_sessions:{userId}*");
}

public async Task ClearAllCacheAsync()
{
// Clear entire cache when needed
await _cache.ClearAsync();
}
}
```

## LoggingOptions

The `LoggingOptions` class provides configuration for the authorization server's logging system. It controls log verbosity, formatting, and what information is included in logs, making it essential for debugging, security auditing, and performance monitoring while protecting sensitive data.

```csharp
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.Configure<LoggingOptions>(builder.Configuration.GetSection("Logging"));

// Example usage in a service
public class LoggingConfigurationService
{
private readonly LoggingOptions _loggingOptions;

public LoggingConfigurationService(IOptions<LoggingOptions> loggingOptions)
{
_loggingOptions = loggingOptions.Value;
}

public void ConfigureLoggingSettings()
{
// Configure basic logging settings
var loggingOptions = new LoggingOptions
{
MinimumLevel = LogLevel.Debug, // Capture all logs for development
LogSensitiveData = false, // Never log sensitive data in production
LogRequestBodies = true, // Log request/response bodies for debugging
LogRequestTiming = true, // Include timing information
MaxBodyLogLength = 2000, // Increase body log limit for large payloads
ExcludedPaths = new List<string> { "/health", "/swagger", "/.well-known" },
IncludeCorrelationId = true, // Add correlation IDs to all logs
StructuredLogging = false // Use plain text format for development
};

// Configure production logging settings
var productionLogging = new LoggingOptions
{
MinimumLevel = LogLevel.Information, // Only important logs in production
LogSensitiveData = false, // Always false in production
LogRequestBodies = false, // Disable body logging for performance
LogRequestTiming = true, // Keep timing for performance monitoring
MaxBodyLogLength = 1000, // Reasonable limit for error cases
ExcludedPaths = new List<string> { "/health", "/swagger", "/.well-known", "/oauth/token" },
IncludeCorrelationId = true, // Correlation IDs help with debugging
StructuredLogging = true // JSON format for log aggregation
};
}
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

## DomainEntityTests

The `DomainEntityTests` class provides unit tests for domain entity classes in the authorization server. It verifies core domain logic and behavior for entities like `User`, `Client`, and `RefreshToken`, ensuring that authentication, authorization, and token management operations work correctly according to the domain model's invariants.


```csharp
using DotnetAuthServer.Domain.Entities;
using Xunit;

// Test user login behavior
var user = new User
{
    UserId = "user-123",
    Username = "testuser",
    Email = "test@example.com",
    PasswordHash = "hashed-password"
};

// Simulate failed login attempts
for (int i = 0; i < 4; i++)
{
    user.RecordFailedLogin();
}

Assert.False(user.IsLocked());
Assert.Equal(4, user.FailedLoginAttempts);

// Simulate successful login
user.RecordSuccessfulLogin();
Assert.Equal(0, user.FailedLoginAttempts);
Assert.NotNull(user.LastLoginAt);

// Test client validation
var client = new Client
{
    ClientId = "web-client",
    ClientName = "Web Client",
    IsConfidential = true,
    ClientSecretHash = "secret-hash",
    RedirectUris = new List<string> { "https://client.example.com/callback" },
    AllowedGrantTypes = new List<string> { "authorization_code", "refresh_token" }
};

Assert.True(client.IsValid());
Assert.True(client.IsGrantTypeAllowed("authorization_code"));
Assert.True(client.IsRedirectUriValid("https://CLIENT.EXAMPLE.COM/callback"));

// Test refresh token rotation
var token = new RefreshToken
{
    TokenId = Guid.NewGuid().ToString(),
    TokenHash = "original-hash",
    ClientId = "client-123",
    UserId = "user-123",
    GrantedScopes = "openid profile email",
    ExpiresAt = DateTime.UtcNow.AddDays(30)
};

var originalVersion = token.Version;
token.Rotate();

Assert.Equal(originalVersion + 1, token.Version);
Assert.Equal("original-hash", token.PreviousTokenHash);
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

## IAuthorizationGrantRepository

The `IAuthorizationGrantRepository` interface provides data access operations for managing OAuth 2.0 authorization grants in the authorization server. It extends the base `IRepository<AuthorizationGrant, string>` interface and adds grant-specific operations for retrieving grants by authorization code, user ID, client ID, and managing expired grants. This repository serves as the data access layer for authorization grant management operations during the authorization code flow.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a token service or controller
public class AuthorizationGrantManagementService
{
    private readonly IAuthorizationGrantRepository _grantRepository;

    public AuthorizationGrantManagementService(IAuthorizationGrantRepository grantRepository)
    {
        _grantRepository = grantRepository;
    }

    public async Task ManageAuthorizationGrantsExample()
    {
        // Create a new authorization grant
        var newGrant = new AuthorizationGrant
        {
            GrantId = Guid.NewGuid().ToString(),
            Code = "auth-code-123",
            UserId = "user-123",
            ClientId = "web-client",
            Scope = "openid profile email api:read",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CodeChallenge = "E9Melhoa2OwvFrEMTJks93UTHBbYu3_KJDijOhbwNY",
            CodeChallengeMethod = "S256",
            RedirectUri = "https://client.example.com/callback"
        };
        await _grantRepository.CreateAsync(newGrant);

        // Get a grant by ID
        var existingGrant = await _grantRepository.GetByIdAsync(newGrant.GrantId);

        // Get all grants
        var allGrants = await _grantRepository.GetAllAsync();

        // Get grant by authorization code
        var grantByCode = await _grantRepository.GetByCodeAsync("auth-code-123");

        // Get all grants for a specific user
        var userGrants = await _grantRepository.GetByUserIdAsync("user-123");

        // Get all grants for a specific client
        var clientGrants = await _grantRepository.GetByClientIdAsync("web-client");

        // Check if grant exists
        var exists = await _grantRepository.ExistsAsync(newGrant.GrantId);

        // Update a grant
        if (existingGrant != null)
        {
            existingGrant.UpdatedAt = DateTime.UtcNow;
            await _grantRepository.UpdateAsync(existingGrant);
        }

        // Delete a grant
        await _grantRepository.DeleteAsync(existingGrant!);

        // Delete by ID
        await _grantRepository.DeleteByIdAsync(newGrant.GrantId);

        // Cleanup expired grants
        await _grantRepository.DeleteExpiredAsync();
    }
}
```

## IRefreshTokenRepository

The `IRefreshTokenRepository` interface provides data access operations for managing OAuth 2.0 refresh tokens in the authorization server. It extends the base `IRepository<RefreshToken, string>` interface and adds refresh token-specific operations for retrieving tokens by hash, user ID, client ID, managing valid tokens, revoking all tokens for a user, and cleaning up expired tokens. This repository serves as the data access layer for refresh token management operations.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a token service or controller
public class TokenManagementService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenManagementService(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task ManageRefreshTokensExample()
    {
        // Create a new refresh token
        var newToken = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = "hashed-token-value",
            UserId = "user-123",
            ClientId = "web-client",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Scope = "openid profile email api:read"
        };
        await _refreshTokenRepository.CreateAsync(newToken);

        // Get a refresh token by ID
        var existingToken = await _refreshTokenRepository.GetByIdAsync(newToken.TokenId);

        // Get all refresh tokens
        var allTokens = await _refreshTokenRepository.GetAllAsync();

        // Get refresh token by token hash
        var tokenByHash = await _refreshTokenRepository.GetByTokenHashAsync("hashed-token-value");

        // Get all tokens for a specific user
        var userTokens = await _refreshTokenRepository.GetByUserIdAsync("user-123");

        // Get all tokens for a specific client
        var clientTokens = await _refreshTokenRepository.GetByClientIdAsync("web-client");

        // Get all valid tokens for a user
        var validUserTokens = await _refreshTokenRepository.GetValidTokensByUserAsync("user-123");

        // Check if token exists
        var exists = await _refreshTokenRepository.ExistsAsync(newToken.TokenId);

        // Update a refresh token
        if (existingToken != null)
        {
            existingToken.UpdatedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(existingToken);
        }

        // Revoke all tokens for a user (e.g., after password change)
        await _refreshTokenRepository.RevokeAllUserTokensAsync("user-123", "Password changed - all sessions invalidated");

        // Delete a refresh token
        await _refreshTokenRepository.DeleteAsync(existingToken!);

        // Delete by ID
        await _refreshTokenRepository.DeleteByIdAsync(newToken.TokenId);

        // Cleanup expired tokens
        await _refreshTokenRepository.DeleteExpiredAsync();
    }
}
```

## AuthServerOptions

The `AuthServerOptions` class provides configuration for the authorization server's core authentication and token issuance settings. It controls JWT signing, token lifetimes, security policies, database connectivity, and various behavioral options that determine how the authorization server operates in different environments.

```csharp
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.Configure<AuthServerOptions>(builder.Configuration.GetSection("AuthServer"));

// Example usage in a service
public class AuthServerConfigurationService
{
    private readonly AuthServerOptions _authOptions;

    public AuthServerConfigurationService(IOptions<AuthServerOptions> authOptions)
    {
        _authOptions = authOptions.Value;
    }

    public void ConfigureAuthSettings()
    {
        // Configure JWT signing settings
        var jwtOptions = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "your-256-bit-secret-key-here-at-least-32-characters",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600, // 1 hour
            RefreshTokenLifetimeSeconds = 2592000, // 30 days
            AuthorizationCodeLifetimeSeconds = 600, // 10 minutes
            ClockSkewToleranceSeconds = 300 // 5 minutes
        };

        // Configure security policies
        var securityOptions = new AuthServerOptions
        {
            RequirePkceForAllClients = true, // Enforce PKCE for all clients
            AutoRefreshTokenRotation = true, // Auto-rotate refresh tokens
            MaxRefreshTokenGenerations = 5, // Maximum refresh token generations
            FailedLoginAttemptThreshold = 5, // Lock account after 5 failed attempts
            AccountLockoutDurationMinutes = 15, // Lock for 15 minutes
            RequireUserConsent = true // Require explicit user consent
        };

        // Configure database settings
        var dbOptions = new AuthServerOptions
        {
            DatabaseConnectionString = "Server=localhost;Database=AuthServer;User Id=sa;Password=YourPassword123;",
            UseInMemoryDatabase = false // Use real database in production
        };

        // Configure supported scopes and grant types
        var featuresOptions = new AuthServerOptions
        {
            SupportedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token", "client_credentials", "password" }
        };
    }
}
```

## JsonTokenResponseFormatter

The `JsonTokenResponseFormatter` class provides utilities for serializing and deserializing OAuth 2.0 token responses according to RFC 6749. It ensures consistent JSON formatting with snake_case field names and compact output suitable for client parsing, while handling both standard and non-standard fields gracefully.

```csharp
using DotnetAuthServer.Formatters;
using DotnetAuthServer.Domain.Models;

// Format a token response to JSON
var tokenResponse = new TokenResponse
{
    AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    TokenType = "Bearer",
    ExpiresIn = 3600,
    RefreshToken = "refresh-token-xyz",
    Scope = "openid profile email api:read"
};

// Serialize to JSON (snake_case fields)
string jsonResponse = JsonTokenResponseFormatter.FormatTokenResponse(tokenResponse);
// Output: {"access_token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","token_type":"Bearer","expires_in":3600,"refresh_token":"refresh-token-xyz","scope":"openid profile email api:read"}

// Parse JSON back to TokenResponse
var parsedResponse = JsonTokenResponseFormatter.ParseTokenResponse(jsonResponse);
if (parsedResponse != null)
{
    Console.WriteLine($"Access Token: {parsedResponse.AccessToken}");
    Console.WriteLine($"Expires In: {parsedResponse.ExpiresIn} seconds");
}
```

```csharp
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.Configure<AuthServerOptions>(builder.Configuration.GetSection("AuthServer"));

// Example usage in a service
public class AuthServerConfigurationService
{
    private readonly AuthServerOptions _authOptions;

    public AuthServerConfigurationService(IOptions<AuthServerOptions> authOptions)
    {
        _authOptions = authOptions.Value;
    }

    public void ConfigureAuthSettings()
    {
        // Configure JWT signing settings
        var jwtOptions = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "your-256-bit-secret-key-here-at-least-32-characters",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600, // 1 hour
            RefreshTokenLifetimeSeconds = 2592000, // 30 days
            AuthorizationCodeLifetimeSeconds = 600, // 10 minutes
            ClockSkewToleranceSeconds = 300 // 5 minutes
        };

        // Configure security policies
        var securityOptions = new AuthServerOptions
        {
            RequirePkceForAllClients = true, // Enforce PKCE for all clients
            AutoRefreshTokenRotation = true, // Auto-rotate refresh tokens
            MaxRefreshTokenGenerations = 5, // Maximum refresh token generations
            FailedLoginAttemptThreshold = 5, // Lock account after 5 failed attempts
            AccountLockoutDurationMinutes = 15, // Lock for 15 minutes
            RequireUserConsent = true // Require explicit user consent
        };

        // Configure database settings
        var dbOptions = new AuthServerOptions
        {
            DatabaseConnectionString = "Server=localhost;Database=AuthServer;User Id=sa;Password=YourPassword123;",
            UseInMemoryDatabase = false // Use real database in production
        };

        // Configure supported scopes and grant types
        var featuresOptions = new AuthServerOptions
        {
            SupportedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token", "client_credentials", "password" }
        };

        // Combine all configurations
        var fullConfig = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? "default-secret-key-32-chars-minimum",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeSeconds = 2592000,
            AuthorizationCodeLifetimeSeconds = 600,
            RequirePkceForAllClients = true,
            AutoRefreshTokenRotation = true,
            MaxRefreshTokenGenerations = 5,
            ClockSkewToleranceSeconds = 300,
            DatabaseConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"),
            UseInMemoryDatabase = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")),
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 15,
            RequireUserConsent = true,
            SupportedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token", "client_credentials", "password" }
        };
    }
}
```

## IClientRepository

The `IClientRepository` interface provides data access operations for managing OAuth 2.0 clients in the authorization server. It extends the base `IRepository<Client, string>` interface and adds client-specific operations for retrieving clients by client ID, managing active clients, searching clients by name or identifier, and checking client existence. This repository serves as the data access layer for OAuth client management operations.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a client management service or controller
public class ClientManagementService
{
    private readonly IClientRepository _clientRepository;

    public ClientManagementService(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task ManageClientsExample()
    {
        // Create a new OAuth client
        var newClient = new Client
        {
            ClientId = "web-client-123",
            ClientName = "My Web Application",
            ClientSecret = "s3cr3tP@ssw0rd",
            IsConfidential = true,
            GrantTypes = new List<string> { "authorization_code", "refresh_token" },
            RedirectUris = new List<string> { "https://client.example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "email", "api:read" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _clientRepository.CreateAsync(newClient);

        // Get a client by ID
        var existingClient = await _clientRepository.GetByIdAsync("web-client-123");

        // Get all clients
        var allClients = await _clientRepository.GetAllAsync();

        // Get active clients
        var activeClients = await _clientRepository.GetActiveClientsAsync();

        // Get client by client ID
        var clientByClientId = await _clientRepository.GetByClientIdAsync("web-client-123");

        // Check if client exists
        var exists = await _clientRepository.ExistsAsync("web-client-123");

        // Search clients by query
        var searchResults = await _clientRepository.SearchAsync("web");

        // Update a client
        if (existingClient != null)
        {
            existingClient.ClientName = "Updated Web Application";
            existingClient.AllowedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" };
            await _clientRepository.UpdateAsync(existingClient);
        }

        // Delete a client
        await _clientRepository.DeleteAsync(existingClient!);

        // Delete by ID
        await _clientRepository.DeleteByIdAsync("web-client-123");
    }
}
```

## IUserRepository

The `IUserRepository` interface provides data access operations for managing user accounts in the authorization server. It extends the base `IRepository<User, string>` interface and adds user-specific operations for retrieving users by username, email, role, and searching users by name or email. This repository serves as the data access layer for user management operations.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a user management service or controller
public class UserManagementService
{
    private readonly IUserRepository _userRepository;

    public UserManagementService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task ManageUsersExample()
    {
        // Create a new user
        var newUser = new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = "johndoe",
            Email = "john.doe@example.com",
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
        await _userRepository.CreateAsync(newUser);

        // Get a user by ID
        var existingUser = await _userRepository.GetByIdAsync(newUser.UserId);

        // Get all users
        var allUsers = await _userRepository.GetAllAsync();

        // Get a user by username
        var userByUsername = await _userRepository.GetByUsernameAsync("johndoe");

        // Get a user by email
        var userByEmail = await _userRepository.GetByEmailAsync("john.doe@example.com");

        // Get all users with a specific role
        var adminUsers = await _userRepository.GetByRoleAsync("admin");

        // Get active users only
        var activeUsers = await _userRepository.GetActiveUsersAsync();

        // Search users by query
        var searchResults = await _userRepository.SearchAsync("john");

        // Check if user exists
        var exists = await _userRepository.ExistsAsync(newUser.UserId);

        // Update a user
        if (existingUser != null)
        {
            existingUser.FullName = "John Doe Updated";
            existingUser.Roles = new List<string> { "user", "premium", "vip" };
            await _userRepository.UpdateAsync(existingUser);
        }

        // Delete a user
        await _userRepository.DeleteAsync(existingUser!);

        // Delete by ID
        await _userRepository.DeleteByIdAsync(newUser.UserId);
    }
}
```

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a client management service or controller
public class ClientManagementService
{
    private readonly IClientRepository _clientRepository;

    public ClientManagementService(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task ManageClientsExample()
    {
        // Create a new OAuth client
        var newClient = new Client
        {
            ClientId = "web-client-123",
            ClientName = "My Web Application",
            ClientSecret = "s3cr3tP@ssw0rd",
            IsConfidential = true,
            GrantTypes = new List<string> { "authorization_code", "refresh_token" },
            RedirectUris = new List<string> { "https://client.example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "email", "api:read" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _clientRepository.CreateAsync(newClient);

        // Get a client by ID
        var existingClient = await _clientRepository.GetByIdAsync("web-client-123");

        // Get all clients
        var allClients = await _clientRepository.GetAllAsync();

        // Get active clients
        var activeClients = await _clientRepository.GetActiveClientsAsync();

        // Get client by client ID
        var clientByClientId = await _clientRepository.GetByClientIdAsync("web-client-123");

        // Check if client exists
        var exists = await _clientRepository.ExistsAsync("web-client-123");

        // Search clients by query
        var searchResults = await _clientRepository.SearchAsync("web");

        // Update a client
        if (existingClient != null)
        {
            existingClient.ClientName = "Updated Web Application";
            existingClient.AllowedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" };
            await _clientRepository.UpdateAsync(existingClient);
        }

        // Delete a client
        await _clientRepository.DeleteAsync(existingClient!);

        // Delete by ID
        await _clientRepository.DeleteByIdAsync("web-client-123");
    }
}
```

## IUserSessionRepository

The `IUserSessionRepository` interface provides data access operations for managing user session entities in the authorization server. It extends the base `IRepository<UserSession, string>` interface and adds session-specific operations for retrieving sessions by user ID, managing active sessions, revoking sessions, and cleaning up expired sessions. This repository serves as the data access layer for user session management operations.

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;

// Example usage in a session management service or controller
public class SessionManagementService
{
    private readonly IUserSessionRepository _sessionRepository;

    public SessionManagementService(IUserSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task ManageUserSessionsExample()
    {
        // Create a new user session
        var newSession = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = "user-123",
            ClientId = "web-client",
            SessionToken = "session-token-abc123",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            LastActivityAt = DateTime.UtcNow,
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            GrantedScopes = "openid profile email api:read",
            IsRevoked = false,
            RevokedAt = null,
            RevokedReason = null
        };
        await _sessionRepository.CreateAsync(newSession);

        // Get a session by ID
        var existingSession = await _sessionRepository.GetByIdAsync(newSession.SessionId);

        // Get all sessions
        var allSessions = await _sessionRepository.GetAllAsync();

        // Get all sessions for a specific user
        var userSessions = await _sessionRepository.GetByUserIdAsync("user-123");

        // Get all active sessions for a specific user
        var activeUserSessions = await _sessionRepository.GetActiveByUserIdAsync("user-123");

        // Get all active sessions across all users
        var allActiveSessions = await _sessionRepository.GetAllActiveAsync();

        // Check if session exists
        var exists = await _sessionRepository.ExistsAsync(newSession.SessionId);

        // Update a session
        if (existingSession != null)
        {
            existingSession.LastActivityAt = DateTime.UtcNow;
            existingSession.ExpiresAt = DateTime.UtcNow.AddHours(8);
            await _sessionRepository.UpdateAsync(existingSession);
        }

        // Revoke a session
        await _sessionRepository.DeleteAsync(existingSession!);

        // Revoke all sessions for a user (e.g., after password change)
        var revokedCount = await _sessionRepository.RevokeAllUserSessionsAsync("user-123", "Password changed - all sessions invalidated");

        // Delete by ID
        await _sessionRepository.DeleteByIdAsync(newSession.SessionId);

        // Delete expired sessions
        var deletedCount = await _sessionRepository.DeleteExpiredAsync();
    }
}
```

## SessionManagementController

The `SessionManagementController` provides a REST API for managing user sessions in the authorization server. It exposes endpoints for listing active sessions, viewing session statistics, retrieving user-specific sessions, and revoking sessions either individually or for all sessions belonging to a user. This controller is essential for administrative operations, security monitoring, and user session management.


## UserManagementController

The `UserManagementController` provides a REST API for administrative user account management. It exposes CRUD operations, user search, role assignment/removal, and account lock/unlock functionality. All routes are under `/api/users`.

```csharp
using DotnetAuthServer.Controllers;
using Microsoft.AspNetCore.Mvc;

// Example usage in an admin controller or background service
public class UserAdminController
{
    private readonly UserManagementController _userController;

    public UserAdminController(UserManagementController userController)
    {
        _userController = userController;
    }

    public async Task<IActionResult> ListAllUsers()
    {
        // Returns all registered users
        var result = await _userController.GetUsersAsync(null, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> GetUserDetails(string userId)
    {
        // Get a specific user by ID
        var result = await _userController.GetUserAsync(userId, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> SearchUsers(string query)
    {
        // Search users by username, email, or full name
        var result = await _userController.GetUsersAsync(query, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> CreateNewUser()
    {
        // Create a new user account
        var createRequest = new CreateUserRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "SecurePassword123!",
            FullName = "New User",
            Roles = new List<string> { "user" }
        };

        var result = await _userController.CreateUserAsync(createRequest, CancellationToken.None);
        return CreatedAtAction(nameof(GetUserDetails), new { userId = "newuser" }, result);
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

        var result = await _userController.UpdateUserAsync(userId, updateRequest, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> AssignRoleToUser(string userId, string role)
    {
        // Assign a role to a user
        var assignRequest = new AssignRoleRequest { Role = role };
        var result = await _userController.AssignRoleAsync(userId, assignRequest, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> RemoveRoleFromUser(string userId, string role)
    {
        // Remove a role from a user
        var result = await _userController.RemoveRoleAsync(userId, role, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> LockUserAccount(string userId, int hours = 24)
    {
        // Lock a user account for security reasons
        var result = await _userController.LockUserAsync(userId, hours * 60, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> UnlockUserAccount(string userId)
    {
        // Unlock a user account
        var result = await _userController.UnlockUserAsync(userId, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> DeleteUser(string userId)
    {
        // Permanently delete a user account and revoke all tokens
        var result = await _userController.DeleteUserAsync(userId, CancellationToken.None);
        return NoContent();
    }
}
```

## MfaController

The `MfaController` provides a REST API for managing Time-based One-Time Password (TOTP) multi-factor authentication in the authorization server. It handles enrollment initiation with secret generation and QR code provisioning URIs, setup confirmation via code verification, ongoing code verification with configurable time-step windows, backup code redemption, status queries, and MFA disablement. All routes are nested under `/api/users/{userId}/mfa`.

```csharp
using DotnetAuthServer.Controllers;
using Microsoft.AspNetCore.Mvc;

// Example usage in a controller or service
public class UserMfaManagementService
{
    private readonly MfaController _mfaController;

    public UserMfaManagementService(MfaController mfaController)
    {
        _mfaController = mfaController;
    }

    public async Task<IActionResult> CheckMfaStatus(string userId)
    {
        // Get current MFA status for a user
        var result = await _mfaController.GetStatusAsync(userId, CancellationToken.None);
        return Ok(result);
    }

    public async Task<IActionResult> EnableMfaForUser(string userId, string username)
    {
        // Initiate MFA enrollment for a user
        var setupResult = await _mfaController.SetupAsync(userId, CancellationToken.None);
        
        // The response contains:
        // - SecretKey: The TOTP secret key to share with the user
        // - ProvisioningUri: A URI for QR code generation
        // - BackupCodes: 8-character backup codes for offline recovery
        
        return Ok(setupResult);
    }

    public async Task<IActionResult> ConfirmMfaSetup(string userId, string totpCode)
    {
        // Confirm MFA setup with the user's TOTP code
        var confirmRequest = new MfaVerifyRequest { Code = totpCode };
        var confirmResult = await _mfaController.ConfirmSetupAsync(
            userId, 
            confirmRequest, 
            CancellationToken.None
        );
        
        return Ok(confirmResult);
    }

    public async Task<IActionResult> VerifyMfaCode(string userId, string code)
    {
        // Verify a TOTP or backup code during login
        var verifyRequest = new MfaVerifyRequest { Code = code };
        var verifyResult = await _mfaController.VerifyAsync(
            userId, 
            verifyRequest, 
            CancellationToken.None
        );
        
        return verifyResult;
    }

    public async Task<IActionResult> DisableMfa(string userId)
    {
        // Disable MFA for a user
        var disableResult = await _mfaController.DisableMfaAsync(userId, CancellationToken.None);
        return Ok(disableResult);
    }
}
```

```csharp
using DotnetAuthServer.Controllers;
using Microsoft.AspNetCore.Mvc;

// Example usage in an admin controller or background service
public class SessionAdminController
{
private readonly SessionManagementController _sessionController;

public SessionAdminController(SessionManagementController sessionController)
{
_sessionController = sessionController;
}

public async Task<IActionResult> ListAllActiveSessions()
{
// Returns all currently active sessions across all users
var result = await _sessionController.GetAllActiveSessionsAsync(CancellationToken.None);
return Ok(result);
}

public async Task<IActionResult> GetSessionStatistics()
{
// Returns aggregate session statistics
var result = await _sessionController.GetStatsAsync(CancellationToken.None);
return Ok(result);
}

public async Task<IActionResult> GetUserSessionHistory(string userId)
{
// Returns all active sessions for a specific user
var result = await _sessionController.GetUserSessionsAsync(userId, CancellationToken.None);
return Ok(result);
}

public async Task<IActionResult> RevokeSingleSession(string sessionId, string? reason = null)
{
// Revokes a single session by its ID
var result = await _sessionController.RevokeSessionAsync(sessionId, reason, CancellationToken.None);
return Ok(result);
}

public async Task<IActionResult> RevokeAllUserSessions(string userId, string? reason = null)
{
// Revokes all active sessions for a specific user
var result = await _sessionController.RevokeAllUserSessionsAsync(userId, reason, CancellationToken.None);
return Ok(result);
}

public async Task<IActionResult> CleanupExpiredSessions()
{
// Removes all expired sessions from storage
var result = await _sessionController.CleanupExpiredAsync(CancellationToken.None);
return Ok(result);
}
}
```

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


## JwtTokenFormatter

The `JwtTokenFormatter` class provides utilities for formatting, inspecting, and serializing JWT tokens in the authorization server. It handles JWT token construction with configurable headers and payloads, supports token inspection for debugging and validation, and provides formatting options for logging and display purposes. The formatter works with standard JWT claims and supports custom claims through a dictionary interface.

```csharp
using DotnetAuthServer.Formatters;
using System.Security.Claims;

// Create a JWT token formatter with default settings
var formatter = new JwtTokenFormatter();

// Create a token header with standard JWT properties
var header = new TokenHeader
{
    Alg = "HS256",
    Typ = "JWT",
    Kid = "key-123"
};

// Create a token payload with standard JWT claims
var payload = new TokenPayload
{
    Subject = "user-123",
    Issuer = "https://auth.example.com",
    Audience = "https://api.example.com",
    IssuedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddHours(1),
    NotBefore = DateTime.UtcNow,
    Claims = new Dictionary<string, List<string>>
    {
        { "scope", new List<string> { "openid", "profile", "email" } },
        { "roles", new List<string> { "user", "premium" } },
        { "department", new List<string> { "engineering" } }
    }
};

// Format the token
var rawToken = formatter.Format(header, payload);
Console.WriteLine($"Generated JWT: {rawToken}");

// Inspect the token (useful for debugging)
var inspection = formatter.InspectToken(rawToken);
if (inspection != null)
{
    Console.WriteLine($"Token Header Alg: {inspection.Header.Alg}");
    Console.WriteLine($"Token Payload Subject: {inspection.Payload.Subject}");
    Console.WriteLine($"Token Payload Issuer: {inspection.Payload.Issuer}");
    Console.WriteLine($"Token Payload ExpiresAt: {inspection.Payload.ExpiresAt}");
    Console.WriteLine($"Token Claims Count: {inspection.Payload.Claims.Count}");
}

// Format token for logging (redacts sensitive parts)
var logFormatted = formatter.FormatForLogging(rawToken);
Console.WriteLine($"Log-safe token: {logFormatted}");

// Access individual properties
Console.WriteLine($"Token Raw: {formatter.Raw}");
Console.WriteLine($"Token Alg: {formatter.Alg}");
Console.WriteLine($"Token Typ: {formatter.Typ}");
Console.WriteLine($"Token Kid: {formatter.Kid}");
Console.WriteLine($"Token Subject: {formatter.Subject}");
Console.WriteLine($"Token Issuer: {formatter.Issuer}");
Console.WriteLine($"Token Audience: {formatter.Audience}");
Console.WriteLine($"Token IssuedAt: {formatter.IssuedAt}");
Console.WriteLine($"Token ExpiresAt: {formatter.ExpiresAt}");
Console.WriteLine($"Token NotBefore: {formatter.NotBefore}");
```

## UserService

The `UserService` handles user authentication and self-service management operations. It provides methods for authenticating users, creating new accounts, updating profiles, changing passwords, and managing user roles. This service is designed for end-user operations and integrates with the authorization server's authentication flows.

```csharp

## JwtTokenFormatter

The `JwtTokenFormatter` class provides utilities for formatting, inspecting, and serializing JWT tokens in the authorization server. It handles JWT token construction with configurable headers and payloads, supports token inspection for debugging and validation, and provides formatting options for logging and display purposes. The formatter works with standard JWT claims and supports custom claims through a dictionary interface.

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

## OpaOptions

The `OpaOptions` class configures the Open Policy Agent (OPA) integration for external policy evaluation. When enabled, policy decisions are delegated to an OPA REST API instead of using the built-in evaluator. This allows teams to manage and version policies externally using Rego.

```csharp
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup in Program.cs
builder.Services.Configure<OpaOptions>(builder.Configuration.GetSection("Opa"));

// Example configuration in appsettings.json
{
  "Opa": {
    "Enabled": true,
    "BaseUrl": "http://opa:8181",
    "PolicyPath": "authz",
    "TimeoutSeconds": 5,
    "FailClosedOnError": false
  }
}

// Usage in services
public class PolicyService
{
    private readonly OpaOptions _opaOptions;

    public PolicyService(IOptions<OpaOptions> opaOptions)
    {
        _opaOptions = opaOptions.Value;
    }

    public bool IsOpaEnabled => _opaOptions.Enabled;
    public string BaseUrl => _opaOptions.BaseUrl;
    public string PolicyPath => _opaOptions.PolicyPath;
    public int TimeoutSeconds => _opaOptions.TimeoutSeconds;
    public bool FailClosedOnError => _opaOptions.FailClosedOnError;
}
```

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
