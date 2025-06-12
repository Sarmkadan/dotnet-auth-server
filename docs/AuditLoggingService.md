# AuditLoggingService

The `AuditLoggingService` is a core component within the `dotnet-auth-server` project responsible for capturing, storing, and retrieving security-relevant events. It provides a centralized mechanism to record token issuance, authentication attempts, authorization decisions, suspicious activities, and administrative actions. The service maintains an in-memory collection of log entries enriched with contextual metadata such as user identity, client information, request tracking IDs, and severity levels, facilitating real-time monitoring and post-incident analysis.

## API

### Constructors

#### `public AuditLoggingService()`
Initializes a new instance of the `AuditLoggingService` class. This constructor sets up the internal storage for audit entries and prepares the service to begin logging events immediately.

### Logging Methods

#### `public void LogTokenIssuance(string? userId, string? clientId, Dictionary<string, string> details)`
Records an event where a security token has been successfully issued to a client.
*   **Parameters**:
    *   `userId`: The identifier of the user for whom the token was issued. Can be null for client-credential flows.
    *   `clientId`: The identifier of the client application requesting the token.
    *   `details`: A dictionary containing additional context, such as token type, scope, or expiration time.
*   **Returns**: `void`
*   **Throws**: Throws `ArgumentNullException` if `details` is null.

#### `public void LogAuthentication(string? userId, string? clientId, Dictionary<string, string> details)`
Logs the result of an authentication attempt, whether successful or failed.
*   **Parameters**:
    *   `userId`: The identifier of the user attempting to authenticate.
    *   `clientId`: The identifier of the client initiating the authentication request.
    *   `details`: Contextual data such as authentication method used, IP address, or failure reasons.
*   **Returns**: `void`
*   **Throws**: Throws `ArgumentNullException` if `details` is null.

#### `public void LogAuthorizationDecision(string? userId, string? clientId, Dictionary<string, string> details)`
Captures the outcome of an authorization check, recording whether access to a specific resource was granted or denied.
*   **Parameters**:
    *   `userId`: The user subject to the authorization check.
    *   `clientId`: The client application requesting access.
    *   `details`: Information regarding the resource accessed, the policy evaluated, and the final decision.
*   **Returns**: `void`
*   **Throws**: Throws `ArgumentNullException` if `details` is null.

#### `public void LogSuspiciousActivity(string? userId, string? clientId, Dictionary<string, string> details)`
Records potentially malicious or anomalous behavior detected within the system. Entries created via this method typically default to a higher `AuditSeverity`.
*   **Parameters**:
    *   `userId`: The user associated with the suspicious activity, if identifiable.
    *   `clientId`: The client associated with the activity.
    *   `details`: Specifics about the anomaly, such as repeated failed logins, invalid signature attempts, or unusual geographic locations.
*   **Returns**: `void`
*   **Throws**: Throws `ArgumentNullException` if `details` is null.

#### `public void LogAdministrativeAction(string? userId, string? clientId, Dictionary<string, string> details)`
Logs actions performed by administrators that alter the system configuration or state.
*   **Parameters**:
    *   `userId`: The administrator performing the action.
    *   `clientId`: The management interface or tool used.
    *   `details`: Description of the change, including old and new values for modified settings.
*   **Returns**: `void`
*   **Throws**: Throws `ArgumentNullException` if `details` is null.

### Retrieval and Management

#### `public IEnumerable<AuditLogEntry> GetRecentEntries()`
Retrieves a collection of the most recently recorded audit log entries.
*   **Parameters**: None.
*   **Returns**: An `IEnumerable<AuditLogEntry>` containing the log entries, ordered by timestamp descending.
*   **Throws**: Does not throw under normal conditions; returns an empty enumerable if no entries exist.

#### `public void Clear()`
Removes all currently stored audit log entries from the service's memory.
*   **Parameters**: None.
*   **Returns**: `void`
*   **Throws**: Does not throw.

### Properties

