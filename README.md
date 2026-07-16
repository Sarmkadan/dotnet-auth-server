## UserSession

The `UserSession` entity represents an active authentication session for a user, associated with a specific OAuth2 grant. It tracks vital session metadata including the originating client, user, granted scopes, and session lifecycle events like expiration, last activity, and revocation status.

### Usage Example

```csharp
using DotnetAuthServer.Domain.Entities;

// Create a new user session
var session = new UserSession
{
    UserId = "user-123",
    ClientId = "web-client-abc",
    GrantedScopes = "openid profile email",
    ExpiresAt = DateTime.UtcNow.AddHours(1),
    IpAddress = "192.168.1.100",
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
};

// Record user activity
session.Touch(); // Updates LastActivityAt

// Check session validity
if (session.IsActive())
{
    Console.WriteLine($"Session {session.SessionId} is active for user {session.UserId}.");
}

// Revoke the session if necessary
session.Revoke("User logged out explicitly");

if (session.IsRevoked)
{
    Console.WriteLine($"Session revoked: {session.RevocationReason}");
}
```

## DeviceFlowHandler


The `DeviceFlowHandler` class is responsible for managing device flow authorizations, enabling devices with limited input capabilities to obtain access tokens. It provides methods for initiating, approving, denying, and polling device flow sessions.

### Usage Example

```csharp
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Domain.Models;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<DeviceFlowHandler>();
var deviceFlowHandler = new DeviceFlowHandler(logger);

// Initiate a device flow
var initiation = deviceFlowHandler.InitiateFlow(
    "client123",
    "openid profile");

// Display the device code and user code to the user
Console.WriteLine($"Device code: {initiation.DeviceCode}");
Console.WriteLine($"User code: {initiation.UserCode}");
Console.WriteLine($"Verification URI: {initiation.VerificationUri}");

// Approve the device flow after user verification
deviceFlowHandler.ApproveDeviceFlow(initiation.UserCode, "user123");

// Poll the status of the device flow
var pollResult = deviceFlowHandler.PollDeviceFlow(initiation.DeviceCode);
if (pollResult.Status == DeviceFlowStatus.Approved)
{
    Console.WriteLine("Device flow approved.");
}
else
{
    Console.WriteLine("Device flow denied or expired.");
}
```

This example demonstrates how to use the `DeviceFlowHandler` to initiate, approve, and poll a device flow authorization.

## TokenIntrospectionHandler

The `TokenIntrospectionHandler` class is responsible for token introspection, allowing authenticated clients to query information about tokens without needing to parse JWTs themselves. It provides a method for introspecting a token and returning its active status and claims.

### Usage Example

```csharp
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Domain.Models;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<TokenIntrospectionHandler>();
var tokenIntrospectionHandler = new TokenIntrospectionHandler(logger);

// Introspect a token
var token = "your_token_here";
var introspectionResponse = tokenIntrospectionHandler.IntrospectToken(token);

// Check the token's active status
if (introspectionResponse.Active)
{
    Console.WriteLine("Token is active.");
}
else
{
    Console.WriteLine("Token is inactive.");
}

// Get the token's scope
Console.WriteLine($"Token scope: {introspectionResponse.Scope}");

// Get the token's client ID
Console.WriteLine($"Token client ID: {introspectionResponse.ClientId}");

// Get the token's username
Console.WriteLine($"Token username: {introspectionResponse.Username}");

// Get the token's type
Console.WriteLine($"Token type: {introspectionResponse.TokenType}");

// Get the token's expiration time
Console.WriteLine($"Token expiration time: {introspectionResponse.Exp}");

// Get the token's issuance time
Console.WriteLine($"Token issuance time: {introspectionResponse.Iat}");

// Get the token's subject
Console.WriteLine($"Token subject: {introspectionResponse.Sub}");
```

This example demonstrates how to use the `TokenIntrospectionHandler` to introspect a token and retrieve its active status and claims.

## UserinfoHandler

The `UserinfoHandler` class is responsible for handling OpenID Connect userinfo requests. It retrieves user information based on claims from an access token and returns only the claims that are allowed by the token's scope. The handler supports standard OpenID Connect claims including profile information, email, address, and phone number.


### Usage Example

