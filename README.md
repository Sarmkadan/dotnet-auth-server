## RequestValidationHandler

The `RequestValidationHandler` class provides comprehensive validation for OAuth2 requests, ensuring authorization, token, and consent requests meet structural and security requirements. It validates required parameters, checks for size limits to prevent DOS attacks, and verifies compliance with OAuth2 specifications for response types, grant types, and scopes.

### Usage Example

```csharp
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Domain.Models;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<RequestValidationHandler>();
var validationHandler = new RequestValidationHandler(logger);

// Example authorization request
var request = new AuthorizationRequest
{
    ClientId = "client123",
    ResponseType = "code",
    RedirectUri = "https://client-app.com/callback",
    Scope = "openid profile",
    State = "abc123"
};

try
{
    // Validate the request
    validationHandler.ValidateAuthorizationRequest(request);

    // Additional checks for response type validity
    if (!validationHandler.IsValidResponseType(request.ResponseType))
    {
        throw new AuthServerException("invalid_request", "Invalid response type", 400);
    }

    Console.WriteLine("Authorization request is valid.");
}
catch (AuthServerException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
catch (InvalidClientException ex)
{
    Console.WriteLine($"Client error: {ex.Message}");
}
```

This example demonstrates validating an OAuth2 authorization request, including error handling for invalid inputs and response type validation. The handler ensures all required parameters are present, enforces size limits, and confirms compliance with OAuth2 standards.
