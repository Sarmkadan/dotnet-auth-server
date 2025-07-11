# UserSessionServiceExtensions

The `UserSessionServiceExtensions` class provides a collection of static extension methods that streamline operations on user session data within the `dotnet-auth-server` infrastructure. These methods abstract the underlying data access logic, enabling efficient retrieval, monitoring, and revocation of user sessions, thereby simplifying administrative tasks and security management related to user authentication states.

## API

### GetActiveSessionsDictionaryAsync
Retrieves a dictionary of all currently active user sessions, keyed by their unique identifier.
- **Returns:** `Task<Dictionary<string, UserSession>>`
- **Throws:** May throw an exception if the underlying session store is inaccessible.

### GetAllSessionsDictionaryAsync
Retrieves a dictionary containing all user sessions managed by the system, regardless of their active status, keyed by unique identifier.
- **Returns:** `Task<Dictionary<string, UserSession>>`

### HasActiveSessionsAsync
Determines whether there are any active sessions currently present in the system.
- **Returns:** `Task<bool>`

### GetTotalActiveSessionsCountAsync
Returns the total count of currently active sessions.
- **Returns:** `Task<int>`

### GetSessionsByClientIdAsync
Retrieves a collection of all sessions associated with a specific client ID.
- **Parameters:** `string clientId`
- **Returns:** `Task<IEnumerable<UserSession>>`

### GetActiveSessionsByClientIdAsync
Retrieves a collection of currently active sessions associated with a specific client ID.
- **Parameters:** `string clientId`
- **Returns:** `Task<IEnumerable<UserSession>>`

### RevokeAllSessionsForUsersAsync
Revokes all sessions for users according to internal criteria.
- **Returns:** `Task<int>` - The total number of sessions successfully revoked.

### GetExpiringSessionsAsync
Retrieves a collection of sessions that are currently approaching their expiration threshold.
- **Returns:** `Task<IEnumerable<UserSession>>`

## Usage

```csharp
// Example 1: Checking for active user sessions and reporting count
var hasSessions = await UserSessionServiceExtensions.HasActiveSessionsAsync();
if (hasSessions)
{
    var activeCount = await UserSessionServiceExtensions.GetTotalActiveSessionsCountAsync();
    Console.WriteLine($"There are currently {activeCount} active user sessions.");
}
```

```csharp
// Example 2: Managing sessions for a specific client
string clientId = "mobile-app-client";
var activeSessions = await UserSessionServiceExtensions.GetActiveSessionsByClientIdAsync(clientId);

if (activeSessions.Any())
{
    // Revoke all sessions; Note: RevokeAllSessionsForUsersAsync 
    // handles bulk revocation for eligible sessions.
    var revokedCount = await UserSessionServiceExtensions.RevokeAllSessionsForUsersAsync();
    Console.WriteLine($"Revoked {revokedCount} sessions.");
}
```

## Notes

- **Thread Safety:** These methods are designed for asynchronous execution. Because they interface with shared session state, the consistency of the results depends entirely on the implementation of the underlying session store (e.g., Redis, SQL Server).
- **Error Handling:** All methods return `Task` objects. If the underlying data store experiences connectivity, query, or serialization issues, these methods may throw exceptions corresponding to the failure mode of the store (e.g., `TimeoutException`, `HttpRequestException`).
- **Performance:** Methods that retrieve entire dictionaries (`GetActiveSessionsDictionaryAsync`, `GetAllSessionsDictionaryAsync`) should be used judiciously in high-traffic environments, as they may incur significant performance costs and memory usage depending on the volume of active sessions.