```csharp
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Data.Repositories;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

// Setup dependencies
var logger = new Logger<UserinfoHandler>();
var userRepository = new UserRepository(); // Use your actual repository implementation
var userinfoHandler = new UserinfoHandler(userRepository, logger);

// Create a claims principal with required claims (typically from access token)
var claims = new List<Claim>
{
    new Claim("sub", "user123"),
    new Claim("scope", "openid profile email")
};
var identity = new ClaimsIdentity(claims, "Bearer");
var principal = new ClaimsPrincipal(identity);

// Get user information
var userinfo = await userinfoHandler.GetUserinfoAsync(principal);

if (userinfo != null)
{
    Console.WriteLine($"Subject: {userinfo.Sub}");
    
    if (!string.IsNullOrEmpty(userinfo.Name))
        Console.WriteLine($"Name: {userinfo.Name}");
    
    if (!string.IsNullOrEmpty(userinfo.GivenName))
        Console.WriteLine($"Given Name: {userinfo.GivenName}");
    
    if (!string.IsNullOrEmpty(userinfo.FamilyName))
        Console.WriteLine($"Family Name: {userinfo.FamilyName}");
    
    if (userinfo.UpdatedAt.HasValue)
        Console.WriteLine($"Updated At: {userinfo.UpdatedAt}");
    
    if (!string.IsNullOrEmpty(userinfo.Email))
    {
        Console.WriteLine($"Email: {userinfo.Email}");
        if (userinfo.EmailVerified.HasValue)
            Console.WriteLine($"Email Verified: {userinfo.EmailVerified}");
    }
    
    if (userinfo.Address != null)
    {
        Console.WriteLine($"Address:");
        if (!string.IsNullOrEmpty(userinfo.Address.StreetAddress))
            Console.WriteLine($"  Street: {userinfo.Address.StreetAddress}");
        if (!string.IsNullOrEmpty(userinfo.Address.Locality))
            Console.WriteLine($"  City: {userinfo.Address.Locality}");
        if (!string.IsNullOrEmpty(userinfo.Address.Region))
            Console.WriteLine($"  Region: {userinfo.Address.Region}");
        if (!string.IsNullOrEmpty(userinfo.Address.PostalCode))
            Console.WriteLine($"  Postal Code: {userinfo.Address.PostalCode}");
        if (!string.IsNullOrEmpty(userinfo.Address.Country))
            Console.WriteLine($"  Country: {userinfo.Address.Country}");
    }
}
```

This example demonstrates how to use the `UserinfoHandler` to retrieve user information based on an access token's claims and scopes.


## JwksHandler

The `JwksHandler` class is responsible for managing JSON Web Key Sets (JWKS) and provides methods for retrieving the current JWKS and validating key IDs. It can be used to obtain the public keys used to validate JWTs issued by this authorization server.

### Usage Example

```csharp
using DotnetAuthServer.Handlers;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<JwksHandler>();
var jwksHandler = new JwksHandler(new AuthServerOptions(), new CacheService(), logger);

// Get the current JWKS
var jwksResponse = await jwksHandler.GetJwksAsync();

// Validate a key ID
var isValidKeyId = await jwksHandler.IsValidKeyIdAsync("key-id");
Console.WriteLine($"Is key ID valid: {isValidKeyId}");
```

This example demonstrates how to use the `JwksHandler` to retrieve the current JWKS and validate a key ID.

## OpaClient

The `OpaClient` class is an HTTP client wrapper for the Open Policy Agent (OPA) REST API. It sends policy queries to OPA and returns allow/deny decisions.

### Usage Example

```csharp
using DotnetAuthServer.Integration;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<OpaClient>();
var httpClient = new HttpClient();
var opaOptions = new OpaOptions { BaseUrl = "https://example.com/opa", PolicyPath = "policy" };
var opaClient = new OpaClient(httpClient, opaOptions, logger);

// Create a claims principal with required claims
var claims = new List<Claim>
{
    new Claim("sub", "user123"),
    new Claim(ClaimTypes.Role, "admin"),
    new Claim("scope", "openid profile")
};
var identity = new ClaimsIdentity(claims, "Bearer");
var principal = new ClaimsPrincipal(identity);

// Evaluate a policy
var policyName = "example-policy";
var result = await opaClient.EvaluatePolicyAsync(policyName, principal);

if (result.HasValue)
{
    Console.WriteLine($"Policy {policyName} evaluation result: {result}");
}
else
{
    Console.WriteLine($"Unable to evaluate policy {policyName}");
}
```

## DateTimeExtensions

