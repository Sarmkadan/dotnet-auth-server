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
