# IAuditLoggingService

`IAuditLoggingService` defines the contract for recording and retrieving security-relevant audit events in `DotnetAuthServer.Services`. Consumers can depend on this abstraction to record authentication activity, token issuance, authorization decisions, suspicious behavior, and administrative changes without depending on a particular storage implementation.

The interface is synchronous. Its retrieval methods return `AuditLogEntry` objects, which contain the event type, user and client identifiers, timestamp, request identifier, severity, and event-specific details.

## Namespace

```csharp
using DotnetAuthServer.Services;
```

## API

### `void LogTokenIssuance(string userId, string clientId, string grantType, string scopes, string? ipAddress = null)`

Records a successful token issuance event.

- `userId`: Identifier of the user for whom the token was issued.
- `clientId`: Identifier of the client receiving the token.
- `grantType`: OAuth grant type used to obtain the token.
- `scopes`: Scopes associated with the issued token.
- `ipAddress`: Optional originating IP address. The default is `null`.

### `void LogAuthentication(string userId, string? username, string? ipAddress = null, bool success = true)`

Records the outcome of a user authentication attempt.

- `userId`: Identifier of the user being authenticated.
- `username`: Optional username associated with the attempt.
- `ipAddress`: Optional originating IP address. The default is `null`.
- `success`: Whether authentication succeeded. The default is `true`.

Implementations can distinguish successful and failed attempts through the `success` value.

### `void LogAuthorizationDecision(string userId, string clientId, bool granted, string reason = "")`

Records a decision to grant or deny authorization.

- `userId`: Identifier of the user affected by the decision.
- `clientId`: Identifier of the requesting client.
- `granted`: `true` when authorization was granted; `false` when it was denied.
- `reason`: Optional explanation for the decision. The default is an empty string.

### `void LogSuspiciousActivity(string activityType, string? userId = null, string? clientId = null, string? ipAddress = null)`

Records potentially suspicious behavior, such as repeated invalid credentials or a rate-limit violation.

- `activityType`: Description or category of the suspicious activity.
- `userId`: Optional related user identifier.
- `clientId`: Optional related client identifier.
- `ipAddress`: Optional originating IP address.

### `void LogAdministrativeAction(string action, string? targetClientId = null, string? targetUserId = null, Dictionary<string, string>? changes = null)`

Records a configuration change or another administrative operation.

- `action`: Name or description of the administrative action.
- `targetClientId`: Optional identifier of the client affected by the action.
- `targetUserId`: Optional identifier of the user affected by the action.
- `changes`: Optional key-value collection describing the changes.

Callers should treat `changes` as event data passed to the implementation and should not rely on the dictionary remaining unchanged after the call.

### `IEnumerable<AuditLogEntry> GetRecentEntries(int count = 100)`

Returns up to `count` recent audit entries. The default limit is 100. The concrete implementation determines its storage and retention strategy, so callers should not assume that this method provides a complete audit history.

### `IEnumerable<AuditLogEntry> GetEntriesByEventTypeAndTimeRange(string eventType, DateTime startTime, DateTime endTime, int maxCount = 100)`

Returns entries whose event type matches `eventType` and whose timestamps fall within the inclusive range from `startTime` through `endTime`.

- `eventType`: Event type to match, such as `TOKEN_ISSUED` or `AUTH_SUCCESS`.
- `startTime`: Inclusive lower timestamp bound.
- `endTime`: Inclusive upper timestamp bound.
- `maxCount`: Maximum number of entries to return. The default is 100.

Matching entries are ordered from newest to oldest.

### `void Clear()`

Clears audit entries held by the implementation. This operation should be used deliberately: production audit records are commonly retained in durable storage according to security and compliance policies.

### `string ExportToCsv(DateTime startTime, DateTime endTime, int maxCount = 1000)`

Exports audit entries within the inclusive timestamp range as CSV.

- `startTime`: Inclusive lower timestamp bound.
- `endTime`: Inclusive upper timestamp bound.
- `maxCount`: Maximum number of entries to export. The default is 1,000.

The returned string includes the CSV representation produced by the implementation, including any header row.

## Usage

Inject the interface into application components that need to produce audit records:

```csharp
using DotnetAuthServer.Services;

public sealed class LoginHandler
{
    private readonly IAuditLoggingService _auditLogging;

    public LoginHandler(IAuditLoggingService auditLogging)
    {
        _auditLogging = auditLogging;
    }

    public void RecordLogin(
        string userId,
        string username,
        string? ipAddress,
        bool succeeded)
    {
        _auditLogging.LogAuthentication(
            userId,
            username,
            ipAddress,
            success: succeeded);
    }
}
```

Query a bounded set of failed authentications for a UTC time range:

```csharp
DateTime endTime = DateTime.UtcNow;
DateTime startTime = endTime.AddHours(-1);

IEnumerable<AuditLogEntry> failures =
    auditLogging.GetEntriesByEventTypeAndTimeRange(
        "AUTH_FAILURE",
        startTime,
        endTime,
        maxCount: 100);
```

## Implementation considerations

- Keep retrieval and export requests bounded by using the count parameters.
- Use a consistent time basis—normally UTC—for timestamps and query ranges.
- Avoid placing secrets, access tokens, passwords, or unnecessary personal data in audit fields.
- Treat audit data as security-sensitive and apply appropriate access controls, retention rules, and tamper protection in production implementations.
- The supplied `AuditLoggingService` implementation stores a bounded collection in memory. Applications that require durable or centralized audit records can provide another implementation of this interface.