The `DateTimeExtensions` class provides essential extension methods for handling `DateTime` objects, particularly within the context of JWT and OAuth2 token management. It simplifies Unix timestamp conversions, expiration checks, and RFC 3339 string formatting, ensuring consistent and correct temporal operations across the authorization server.

### Usage Example

```csharp
using DotnetAuthServer.Extensions;

// Get current time
var now = DateTime.UtcNow;

// Convert to Unix timestamp
long timestamp = now.ToUnixTimestamp();

// Convert from Unix timestamp
DateTime dateTime = DateTimeExtensions.FromUnixTimestamp(timestamp);

// Add a lifetime (e.g., 3600 seconds)
DateTime expiresAt = now.AddLifetime(3600);

// Check expiration
bool isExpired = expiresAt.IsExpired();
bool isValid = expiresAt.IsValid();

// Get remaining time
long remaining = expiresAt.RemainingSeconds();

// Format as RFC 3339 string for OIDC/OAuth responses
string rfc3339String = now.ToRfc3339String();
```

## WebhookClient

The `WebhookClient` class provides functionality for sending webhook events to external services. It supports configurable retry logic, timeout handling, and provides detailed result information including success status, error details, and event metadata.

### Usage Example

## HttpRequestExtensions

The `HttpRequestExtensions` class provides extension methods for extracting OAuth2/OIDC parameters from HTTP requests. It handles both query string and form body parameters (valid in OAuth2), supports client credential extraction from Basic Auth headers or form parameters, retrieves client IP addresses (accounting for proxies), checks for secure transport, and extracts bearer tokens from Authorization headers.

### Usage Example

```csharp
using DotnetAuthServer.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

// Example 1: Extract OAuth parameter from query string or form body
var request = new DefaultHttpContext().Request;
request.Query = new QueryCollection(new Dictionary<string, StringValues> { ["client_id"] = "my-client-id" });
var clientId = request.GetOAuthParameter("client_id"); // Returns "my-client-id"

// Example 2: Extract client credentials from Basic Auth header
request = new DefaultHttpContext().Request;
request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("client123:secret456"));
var (extractedClientId, extractedClientSecret) = request.ExtractClientCredentials();
// extractedClientId = "client123", extractedClientSecret = "secret456"

// Example 3: Extract client credentials from form parameters
request = new DefaultHttpContext().Request;
request.Form = new FormCollection(new Dictionary<string, StringValues> 
{
    ["client_id"] = "form-client-id",
    ["client_secret"] = "form-client-secret"
});
var (formClientId, formClientSecret) = request.ExtractClientCredentials();
// formClientId = "form-client-id", formClientSecret = "form-client-secret"

// Example 4: Get client IP address (handles X-Forwarded-For)
request = new DefaultHttpContext().Request;
request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1";
var clientIp = request.GetClientIpAddress(); // Returns "192.168.1.100"

// Example 5: Check if request uses HTTPS
request = new DefaultHttpContext().Request;
request.IsHttps = true;
var isSecure = request.IsSecureTransport(); // Returns true

// Example 6: Extract bearer token from Authorization header
request = new DefaultHttpContext().Request;
request.Headers.Authorization = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
var bearerToken = request.GetBearerToken();
// bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

## StringExtensions

The `StringExtensions` class provides a set of utility extension methods for string manipulation, focusing on OAuth2/OIDC requirements such as scope parsing, URI validation, URL safety checks, and sensitive data masking. These methods help maintain code consistency and security when handling string-based identifiers and configuration values.

### Usage Example

```csharp
using DotnetAuthServer.Extensions;

// Example 1: Parse a space-delimited scope string
string scopes = "openid profile email email";
var parsedScopes = scopes.ParseScopes(); 
// Returns ["openid", "profile", "email"] (distinct and non-empty)

// Example 2: Join scopes into a space-delimited string
var scopeList = new List<string> { "openid", "profile" };
var joinedScopes = scopeList.JoinScopes();
// Returns "openid profile"

// Example 3: Validate an absolute URI
string uri = "https://example.com/callback";
bool isValid = uri.IsValidAbsoluteUri(); // Returns true

// Example 4: Compare two URIs safely
string uri1 = "https://example.com/";
string uri2 = "https://example.com";
bool isEqual = uri1.UriEquals(uri2); // Returns true (normalized comparison)

// Example 5: Check if a string is URL safe
string safeValue = "client-id_123";
bool isSafe = safeValue.IsUrlSafe(); // Returns true

// Example 6: Safely truncate a string
string longString = "some_very_long_identifier_value";
string truncated = longString.SafeTruncate(10);
// Returns "some_very_"

