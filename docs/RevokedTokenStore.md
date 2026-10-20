# RevokedTokenStore

**Namespace:** `DotnetAuthServer.Security`

In-memory store for revoked JWT IDs (`jti` claims) and refresh-token families. Enables individual access-token revocation without invalidating all tokens for a user, and supports refresh-token rotation with reuse detection (OAuth 2.1). Entries are self-expiring: once the original token's expiry passes, the entry is pruned on the next access.

Thread-safe. Backed by `ConcurrentDictionary`. Upper bound: 10 000 revoked entries (triggers automatic purge on overflow).

---

## Constructor

### `RevokedTokenStore()`

Creates a new instance and initializes OpenTelemetry `Meter` instruments:

| Instrument | Name | Unit | Kind |
|---|---|---|---|
| Counter | `dotnet_auth_server.security.revoked_tokens.count` | tokens | Counter\<long\> |
| Histogram | `dotnet_auth_server.security.revoked_tokens.lifetime_seconds` | s | Histogram\<long\> |

---

## Methods

### Registration & Revocation

#### `void RegisterToken(string jti, DateTime expiresAt, string familyId)`

Registers a newly-issued token (access or refresh) with its family identifier. The token is **not** revoked - it is tracked so that a family-wide revocation can locate it later.

| Parameter | Type | Description |
|---|---|---|
| `jti` | `string` | JWT ID of the token. |
| `expiresAt` | `DateTime` | UTC expiry time. |
| `familyId` | `string` | Identifier grouping related refresh tokens. |

**Throws:** `ArgumentException` if `jti` or `familyId` is null or empty.

---

#### `void Revoke(string jti, DateTime tokenExpiresAt, string familyId)`

Revokes a single token by its `jti`. If the token was not previously registered, it is added to the token-info dictionary so family information is retained. Triggers automatic purge when the store exceeds 10 000 entries.

| Parameter | Type | Description |
|---|---|---|
| `jti` | `string` | JWT ID to revoke. |
| `tokenExpiresAt` | `DateTime` | UTC expiry time of the original token. |
| `familyId` | `string` | Family identifier. |

**Throws:** `ArgumentException` if `jti` or `familyId` is null or empty.

---

#### `void Revoke(string jti, DateTime tokenExpiresAt)`

Revokes a single token without specifying a family identifier. Provided for backward compatibility.

| Parameter | Type | Description |
|---|---|---|
| `jti` | `string` | JWT ID to revoke. |
| `tokenExpiresAt` | `DateTime` | UTC expiry time of the original token. |

**Throws:** `ArgumentNullException` if `jti` is null.

---

#### `void RevokeFamily(string familyId)`

Revokes **every** token belonging to the specified family. Intended for refresh-token reuse detection: when a previously-revoked refresh token is presented again, the entire family is invalidated to contain potential theft.

| Parameter | Type | Description |
|---|---|---|
| `familyId` | `string` | Family identifier to revoke. |

**Throws:** `ArgumentNullException` if `familyId` is null.

---

### Query

#### `bool IsRevoked(string jti, out string? familyId)`

Checks whether a token is revoked. If the token's natural expiry has passed, the entry is removed (lazy cleanup) and the method returns `false`.

| Parameter | Type | Description |
|---|---|---|
| `jti` | `string` | JWT ID to check. |
| `familyId` | `out string?` | Receives the family identifier when the token is revoked; `null` otherwise. |

**Returns:** `true` if the token is revoked and not yet expired.

**Throws:** `ArgumentNullException` if `jti` is null.

---

#### `bool IsRevoked(string jti)`

Convenience overload that discards the family identifier. Delegates to `IsRevoked(string, out string?)`.

**Returns:** `true` if the token is revoked and not yet expired.

**Throws:** `ArgumentNullException` if `jti` is null.

---

### Cleanup & Metrics

#### `void PurgeExpired()`

Removes all entries (both revoked and tracked) whose tokens have already expired. Called periodically by `BackgroundWorkers.TokenCleanupWorker` and automatically when the store exceeds 10 000 entries.

---

#### `void RemoveExpired(DateTime now)`

Same as `PurgeExpired()` but accepts an explicit UTC timestamp for comparison instead of using `DateTime.UtcNow`.

| Parameter | Type | Description |
|---|---|---|
| `now` | `DateTime` | The UTC time to compare against token expiry. |

---

#### `int Count()`

Returns the current number of revoked tokens in the store (does not include tokens that are only registered but not revoked).
