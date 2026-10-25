# AuthorizationGrantRepository

`AuthorizationGrantRepository` is the in-memory implementation of `IAuthorizationGrantRepository`. It stores `AuthorizationGrant` entities (issued authorization codes and their state) in a `ConcurrentDictionary<string, AuthorizationGrant>` keyed by `GrantId`. The class is `public sealed`, lives in `src/Data/Repositories/AuthorizationGrantRepository.cs`, and uses the namespace `DotnetAuthServer.Data.Repositories`.

It is registered as a singleton in `Program.cs`:

```csharp
builder.Services.AddSingleton<IAuthorizationGrantRepository, AuthorizationGrantRepository>();
```

Consumers depend on the interface, not the class: `AuthorizationService`, `TokenIssuer`, `TokenRevocationHandler`, and `TokenCleanupWorker`.

Storage is in memory only. Data is lost on restart, and nothing is shared between processes. The interface is the seam for a persistent implementation.

## Type declaration

```csharp
public sealed class AuthorizationGrantRepository : IAuthorizationGrantRepository
```

The class has no public constructor parameters and no public members beyond those listed below. The dictionary is a private field and uses `StringComparer.OrdinalIgnoreCase` for keys, so `GrantId` values are compared case-insensitively.

## Inherited members

`IAuthorizationGrantRepository` extends `IRepository<AuthorizationGrant, string>`. The class implements all of these members.

| Member | Signature | Summary |
|---|---|---|
| `GetByIdAsync` | `Task<AuthorizationGrant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)` | Returns the grant with the given `GrantId`, or `null`. |
| `GetAllAsync` | `Task<IEnumerable<AuthorizationGrant>> GetAllAsync(CancellationToken cancellationToken = default)` | Returns a snapshot list of all grants. |
| `CreateAsync` | `Task<AuthorizationGrant> CreateAsync(AuthorizationGrant entity, CancellationToken cancellationToken = default)` | Adds a grant. Throws if the ID already exists. |
| `UpdateAsync` | `Task<AuthorizationGrant> UpdateAsync(AuthorizationGrant entity, CancellationToken cancellationToken = default)` | Replaces an existing grant. Throws if it does not exist. |
| `DeleteAsync` | `Task DeleteAsync(AuthorizationGrant entity, CancellationToken cancellationToken = default)` | Deletes the grant with the entity's `GrantId`. |
| `DeleteByIdAsync` | `Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)` | Deletes the grant with the given ID, if present. Does not throw when it is missing. |
| `ExistsAsync` | `Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)` | Returns whether a grant with the given ID exists. |

## Authorization-grant-specific members

These are declared on `IAuthorizationGrantRepository` and implemented by the class.

### `GetByCodeAsync`

```csharp
Task<AuthorizationGrant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
```

Returns the grant whose `Code` matches `code`, or `null` if none matches. The comparison is case-insensitive (`StringComparison.OrdinalIgnoreCase`). The lookup scans every stored grant, so it is O(n).

**Throws:** `ArgumentException` (parameter `code`) if `code` is `null` or empty.

### `GetByUserIdAsync`

```csharp
Task<IEnumerable<AuthorizationGrant>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
```

Returns every grant whose `UserId` matches `userId`. The comparison is case-insensitive. The result is a new list, so later changes to the store do not affect it. If no grant matches, the result is empty.

**Throws:** `ArgumentException` (parameter `userId`) if `userId` is `null` or empty.

### `GetByClientIdAsync`

```csharp
Task<IEnumerable<AuthorizationGrant>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
```

Returns every grant whose `ClientId` matches `clientId`. The comparison is case-insensitive. The result is a new list. If no grant matches, the result is empty.

**Throws:** `ArgumentException` (parameter `clientId`) if `clientId` is `null` or empty.

### `DeleteExpiredAsync`

```csharp
Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
```

