# RefreshTokenRepository

`RefreshTokenRepository` is the in-memory implementation of `IRefreshTokenRepository`. It stores `RefreshToken` entities in a `ConcurrentDictionary<string, RefreshToken>` keyed by `RefreshToken.TokenId`, and it is registered in `Program.cs` as a singleton (`AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>()`). The class is `public sealed` and lives in `src/Data/Repositories/RefreshTokenRepository.cs`, namespace `DotnetAuthServer.Data.Repositories`.

The class implements the generic `IRepository<RefreshToken, string>` CRUD contract and the five refresh-token-specific members of `IRefreshTokenRepository`: `GetByTokenHashAsync`, `GetByUserIdAsync`, `GetByClientIdAsync`, `GetValidTokensByUserAsync`, `RevokeAllUserTokensAsync`, and `DeleteExpiredAsync`. The interface is declared in the same file. This page describes what the in-memory class actually does.

## Storage characteristics

- **Key:** `RefreshToken.TokenId`. The dictionary uses `StringComparer.OrdinalIgnoreCase`, so `"ABC"` and `"abc"` address the same token. Stored `TokenId` values keep their original casing.
- **Identity:** the stored object is the same instance the caller passed in. Getters return live references, not copies. Mutating a returned `RefreshToken` changes the stored state immediately.
- **Hash lookup:** `GetByTokenHashAsync` scans all values and compares `TokenHash` with `OrdinalIgnoreCase`. It is O(n) and does not use an index.
- **Snapshots:** collection-returning methods materialise a `List<RefreshToken>` at call time. Later additions or removals do not affect a list already returned.
- **Cancellation:** every method accepts a `CancellationToken`, but none of them observe it. The work is synchronous in-memory; async methods wrap the result with `Task.FromResult`.
- **Durability:** nothing is persisted. All tokens are lost on process restart.
- **Lifetime:** singleton. One dictionary is shared by every request and every scope, including `TokenCleanupWorker`.

## Constructor

### `public RefreshTokenRepository()`

Implicit parameterless constructor. It creates an empty dictionary. There are no options or dependencies.

## Members

### `GetByIdAsync`

```csharp
public Task<RefreshToken?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```

Gets a refresh token by its `TokenId`, or `null` if no such token is stored. The lookup is case-insensitive.

- **Throws:** `ArgumentException` (via `ArgumentException.ThrowIfNullOrEmpty`) when `id` is `null` or empty. The method is `async`, so the exception is delivered through the returned task.

### `GetAllAsync`

```csharp
public Task<IEnumerable<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
```

Returns every stored refresh token, valid, revoked, or expired, as a new list. The list is empty when the repository is empty.

### `CreateAsync`

```csharp
public Task<RefreshToken> CreateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
```

Adds `entity` under its `TokenId` and returns the same instance. `CreatedAt` and `UpdatedAt` are not changed; they keep whatever values the entity already has.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- **Throws:** `InvalidOperationException` with the message `Token with ID {id} already exists` when a token with the same ID (case-insensitive) is already stored. The existing entry is not replaced.
- **Throws:** `ArgumentNullException` from the dictionary when `entity.TokenId` is `null`. `RefreshToken.TokenId` defaults to `null!`, so callers must set it first.

### `UpdateAsync`

```csharp
public Task<RefreshToken> UpdateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
```

Replaces the stored token that has the same `TokenId`. It sets `entity.UpdatedAt = DateTime.UtcNow` on the passed instance, stores it, and returns that instance. Because the store holds references, a token obtained from a getter is already up to date after mutation, and `UpdateAsync` mainly refreshes `UpdatedAt`.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- **Throws:** `InvalidOperationException` with the message `Token with ID {id} not found` when no token with that ID exists. Nothing is added in this case.
- **Side effect:** `UpdatedAt` is overwritten. Callers cannot set it through this method.

### `DeleteAsync`

```csharp
public Task DeleteAsync(RefreshToken entity, CancellationToken cancellationToken = default)
```

Removes the token whose `TokenId` matches `entity`. It delegates to `DeleteByIdAsync(entity.TokenId)`.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- Deleting a token that is not stored is not an error.

### `DeleteByIdAsync`

```csharp
public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
```

Removes the token with the given ID, if present. The lookup is case-insensitive.

- **Throws:** `ArgumentException` when `id` is `null` or empty. The method is not `async`, so the exception is thrown synchronously when the method is called, not through the returned task.
- A missing ID is silently ignored.

### `ExistsAsync`

```csharp
public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
```

Returns `true` when a token with the given ID is stored, `false` otherwise. The lookup is case-insensitive. It reports presence only; it does not check whether the token is valid.