// Example 7: Mask sensitive data
string sensitiveData = "supersecretpassword123";
string masked = sensitiveData.MaskSensitive();
// Returns "sup***123"
```

## ClaimsPrincipalExtensions

The `ClaimsPrincipalExtensions` class provides extension methods for extracting and validating standard OAuth2/OIDC claims from a `ClaimsPrincipal`. These methods safely handle missing or malformed claims and provide type-safe access to common claims like subject, email, roles, scopes, and token metadata (issuance time, expiration, audience, etc.).



### Usage Example

```csharp
using DotnetAuthServer.Extensions;
using System.Security.Claims;

// Create a claims principal with typical OAuth2/OIDC claims
var claims = new List<Claim>
{
    new Claim("sub", "user12345"),
    new Claim("email", "user@example.com"),
    new Claim("email_verified", "true"),
    new Claim("scope", "openid profile email api:read api:write"),
    new Claim("aud", "client-app-123"),
    new Claim("roles", "admin"),
    new Claim("roles", "user"),
    new Claim("iat", "1715683200"),
    new Claim("exp", "1715769600")
};
var identity = new ClaimsIdentity(claims, "Bearer");
var principal = new ClaimsPrincipal(identity);

// Extract claims safely
string? subject = principal.GetSubject();
// Returns "user12345"

string? email = principal.GetEmail();
// Returns "user@example.com"

bool isEmailVerified = principal.IsEmailVerified();
// Returns true

IEnumerable<string> roles = principal.GetRoles();
// Returns ["admin", "user"]

bool hasAdminRole = principal.HasRole("admin");
// Returns true

bool hasScope = principal.HasScope("api:read");
// Returns true

string? audience = principal.GetAudience();
// Returns "client-app-123"

IEnumerable<string> scopes = principal.GetScopes();
// Returns ["openid", "profile", "email", "api:read", "api:write"]

long? issuedAt = principal.GetIssuedAt();
// Returns 1715683200

long? expiration = principal.GetExpiration();
// Returns 1715769600
```



```csharp
using DotnetAuthServer.Integration;
using System.Text.Json;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<WebhookClient>();
var httpClient = new HttpClient();
var webhookClient = new WebhookClient(
    httpClient,
    new WebhookOptions
    {
        Url = "https://api.example.com/webhooks/events",
        Enabled = true,
        MaxRetries = 3,
        InitialRetryDelayMs = 1000,
        MaxRetryDelayMs = 10000,
        Timeout = TimeSpan.FromSeconds(30)
    },
    logger
);

// Create event data
var eventData = new
{
    OrderId = "order-12345",
    CustomerId = "customer-67890",
    TotalAmount = 99.99,
    Items = new[]
    {
        new { ProductId = "prod-1", Quantity = 2, Price = 29.99 },
        new { ProductId = "prod-2", Quantity = 1, Price = 40.01 }
    }
};

// Send webhook event
var result = await webhookClient.SendEventWebhookAsync(
    "order.created",
    eventData,
    requestId: "req-98765"
);

// Handle the result
if (result.Success)
{
    Console.WriteLine($"Webhook sent successfully!");
    Console.WriteLine($"Event ID: {result.EventId}");
    Console.WriteLine($"Event Type: {result.EventType}");
    Console.WriteLine($"Occurred At: {result.OccurredAt}");
    Console.WriteLine($"Request ID: {result.RequestId}");
    
    // Access the response data
    var jsonData = result.Data;
    if (jsonData.TryGetProperty("status", out var status))
    {
        Console.WriteLine($"Remote service status: {status}");
    }
}
else
{
    Console.WriteLine($"Failed to send webhook: {result.Error}");
}
```

## Consent

The `Consent` entity represents user consent for OAuth2/OIDC client scope access. It tracks consent status, granted scopes, expiration, and audit information including IP address and user agent. The class provides methods to grant, deny, revoke consent and check scope permissions.

## Client

The `Client` entity represents an OAuth2/OIDC client application registered with the authorization server. It defines client metadata such as identifiers, secrets, redirect URIs, allowed grant types, scopes, and token lifetime settings. Clients can be confidential (with secrets) or public, and support various OAuth2 flows including authorization code, implicit, client credentials, and refresh token flows.

### Usage Example

```csharp
using DotnetAuthServer.Domain.Entities;

