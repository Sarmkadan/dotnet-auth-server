# AuditLoggingService

**Namespace:** `DotnetAuthServer.Services`

Records security-relevant events (token issuance, authentication, authorization decisions, suspicious activity, administrative actions) in an in-memory, bounded queue and writes each event to `ILogger`. The class is `public sealed` and implements [`IAuditLoggingService`](IAuditLoggingService.md). It holds no persistent state; entries live only until they are evicted by the size cap or removed by `Clear()`.

The same file also declares two public types used by the API: [`AuditLogEntry`](#auditlogentry) and [`AuditSeverity`](#auditseverity).

## API

| Member | Returns | Event type / purpose |
|---|---|---|
| [`AuditLoggingService(ILogger<AuditLoggingService>)`](#constructor) | | Constructor |
| [`LogTokenIssuance`](#logtokenissuance) | `void` | `TOKEN_ISSUED` |
| [`LogAuthentication`](#logauthentication) | `void` | `AUTH_SUCCESS` / `AUTH_FAILURE` |
| [`LogAuthorizationDecision`](#logauthorizationdecision) | `void` | `AUTHORIZATION_GRANTED` / `AUTHORIZATION_DENIED` |
| [`LogSuspiciousActivity`](#logsuspiciousactivity) | `void` | `SUSPICIOUS_ACTIVITY` (Warning) |
| [`LogAdministrativeAction`](#logadministrativeaction) | `void` | `ADMIN_ACTION` (Critical) |
| [`GetRecentEntries`](#getrecententries) | `IEnumerable<AuditLogEntry>` | Read the newest entries |
| [`GetEntriesByEventTypeAndTimeRange`](#getentriesbyeventtypeandtimerange) | `IEnumerable<AuditLogEntry>` | Filter by event type and time range |
| [`Clear`](#clear) | `void` | Remove all entries |
| [`ExportToCsv(DateTime, DateTime, int)`](#exporttocsvstarttime-endtime-maxcount) | `string` | Export a time range to CSV |
| [`ExportToCsv(IEnumerable<AuditLogEntry>)`](#exporttocsventries) | `string` | Static: format any entries as CSV |

Unless a method says otherwise, every method logs an `Information` message when it starts and when it completes. Logging calls do not change the stored data.

### Storage and Size Cap

Entries are kept in a `ConcurrentQueue<AuditLogEntry>`. The cap is 10,000 entries, held in a private field (`_maxLogEntries`) that is not configurable. When a new entry pushes the count above the cap, the oldest entry is dequeued. Eviction is silent: nothing is logged and no error is raised.

### Constructor

```csharp
public AuditLoggingService(ILogger<AuditLoggingService> logger)
```

Stores the logger. In `Program.cs` the service is registered as `AddScoped<IAuditLoggingService, AuditLoggingService>()`, so consumers should depend on `IAuditLoggingService` and the logger is resolved from DI.

**Parameters**

- `logger`: The logger used for the method-level messages and for the `AUDIT:` lines.

### LogTokenIssuance

```csharp
public void LogTokenIssuance(
    string userId,
    string clientId,
    string grantType,
    string scopes,
    string? ipAddress = null)
```

Records a successful token issuance. The entry has event type `TOKEN_ISSUED` and severity `Information`.

**Parameters**

- `userId`: The subject of the token. Stored as `UserId`.
- `clientId`: The client that received the token. Stored as `ClientId`.
- `grantType`: The grant type, stored in `Details["grant_type"]`.
- `scopes`: The granted scopes, stored in `Details["scopes"]`.
- `ipAddress`: The caller's IP address. `null` is stored as `"unknown"` in `Details["ip_address"]`.

**Returns**

`void`.

**Exceptions**

None are thrown by the guards. See [Known Limitations](#known-limitations).

### LogAuthentication

```csharp
public void LogAuthentication(
    string userId,
    string? username,
    string? ipAddress = null,
    bool success = true)
```

Records an authentication attempt. The event type is `AUTH_SUCCESS` when `success` is `true` and `AUTH_FAILURE` otherwise. The severity is `Information` in both cases.

**Parameters**

- `userId`: The user being authenticated. Stored as `UserId`.
- `username`: The username, stored in `Details["username"]`. `null` is stored as `"unknown"`.
- `ipAddress`: The caller's IP address, stored in `Details["ip_address"]`. `null` is stored as `"unknown"`.
- `success`: `true` for a successful login, `false` for a failed one.

**Returns**

`void`.

### LogAuthorizationDecision

```csharp
public void LogAuthorizationDecision(
    string userId,
    string clientId,
    bool granted,
    string reason = "")
```

Records an authorization decision. The event type is `AUTHORIZATION_GRANTED` or `AUTHORIZATION_DENIED`, depending on `granted`. The severity is `Information`.

**Parameters**

- `userId`: The subject of the decision. Stored as `UserId`.
- `clientId`: The client requesting access. Stored as `ClientId`.
- `granted`: `true` to record a grant, `false` to record a denial.
- `reason`: Free-text reason, stored in `Details["reason"]`.

**Returns**

`void`.

### LogSuspiciousActivity

```csharp
public void LogSuspiciousActivity(
    string activityType,
    string? userId = null,
    string? clientId = null,
    string? ipAddress = null)
```

Records suspicious activity such as repeated invalid credentials or a rate-limit breach. The entry has event type `SUSPICIOUS_ACTIVITY` and severity `Warning`.

**Parameters**

- `activityType`: A label for the activity, stored in `Details["activity_type"]`.
- `userId`: The user involved, if known. Stored as `UserId`.
- `clientId`: The client involved, if known. Stored as `ClientId`.
- `ipAddress`: The caller's IP address. `null` is stored as `"unknown"` in `Details["ip_address"]`.

**Returns**

`void`.

### LogAdministrativeAction

```csharp
public void LogAdministrativeAction(
    string action,
    string? targetClientId = null,
    string? targetUserId = null,
    Dictionary<string, string>? changes = null)
```

Records a configuration change or administrative action. The entry has event type `ADMIN_ACTION` and severity `Critical`.

**Parameters**

- `action`: The action name. It is written to `Details["action"]`.
- `targetClientId`: The client affected by the action. Stored as `ClientId`.
- `targetUserId`: The user affected by the action. Stored as `UserId`.
- `changes`: Optional key/value details of the change. If `null`, a new dictionary is created.

**Returns**

`void`.

**Side effects**

The `changes` dictionary is **modified in place**: the `action` key is added to it. If the caller's dictionary already has an `action` key, that value is overwritten. The stored entry references the same dictionary instance, so later changes by the caller also show up in the stored entry.

### GetRecentEntries

```csharp
public IEnumerable<AuditLogEntry> GetRecentEntries(int count = 100)
```

Returns the last `count` entries in queue order (oldest to newest among the returned items).

**Parameters**

- `count`: The number of entries to return. Must be zero or greater.

**Returns**

A lazily evaluated `IEnumerable<AuditLogEntry>`. Each enumeration reads the current queue. Call `ToList()` if you need a stable snapshot.

**Exceptions**

- `ArgumentException`: `count` is negative.

### GetEntriesByEventTypeAndTimeRange

```csharp
public IEnumerable<AuditLogEntry> GetEntriesByEventTypeAndTimeRange(
    string eventType,
    DateTime startTime,
    DateTime endTime,
    int maxCount = 100)
```

Returns entries whose `EventType` equals `eventType` (ordinal, case-sensitive) and whose `Timestamp` is within `[startTime, endTime]`, inclusive on both ends. Results are ordered by `Timestamp` descending (newest first) and limited to `maxCount`.

**Parameters**

- `eventType`: The event type to match, for example `"TOKEN_ISSUED"`.
- `startTime`: The start of the range (inclusive). Compared against `Timestamp`, which is UTC.
- `endTime`: The end of the range (inclusive).
- `maxCount`: The maximum number of entries to return. Must be zero or greater.

**Returns**

A lazily evaluated `IEnumerable<AuditLogEntry>`.

**Exceptions**

- `ArgumentException`: `maxCount` is negative.

### Clear

```csharp
public void Clear()
```

Dequeues all entries from the in-memory queue. Logging messages are written before and after the operation.

**Parameters**

None.

**Returns**

`void`.

**Remarks**

In production, entries should be exported or persisted before they are cleared.

### ExportToCsv(startTime, endTime, maxCount)

```csharp
public string ExportToCsv(DateTime startTime, DateTime endTime, int maxCount = 1000)
```

Selects entries whose `Timestamp` is within `[startTime, endTime]` (inclusive), orders them newest first, takes up to `maxCount`, and formats them with the static [`ExportToCsv(IEnumerable<AuditLogEntry>)`](#exporttocsventries) overload. Unlike `GetEntriesByEventTypeAndTimeRange`, this method does not filter by event type.

**Parameters**

- `startTime`: The start of the range (inclusive).
- `endTime`: The end of the range (inclusive).
- `maxCount`: The maximum number of entries to export. Must be zero or greater.

**Returns**

The CSV text, including the header row.

**Exceptions**

- `ArgumentException`: `maxCount` is negative.

### ExportToCsv(entries)

```csharp
public static string ExportToCsv(IEnumerable<AuditLogEntry> entries)
```

Formats the given entries as CSV. The method does not log and does not read the internal queue, so it can be used on any sequence of entries.

The output has this header line:

```
Timestamp,EventType,UserId,ClientId,RequestId,Severity,Details
```

Rows are ordered by `Timestamp` descending. Each row contains:

- `Timestamp`: ISO 8601 round-trip format (`"o"`), invariant culture.
- `EventType`, `UserId`, `ClientId`, `RequestId`: the values as stored. `null` becomes an empty field.
- `Severity`: the enum name, for example `Warning`.
- `Details`: the `Details` dictionary serialized as JSON with `System.Text.Json`.

Fields that contain a comma, double quote, `\n`, or `\r` are wrapped in double quotes, and embedded quotes are doubled (RFC 4180 style). Lines are separated by `StringWriter.WriteLine`, which uses the platform newline.

**Parameters**

- `entries`: The entries to export. A `null` element is not handled and will throw `NullReferenceException`.

**Returns**

The CSV text, including the header row. If `entries` is empty, the result is the header line only.

## AuditLogEntry

```csharp
public sealed class AuditLogEntry
```

A single audit record. All properties have public getters and setters.

| Property | Type | Default | Description |
|---|---|---|---|
| `EventType` | `string` | `string.Empty` | Event type, such as `TOKEN_ISSUED`. |
| `UserId` | `string?` | `null` | User associated with the event, if any. |
| `ClientId` | `string?` | `null` | Client associated with the event, if any. |
| `Timestamp` | `DateTime` | `DateTime.UtcNow` | When the event occurred (UTC). |
| `RequestId` | `string?` | `null` | Correlation ID of the request, if any. |
| `Details` | `Dictionary<string, string>` | `new()` | Extra key/value details. |
| `Severity` | `AuditSeverity` | `Information` | Severity of the event. |

The service sets `Timestamp` and `RequestId` itself when it records an event. `RequestId` comes from `LogicalContext.RequestId`.

## AuditSeverity

```csharp
public enum AuditSeverity
```

| Member | Value | Meaning | Used by |
|---|---|---|---|
| `Information` | 0 | Routine event. | Token, authentication, and authorization events. |
| `Warning` | 1 | Needs attention but is not a failure. | `LogSuspiciousActivity` |
| `Critical` | 2 | Serious event that needs immediate attention. | `LogAdministrativeAction` |

The severity is set by the logging method. Callers cannot choose it through the public API.

The log level written to `ILogger` for each event matches the severity: `Critical` maps to `LogLevel.Critical`, `Warning` to `LogLevel.Warning`, and `Information` to `LogLevel.Information`. Each event produces an `AUDIT: {EventType} | User: ... | Client: ... | RequestId: ...` line.

## Usage

```csharp
// Injected through DI as IAuditLoggingService (scoped registration).
public sealed class LoginHandler
{
    private readonly IAuditLoggingService _audit;

    public LoginHandler(IAuditLoggingService audit)
    {
        _audit = audit;
    }

    public void OnLoginFailed(string userId, string username, string ip)
    {
        _audit.LogAuthentication(userId, username, ip, success: false);
        _audit.LogSuspiciousActivity("REPEATED_FAILED_LOGIN", userId, ipAddress: ip);
    }
}

// Export the last hour of events to CSV.
var now = DateTime.UtcNow;
string csv = audit.ExportToCsv(now.AddHours(-1), now, maxCount: 500);

// Read the most recent token issuances.
var issued = audit
    .GetEntriesByEventTypeAndTimeRange("TOKEN_ISSUED", now.AddDays(-1), now, maxCount: 50)
    .ToList();
```

## Known Limitations

- **Argument guards never fire.** Several methods call `ArgumentException.ThrowIfNullOrEmpty(nameof(...))`. That passes the parameter name (a non-empty string), not the parameter value, so the guard never throws. For example, `LogTokenIssuance(null!, ...)` does not throw, and `UserId` is stored as `null`.
- **Storage is in-memory, capped, and scoped.** The service is registered with `AddScoped`, so each DI scope (normally each HTTP request) gets its own queue. Entries written in one request are not visible to `GetRecentEntries` or the query methods in another. Entries beyond 10,000 are dropped silently, oldest first. Nothing survives a restart. Persist events to an external sink if they need to be queried across requests.
- **`LogAdministrativeAction` mutates its input.** See [LogAdministrativeAction](#logadministrativeaction).
- **Lazy results.** `GetRecentEntries` and `GetEntriesByEventTypeAndTimeRange` return deferred queries. Materialize them with `ToList()` if you need a stable view.
- **CSV does not neutralize formulas.** Values that start with `=`, `+`, `-`, or `@` are not escaped, so spreadsheet applications may treat them as formulas. Check exports before opening them in a spreadsheet.
- **No query by severity or user.** The public API filters by event type and time only. Other filters have to be applied to the results of `GetRecentEntries`.

## See Also

- [IAuditLoggingService](IAuditLoggingService.md)
