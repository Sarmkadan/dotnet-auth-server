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
```