// existing content ...

## AuthServerOptionsExtensions

The `AuthServerOptionsExtensions` class provides a set of extension methods for validating and retrieving configuration settings from `AuthServerOptions`. It allows you to check if certain features are supported, get lifetimes of access tokens, refresh tokens, and authorization codes, as well as other configuration settings.

### Usage Example

```csharp
// Get access token lifetime
var accessTokenLifetime = AuthServerOptionsExtensions.GetAccessTokenLifetime(options);
Console.WriteLine($"Access token lifetime: {accessTokenLifetime.TotalSeconds} seconds");

// Check if PKCE is required
var isPkceRequired = AuthServerOptionsExtensions.IsPkceRequired(options);
Console.WriteLine($"PKCE is required: {isPkceRequired}");

// Get failed login attempt threshold
var failedLoginAttemptThreshold = AuthServerOptionsExtensions.GetFailedLoginAttemptThreshold(options);
Console.WriteLine($"Failed login attempt threshold: {failedLoginAttemptThreshold}");
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