- **Throws:** `ArgumentException` when `id` is `null` or empty (delivered through the returned task).

### `GetByTokenHashAsync`

```csharp
public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
```

Returns the first stored token whose `TokenHash` equals `tokenHash` case-insensitively, or `null` if none matches. The result is not filtered by revocation or expiry; callers must check `IsValid()` themselves.

- **Throws:** `ArgumentException` when `tokenHash` is `null` or empty (delivered through the returned task).
- **Caveat:** the comparison calls `TokenHash.Equals`, so a stored token whose `TokenHash` is `null` causes a `NullReferenceException` during the scan. `TokenHash` defaults to `null!`.

### `GetByUserIdAsync`

```csharp
public Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
```

Returns every stored token whose `UserId` matches `userId` case-insensitively, including revoked and expired tokens. The list is empty when there are none.

- **Throws:** `ArgumentException` when `userId` is `null` or empty (delivered through the returned task).

### `GetByClientIdAsync`

```csharp
public Task<IEnumerable<RefreshToken>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
```

Returns every stored token whose `ClientId` matches `clientId` case-insensitively, regardless of revocation or expiry. The list is empty when there are none.

- **Throws:** `ArgumentException` when `clientId` is `null` or empty (delivered through the returned task).

### `GetValidTokensByUserAsync`

```csharp
public Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(string userId, CancellationToken cancellationToken = default)
```

Returns the tokens for `userId` (case-insensitive) for which `RefreshToken.IsValid()` is `true`, meaning not revoked and not yet past `ExpiresAt`. Revoked and expired tokens are excluded.

- **Throws:** `ArgumentException` when `userId` is `null` or empty (delivered through the returned task).

### `RevokeAllUserTokensAsync`

```csharp
public Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken cancellationToken = default)
```

Calls `Revoke(reason)` on every stored token whose `UserId` matches `userId` (case-insensitive). `Revoke` sets `IsRevoked`, `RevokedAt`, `RevocationReason`, and `UpdatedAt`. The tokens are changed in place; no `UpdateAsync` call is made.

- Tokens that are already revoked are revoked again, so their `RevokedAt` and `RevocationReason` are overwritten with the new values.
- Expired but unrevoked tokens are also marked revoked.
- If the user has no tokens, nothing happens and no error is raised.
- **Throws:** `ArgumentException` when `userId` or `reason` is `null` or empty. The method is not `async`, so these are thrown synchronously.

### `DeleteExpiredAsync`

```csharp
public Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
```

Removes every stored token for which `RefreshToken.IsExpired()` is `true`, that is, `UtcNow >= ExpiresAt`. Revoked tokens that have not yet expired are kept. The method takes a snapshot of expired IDs first, then removes them one by one, so a token that is re-added in between is not affected.

- Does not throw for normal input.
- This is the method `TokenCleanupWorker` uses to purge storage. Nothing else in the repository removes tokens automatically.

## Behaviour summary

| Method | Filters by | Revoked tokens | Expired tokens | Argument errors surface |
|---|---|---|---|---|
| `GetByIdAsync` | `TokenId` | returned | returned | via task |
| `GetAllAsync` | none | returned | returned | n/a |
| `CreateAsync` | `TokenId` (duplicate check) | n/a | n/a | via task (`ArgumentNullException`) |
| `UpdateAsync` | `TokenId` | n/a | n/a | via task |
| `DeleteAsync` | `TokenId` | n/a | n/a | via task |
| `DeleteByIdAsync` | `TokenId` | n/a | n/a | **synchronous** |
| `ExistsAsync` | `TokenId` | counted | counted | via task |
| `GetByTokenHashAsync` | `TokenHash` | returned | returned | via task |
| `GetByUserIdAsync` | `UserId` | returned | returned | via task |
| `GetByClientIdAsync` | `ClientId` | returned | returned | via task |
| `GetValidTokensByUserAsync` | `UserId` + `IsValid()` | excluded | excluded | via task |
| `RevokeAllUserTokensAsync` | `UserId` | re-revoked | revoked | **synchronous** |
| `DeleteExpiredAsync` | `IsExpired()` | kept if not expired | removed | n/a |

## Related

- `IRefreshTokenRepository` and `IRepository<T, TKey>`: declared in `src/Data/Repositories/RefreshTokenRepository.cs` and `src/Data/Repositories/IRepository.cs`.
- `RefreshToken` entity: `src/Domain/Entities/RefreshToken.cs`. `IsValid()`, `IsExpired()`, and `Revoke(string?)` define the behaviour described above.
- Consumers include `RefreshTokenHandler`, `TokenIssuer`, `TokenRevocationHandler`, `UserService`, `UserManagementService`, and `TokenCleanupWorker`.
