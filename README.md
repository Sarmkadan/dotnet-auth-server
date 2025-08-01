// existing content ...

## AuthServerException

The `AuthServerException` class represents a base exception for authorization server errors. It provides properties for error code, HTTP status code, error description, error URI, and additional details. This exception is useful for handling and propagating error information in authentication and authorization flows.

### Usage Example

```csharp
try
{
    // Simulate an authentication error
    throw new AuthServerException(
        errorCode: "invalid_client",
        message: "Client credentials are invalid.",
        statusCode: 401,
        errorDescription: "Invalid client ID or client secret.",
        errorUri: "https://example.com/docs/errors/invalid_client");
}
catch (AuthServerException ex)
{
    var errorResponse = ex.ToErrorResponse();
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.WriteLine($"HTTP Status Code: {ex.StatusCode}");
    Console.WriteLine($"Error Description: {ex.ErrorDescription}");
    Console.WriteLine($"Error URI: {ex.ErrorUri}");

    // Add custom details
    ex.Details["custom_detail"] = "Custom error detail";
    var errorResponseWithDetails = ex.ToErrorResponse();

    // Process the error response
}

// Output:
// Error Code: invalid_client
// HTTP Status Code: 401
// Error Description: Invalid client ID or client secret.
// Error URI: https://example.com/docs/errors/invalid_client
```

// ... rest of content ...
