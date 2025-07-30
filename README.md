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

## UserSessionServiceExtensions
The `UserSessionServiceExtensions` class provides a set of extension methods for managing user sessions. It allows you to retrieve active sessions, get sessions by client ID, revoke sessions, and more. 

### Usage Example
```csharp
// Get all active sessions
var activeSessions = await UserSessionServiceExtensions.GetActiveSessionsDictionaryAsync();
Console.WriteLine($"Active sessions count: {activeSessions.Count}");

// Get sessions by client ID
var clientId = "some-client-id";
var sessionsByClientId = await UserSessionServiceExtensions.GetSessionsByClientIdAsync(clientId);
Console.WriteLine($"Sessions for client {clientId}: {sessionsByClientId.Count()}");

// Revoke all sessions for users
var revokedSessionsCount = await UserSessionServiceExtensions.RevokeAllSessionsForUsersAsync();
Console.WriteLine($"Revoked sessions count: {revokedSessionsCount}");
```

## UserServiceExtensions
The `UserServiceExtensions` class provides extension methods for user management operations including role-based user creation, bulk user creation, role validation, attribute assignment, and authentication attempts.

### Usage Example
```csharp
// Create user with role
var user = await UserServiceExtensions.CreateUserWithRoleAsync("john_doe", "user", "StandardUser");

// Add attributes to user
user = await UserServiceExtensions.WithAttributesAsync(user, new { Email = "john@example.com", Age = 30 });

// Check if user has role
if (UserServiceExtensions.HasRole(user, "StandardUser"))
{
    Console.WriteLine("User has StandardUser role");
}

// Get all users with StandardUser role
var standardUsers = await UserServiceExtensions.GetUsersByRoleAsync("StandardUser");

// Attempt authentication
var (authenticatedUser, success) = await UserServiceExtensions.TryAuthenticateAsync("john_doe", "password123");
if (success)
{
    Console.WriteLine($"Authentication successful for {authenticatedUser.Username}");
}
```