Removes every grant for which `AuthorizationGrant.IsExpired()` returns `true`, that is, where `DateTime.UtcNow >= ExpiresAt`. Expired grants are collected first and then removed, so the method does not change the dictionary while it is enumerating it.

## Member details

### `GetByIdAsync`

Returns the stored grant for `id`, or `null` if there is none. It does not check whether the grant is expired, used, or revoked.

### `GetAllAsync`

Returns a new `List<AuthorizationGrant>` built from a snapshot of the dictionary's values. It includes expired, used, and revoked grants. Callers that need only valid grants must filter them, for example with `AuthorizationGrant.IsValid()`.

### `CreateAsync`

Adds `entity` under `entity.GrantId` using `TryAdd`. It returns the same instance it was given.

**Throws:** `InvalidOperationException` with the message `Grant with ID {GrantId} already exists` if the key is already present. Because keys are compared case-insensitively, a grant whose `GrantId` differs only by case from an existing one is also rejected.

### `UpdateAsync`

Replaces the stored value for `entity.GrantId` with `entity`. It returns the same instance. The stored object is replaced by reference, so the caller's instance becomes the store's instance.

**Throws:** `InvalidOperationException` with the message `Grant with ID {GrantId} not found` if no grant with that ID exists. The check and the write are separate steps, so a concurrent `DeleteAsync` between them can still lead to the entry being re-added (`_grants[...] = entity` is an indexer set).

### `DeleteAsync`

Calls `DeleteByIdAsync(entity.GrantId)`. Only the ID of `entity` is used.

### `DeleteByIdAsync`

Calls `TryRemove` for `id` and ignores the result. It does not throw when the ID is absent, so repeated calls are safe.

### `ExistsAsync`

Returns `ContainsKey(id)`. The key comparison is case-insensitive.

## Behavior notes

- **Async is nominal.** Every method runs synchronously and returns a completed task (`Task.FromResult` or `Task.CompletedTask`). The `CancellationToken` parameters are accepted and ignored, so a cancelled token does not stop an operation.
- **Stored references.** The repository stores the entity instances it receives and returns the same instances from reads. Changes made to a returned grant, such as `MarkAsUsed()` or `Revoke()`, are visible to the store immediately, even without calling `UpdateAsync`. Callers that need a clean copy must make one themselves.
- **Collection results are snapshots.** `GetAllAsync`, `GetByUserIdAsync`, `GetByClientIdAsync`, and `DeleteExpiredAsync` materialize their results with `ToList()`. The returned collections are not live views.
- **Case-insensitive keys and lookups.** Authorization codes are matched case-insensitively in `GetByCodeAsync`. Most OAuth code implementations compare codes exactly, so a code's case is not significant here. Keep this in mind when reviewing security-sensitive changes.
- **No locking across operations.** Each dictionary call is thread-safe on its own, but sequences such as "check then update" or "get then mark as used" are not atomic across calls.
- **No automatic expiry.** Expired grants stay in memory until `DeleteExpiredAsync` runs. Its callers are `TokenCleanupWorker` and `AuthorizationService`.

## Usage example

```csharp
// Create a grant when an authorization code is issued
var grant = new AuthorizationGrant
{
    GrantId = Guid.NewGuid().ToString(),
    Code = code,
    ClientId = client.ClientId,
    UserId = user.UserId,
    RedirectUri = redirectUri,
    RequestedScopes = scopes,
    GrantedScopes = scopes,
    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
};
await repository.CreateAsync(grant, cancellationToken);

// Redeem the code: look up, validate, and mark as used
var stored = await repository.GetByCodeAsync(code, cancellationToken);
if (stored is { } && stored.IsValid())
{
    stored.MarkAsUsed();
    await repository.UpdateAsync(stored, cancellationToken);
}
```

## Related

- `docs/AuthorizationGrant.md` – the entity stored by this repository.
- `docs/IAuthorizationGrantRepository.md` – the interface this class implements.
- `docs/ARCHITECTURE.md` – storage design and known limitations.