The following properties reflect the state of the most recently processed log entry or the current context within the service implementation:

*   **`public string EventType`**: Gets the type of the last recorded event (e.g., "TokenIssuance", "Authentication").
*   **`public string? UserId`**: Gets the user ID associated with the last recorded event.
*   **`public string? ClientId`**: Gets the client ID associated with the last recorded event.
*   **`public DateTime Timestamp`**: Gets the precise time the last event was logged.
*   **`public string? RequestId`**: Gets the unique request identifier correlated with the last event, useful for distributed tracing.
*   **`public Dictionary<string, string> Details`**: Gets the key-value pair collection containing extended metadata for the last event.
*   **`public AuditSeverity Severity`**: Gets the severity level assigned to the last recorded event.

## Usage

### Example 1: Logging a Successful Authentication and Token Issuance
This example demonstrates how to record a standard user login flow, capturing both the authentication success and the subsequent token generation.

```csharp
// Initialize the service
var auditService = new AuditLoggingService();

// Context data for the login
var authDetails = new Dictionary<string, string>
{
    { "AuthMethod", "Password" },
    { "IpAddress", "192.168.1.105" },
    { "UserAgent", "Mozilla/5.0" }
};

// Log the authentication event
auditService.LogAuthentication("user_12345", "client_web_app", authDetails);

// Context data for the token
var tokenDetails = new Dictionary<string, string>
{
    { "TokenType", "Bearer" },
    { "Scope", "read:profile write:documents" },
    { "ExpiresIn", "3600" }
};

// Log the token issuance
auditService.LogTokenIssuance("user_12345", "client_web_app", tokenDetails);

// Verify the last entry
Console.WriteLine($"Last Event: {auditService.EventType} at {auditService.Timestamp}");
```

### Example 2: Detecting and Retrieving Suspicious Activity
This example illustrates logging a potential brute-force attempt and retrieving recent entries to analyze the pattern.

```csharp
var auditService = new AuditLoggingService();

// Simulate multiple failed attempts
var failureDetails = new Dictionary<string, string>
{
    { "FailureReason", "InvalidCredentials" },
    { "AttemptCount", "5" },
    { "IpAddress", "203.0.113.45" }
};

// Log as suspicious activity due to high failure count
auditService.LogSuspiciousActivity("user_99999", "unknown_client", failureDetails);

// Retrieve recent logs for analysis
var recentLogs = auditService.GetRecentEntries();

foreach (var entry in recentLogs)
{
    if (entry.Severity >= AuditSeverity.High)
    {
        Console.WriteLine($"ALERT: {entry.EventType} for User {entry.UserId}");
        Console.WriteLine($"Details: {string.Join(", ", entry.Details.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }
}
```

## Notes

*   **Thread Safety**: The internal collection managing `AuditLogEntry` objects is designed to handle concurrent writes from multiple request threads. However, the scalar properties (e.g., `UserId`, `EventType`) reflect the state of the *last* operation performed on the instance. In a highly concurrent environment, reading these properties immediately after a write operation on a shared static instance may yield race conditions where the properties do not match the specific entry just written by the current thread. It is recommended to rely on the return data from `GetRecentEntries()` for accurate state inspection in multi-threaded scenarios rather than reading the scalar properties directly.
*   **Memory Management**: The `Clear()` method should be invoked periodically or based on a rotation policy, as the service stores entries in memory. Without explicit clearing or an external persistence layer (not included in this specific class signature), long-running instances may consume increasing amounts of memory.
*   **Null Handling**: While `UserId` and `ClientId` are nullable to support various OAuth2 flows (such as client credentials or anonymous endpoints), the `details` dictionary parameter in all logging methods must not be null. Passing null to this parameter will result in an exception.
*   **Severity Defaults**: The `Severity` property is automatically populated based on the logging method used. For instance, `LogSuspiciousActivity` typically assigns a higher severity than `LogAuthentication`. Custom severity overrides are not exposed via the public method signatures listed; severity is inferred from the event type.
