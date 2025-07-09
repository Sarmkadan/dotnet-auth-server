# ErrorHandlingMiddlewareExtensions

ErrorHandlingMiddlewareExtensions provides a set of utility methods designed to streamline error management and serialization within the `dotnet-auth-server` middleware pipeline. It simplifies the process of transforming exceptions into standardized `ErrorResponse` objects, managing error states within the current request context, and serializing error details into JSON format for consistent API responses.

## API

### `public static ErrorResponse ToErrorResponse(Exception exception)`
Converts the provided exception into a standardized `ErrorResponse` object suitable for API responses.
*   **Parameters:** `exception` - The exception to be converted.
*   **Returns:** An `ErrorResponse` instance populated with details derived from the exception.

### `public static void SetErrorFromException(HttpContext context, Exception exception)`
Extracts error information from an exception and sets the corresponding error properties on the current `HttpContext`.
*   **Parameters:** `context` - The current `HttpContext`. `exception` - The source exception.

### `public static string SerializeErrorToJson(ErrorResponse errorResponse)`
Serializes an `ErrorResponse` object into a JSON string formatted for HTTP responses.
*   **Parameters:** `errorResponse` - The object to serialize.
*   **Returns:** A JSON-formatted string representation of the error.

### `public static bool HasError(HttpContext context)`
Checks if the current `HttpContext` contains active error information.
*   **Parameters:** `context` - The `HttpContext` to inspect.
*   **Returns:** `true` if an error is present; otherwise, `false`.

### `public static void ClearError(HttpContext context)`
Removes any existing error information from the current `HttpContext`.
*   **Parameters:** `context` - The `HttpContext` to clear.

### `public string? Error`
Gets or sets the error code string associated with the current context.

### `public string? ErrorDescription`
Gets or sets the detailed description of the error.

### `public string? ErrorUri`
Gets or sets the URI pointing to documentation or additional information about the error.

## Usage

**Example 1: Handling Exceptions in Middleware**
```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (Exception ex)
    {
        ErrorHandlingMiddlewareExtensions.SetErrorFromException(context, ex);
        var errorResponse = ErrorHandlingMiddlewareExtensions.ToErrorResponse(ex);
        
        context.Response.StatusCode = 400; // Map to appropriate status code
        await context.Response.WriteAsync(ErrorHandlingMiddlewareExtensions.SerializeErrorToJson(errorResponse));
    }
}
```

**Example 2: Conditionally Clearing Error State**
```csharp
public void ProcessRequest(HttpContext context)
{
    if (ErrorHandlingMiddlewareExtensions.HasError(context))
    {
        // Log the error before clearing
        _logger.LogWarning("Clearing existing error state from request context.");
        
        ErrorHandlingMiddlewareExtensions.ClearError(context);
    }
}
```

## Notes

*   **Thread Safety:** While `HttpContext` is not inherently thread-safe for concurrent access, these extension methods are designed to operate synchronously within the request pipeline. Ensure that modifications to the error state are performed within the scope of a single request.
*   **Exception Handling:** When calling `ToErrorResponse` with an unrecognized or unhandled exception type, it may return a default error response with limited information. Always validate the input exception to ensure meaningful and secure error reporting.
*   **Serialization:** `SerializeErrorToJson` assumes that the provided `ErrorResponse` object is properly initialized. Ensure all required fields (e.g., `Error`, `ErrorDescription`) are populated before serialization to prevent generating malformed or incomplete JSON responses.
