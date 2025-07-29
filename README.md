// existing content ...

## ErrorHandlingMiddlewareExtensions

The `ErrorHandlingMiddlewareExtensions` class provides a set of extension methods for handling errors in a .NET Core application. It allows you to convert exceptions to error responses, serialize errors to JSON, and check if an error exists.

### Usage Example

```csharp
// In your middleware or controller
var errorResponse = new ErrorResponse
{
    Error = "invalid_request",
    ErrorDescription = "Invalid request parameters",
    ErrorUri = "/docs/errors/invalid-request"
};

// Use extension methods
var json = ErrorHandlingMiddlewareExtensions.SerializeErrorToJson(errorResponse);
Console.WriteLine(json);

// Check if error exists
if (ErrorHandlingMiddlewareExtensions.HasError(errorResponse))
{
    Console.WriteLine("Error exists");
}

// Clear error
ErrorHandlingMiddlewareExtensions.ClearError(errorResponse);
Console.WriteLine(errorResponse.Error); // null

// Set error from exception
try
{
    // Code that might throw
}
catch (Exception ex)
{
    ErrorHandlingMiddlewareExtensions.SetErrorFromException(errorResponse, ex);
}
```