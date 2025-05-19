# TokenIssuedEvent

Represents an event raised when an OAuth token is successfully issued by the authorization server. This event captures the essential context of the token issuance, including the identities of the user and client, the grant type used, the scopes authorized, and the token's lifetime. It is designed for auditing, monitoring, and logging purposes within the token issuance pipeline.

## API

### `public string EventId`

A unique identifier for this event instance. This value is generated at the time the event is created and can be used for correlation across distributed logging and tracing systems.

### `public DateTime OccurredAt`

The UTC timestamp at which the token issuance occurred. This is recorded at the moment the event is constructed and reflects the server-side time of the successful token generation.

### `public string? RequestId`

An optional correlation identifier that links this event to the originating HTTP request or an upstream operation. May be `null` if no request identifier was available or provided during the token flow.

### `public string UserId`

The unique identifier of the authenticated user for whom the token was issued. This is always populated when the event represents a token issued on behalf of a user (e.g., authorization code, resource owner password credentials grants).

### `public string ClientId`

The unique identifier of the OAuth client application that requested and received the token. This is always populated and corresponds to the registered client within the authorization server.

### `public string GrantType`

The OAuth grant type used to obtain the token. Typical values include `authorization_code`, `client_credentials`, `refresh_token`, or custom grant type identifiers. This is always populated.

### `public IEnumerable<string> Scopes`

The collection of scope strings granted in the token. This represents the permissions associated with the issued token. The enumeration may be empty if no explicit scopes were requested or granted, but it is never `null`.

### `public int ExpiresInSeconds`

The lifetime of the issued token in seconds, measured from `OccurredAt`. This value reflects the configured token expiration policy applied at issuance time.

### `public string? ClientIpAddress`

The IP address of the client that made the token request, if available. May be `null` when the IP address cannot be determined or is not captured by the server infrastructure.

## Usage

### Example 1: Logging Token Issuance Details

```csharp
void HandleTokenIssued(TokenIssuedEvent e, ILogger logger)
{
    logger.LogInformation(
        "Token issued: EventId={EventId}, User={UserId}, Client={ClientId}, " +
        "Grant={GrantType}, Scopes={Scopes}, ExpiresIn={ExpiresInSeconds}s, IP={ClientIp}",
        e.EventId,
        e.UserId,
        e.ClientId,
        e.GrantType,
        string.Join(",", e.Scopes),
        e.ExpiresInSeconds,
        e.ClientIpAddress ?? "unknown");
}
```

### Example 2: Auditing Token Issuance to a Persistent Store

```csharp
async Task PersistAuditRecordAsync(TokenIssuedEvent e, AuditDbContext db)
{
    var record = new TokenAuditRecord
    {
        EventId = e.EventId,
        OccurredAt = e.OccurredAt,
        RequestId = e.RequestId,
        UserId = e.UserId,
        ClientId = e.ClientId,
        GrantType = e.GrantType,
        Scopes = e.Scopes.ToList(),
        ExpiresInSeconds = e.ExpiresInSeconds,
        ClientIpAddress = e.ClientIpAddress
    };

    db.TokenAuditRecords.Add(record);
    await db.SaveChangesAsync();
}
```

## Notes

- **Nullability:** `RequestId` and `ClientIpAddress` are nullable and must be checked before use in contexts that require non-null values. `Scopes` is guaranteed to be non-null but may be an empty enumeration.
- **Immutability:** Instances of `TokenIssuedEvent` are expected to be immutable after construction. All properties are read-only and set at creation time. Consumers should not attempt to modify the event.
- **Thread Safety:** The type is safe for concurrent read access from multiple threads once constructed, as it contains no mutable state. No synchronization is required when reading properties across threads.
- **Scope Enumeration:** The `Scopes` property exposes an `IEnumerable<string>`. Consumers that need to enumerate it multiple times should materialize it into a list or array to avoid potential multiple enumeration of an underlying sequence that may not support it.
- **Timestamp Precision:** `OccurredAt` uses `DateTime` with UTC semantics. Consumers should treat this as a point-in-time record and avoid comparing it to local time without proper conversion.
- **Event Identity:** `EventId` is unique per event instance and should not be used as a token identifier. It serves correlation and deduplication purposes in logging and auditing pipelines.