// Create a new confidential client
var client = new Client
{
    ClientId = "web-client-123",
    ClientName = "Web Application Client",
    ClientSecretHash = "hashed-secret-value-here", // In production, use proper hashing
    IsConfidential = true,
    IsActive = true,
    Description = "Main web application client for user authentication",
    
    // Redirect URIs for authorization code flow
    RedirectUris = new List<string>
    {
        "https://example.com/auth/callback",
        "https://example.com/silent-renew"
    },
    
    // Post-logout redirect URIs
    PostLogoutRedirectUris = new List<string>
    {
        "https://example.com/",
        "https://example.com/logged-out"
    },
    
    // Allowed OAuth2 grant types
    AllowedGrantTypes = new List<string>
    {
        "authorization_code",
        "refresh_token",
        "client_credentials"
    },
    
    // Allowed scopes
    AllowedScopes = new List<string>
    {
        "openid",
        "profile",
        "email",
        "api:read",
        "api:write"
    },
    
    // CORS origins
    AllowedCorsOrigins = new List<string>
    {
        "https://example.com",
        "https://staging.example.com"
    },
    
    // Token lifetime settings (in seconds)
    AccessTokenLifetime = 3600, // 1 hour
    RefreshTokenLifetime = 2592000, // 30 days
    
    // Security settings
    RequirePkce = true,
    RequireConsent = true,
    RefreshTokenRotation = true,
    
    // Contact information
    Contacts = new List<string> { "admin@example.com", "security@example.com" },
    
    // URIs for branding and policies
    LogoUri = "https://example.com/images/logo.png",
    PolicyUri = "https://example.com/policies/privacy",
    TermsOfServiceUri = "https://example.com/terms"
};

// Validate client configuration
bool isValid = client.IsValid();

// Check if a redirect URI is registered
bool isValidRedirect = client.IsRedirectUriValid("https://example.com/auth/callback");

// Check if a grant type is allowed
bool allowsAuthorizationCode = client.IsGrantTypeAllowed("authorization_code");

// Check if a scope is allowed
bool hasEmailScope = client.IsScopeAllowed("email");
```

### Usage Example

```csharp
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Enums;

// Create a new consent record
var consent = new Consent
{
    ConsentId = Guid.NewGuid().ToString(),
    UserId = "user-123",
    ClientId = "web-client",
    GrantedScopes = "openid profile email api:read",
    IsOfflineConsent = true,
    IpAddress = "192.168.1.100",
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
};

// Grant consent for specific scopes
consent.Grant("openid profile email api:write");

// Check if consent is valid and approved
bool isValid = consent.IsValidAndApproved(); // Returns true

// Check if a specific scope is granted
bool hasProfileScope = consent.HasScopeConsent("profile"); // Returns true
bool hasAdminScope = consent.HasScopeConsent("admin"); // Returns false

// Get all granted scopes
var grantedScopes = consent.GetGrantedScopes();
foreach (var scope in grantedScopes)
{
    Console.WriteLine($"Granted scope: {scope}");
}

// Check if consent has expired
bool isExpired = consent.IsExpired(); // Returns false

// Revoke consent
consent.Revoke("User requested revocation");

// Deny consent
// consent.Deny("User declined the request");
```

## WebAuthnServiceExtensions

The `WebAuthnServiceExtensions` class provides extension methods for `IServiceCollection` to register WebAuthn/FIDO2 services in the dependency injection container. By default, it configures an in-process, thread-safe credential store suitable for development, which can be replaced with a database-backed implementation for production environments.

### Usage Example

```csharp
using DotnetAuthServer.Extensions;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

// Register WebAuthn services
var services = new ServiceCollection();
services.AddWebAuthn();
var serviceProvider = services.BuildServiceProvider();

// Retrieve the credential store (the underlying service configured by AddWebAuthn)
var credentialStore = serviceProvider.GetRequiredService<IWebAuthnCredentialStore>();

// Create and add a new credential
var newCredential = new WebAuthnCredential 
{ 
    CredentialId = "cred-1", 
    UserId = "user-1", 
    IsActive = true, 
    CreatedAt = DateTime.UtcNow 
};
await credentialStore.AddAsync(newCredential);

// Find a credential by ID
var credential = await credentialStore.FindByCredentialIdAsync("cred-1");

// Get all active credentials for a user
var userCredentials = await credentialStore.GetByUserIdAsync("user-1");

// Update an existing credential
if (credential != null)
{
    credential.IsActive = false;
    await credentialStore.UpdateAsync(credential);
}
```

