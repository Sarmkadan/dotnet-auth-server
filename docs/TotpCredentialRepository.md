# TotpCredentialRepository

`TotpCredentialRepository` is the in-memory implementation of `ITotpCredentialRepository`. It stores `TotpCredential` entities in a `ConcurrentDictionary<string, TotpCredential>` keyed by `TotpCredential.Id`, and it is registered in `Program.cs` as a singleton (`AddSingleton<ITotpCredentialRepository, TotpCredentialRepository>()`). The class is `public sealed` and lives in `src/Data/Repositories/TotpCredentialRepository.cs`, namespace `DotnetAuthServer.Data.Repositories`.

The class implements the generic `IRepository<TotpCredential, string>` CRUD contract and the one user-scoped member of `ITotpCredentialRepository`: `GetByUserIdAsync` and `DeleteByUserIdAsync`. The interface is declared in the same file as the class; this page describes what the in-memory implementation actually does.

## Storage characteristics

- **Key:** `TotpCredential.Id`. The dictionary uses `StringComparer.OrdinalIgnoreCase`, so `"ABC"` and `"abc"` address the same record. Stored `Id` values keep their original casing.
- **User lookup:** `UserId` matching is also `OrdinalIgnoreCase`. Nothing enforces one credential per user: `CreateAsync` accepts several credentials with the same `UserId`.
- **Identity:** the stored object is the same instance the caller passed in. Getters return live references, not copies. Mutating a returned `TotpCredential` changes the stored state immediately, even before `UpdateAsync` is called.
- **Snapshots:** `GetAllAsync` materialises a `List<TotpCredential>` at call time. Later additions or removals do not affect a list already returned.
- **Cancellation:** every method accepts a `CancellationToken`, but none of them observe it. The work is synchronous in-memory; the methods wrap their results with `Task.FromResult` or `Task.CompletedTask`.
- **Exception timing:** none of the members is declared `async`. Argument validation therefore throws synchronously from the call itself, not through the returned task.
- **Secrets:** `SecretKey` is stored as given, in plain text. The entity documentation says it must be encrypted at rest in production; this repository does not do that.
- **Durability:** nothing is persisted. All data is lost on process restart.

## Constructor

### `public TotpCredentialRepository()`

Implicit parameterless constructor. It creates an empty dictionary. There are no options or dependencies.

## Members

### `GetByIdAsync`

```csharp
public Task<TotpCredential?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```

Gets the credential with the given record ID, or `null` if none is stored. The lookup is case-insensitive.

- **Throws:** `ArgumentNullException` when `id` is `null`, and `ArgumentException` when `id` is empty. Both are thrown synchronously. Whitespace is not rejected.

### `GetAllAsync`

```csharp
public Task<IEnumerable<TotpCredential>> GetAllAsync(CancellationToken cancellationToken = default)
```

Returns every stored credential as a new list, whether or not `IsEnabled` is set. The list is empty when the repository is empty.

### `CreateAsync`

```csharp
public Task<TotpCredential> CreateAsync(TotpCredential entity, CancellationToken cancellationToken = default)
```

Adds `entity` under its `Id` and returns the same instance. No fields on the entity are changed; `CreatedAt` and the other timestamps keep their current values.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- **Throws:** `InvalidOperationException` with the message `TOTP credential {id} already exists` when a credential with the same `Id` (case-insensitive) is already stored. The existing entry is not replaced.
- **Does not check `UserId`:** a second credential for a user that already has one is accepted.

### `UpdateAsync`

```csharp
public Task<TotpCredential> UpdateAsync(TotpCredential entity, CancellationToken cancellationToken = default)
```

Replaces the stored credential that has the same `Id` and returns the passed instance. Unlike `ClientRepository.UpdateAsync`, it does not touch any timestamp on the entity.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- **Throws:** `InvalidOperationException` with the message `TOTP credential {id} not found` when no credential with that `Id` exists. Nothing is added in this case.
- **Concurrency:** the existence check and the indexer write are two separate operations. A `DeleteByIdAsync` that runs between them is undone, and the credential is re-added. See [Concurrency](#concurrency).

### `DeleteAsync`

```csharp
public Task DeleteAsync(TotpCredential entity, CancellationToken cancellationToken = default)
```

Removes the credential whose `Id` matches `entity`. It delegates to `DeleteByIdAsync(entity.Id)`.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- Deleting a credential that is not stored is not an error.

### `DeleteByIdAsync`

```csharp
public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
```

Removes the credential with the given record ID, if present. The lookup is case-insensitive.

- **Throws:** `ArgumentNullException` when `id` is `null`. This comes from the underlying `ConcurrentDictionary`, not from explicit validation, and is thrown synchronously.
- An empty `id` and a missing ID are both silently ignored.

### `ExistsAsync`

```csharp
public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
```

Returns `true` when a credential with the given record ID is stored, `false` otherwise. The lookup is case-insensitive.

- **Throws:** `ArgumentNullException` when `id` is `null`, thrown synchronously from the underlying dictionary. An empty `id` returns `false`.

### `GetByUserIdAsync`

```csharp
public Task<TotpCredential?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
```

Returns the first stored credential whose `UserId` matches `userId` (case-insensitive), or `null` when the user has none. `TotpService` calls this method to load a user's credential before verifying or updating it.

- **Multiple matches:** when more than one credential has the same `UserId`, the one returned is whichever the dictionary enumerates first. The order is not defined, so callers should not rely on which credential comes back.
- **Null `userId`:** not rejected. The comparison against a non-null `UserId` returns `false`, so the result is `null`.
- **Null stored `UserId`:** a stored credential whose `UserId` is `null` (the entity default before it is set) causes a `NullReferenceException` during the scan. `CreateAsync` does not prevent this.

### `DeleteByUserIdAsync`

```csharp
public Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
```

Removes every stored credential whose `UserId` matches `userId` (case-insensitive). It collects the matching IDs first, then removes each one.

- **Throws:** `ArgumentException` (via `ArgumentException.ThrowIfNullOrEmpty`) when `userId` is `null` or empty, thrown synchronously. Whitespace is not rejected.
- Users with no credentials are silently ignored.
- **Concurrency:** the matching IDs are collected before removal, so a credential created for the same user during the call can survive it.

## Concurrency

- `CreateAsync` relies on `ConcurrentDictionary.TryAdd`, which is atomic. Two concurrent creates with the same `Id` cannot both succeed.
- `UpdateAsync` is not atomic (`ContainsKey` followed by an indexer write). A concurrent delete can be undone by an update, which re-adds the record.
- `DeleteByUserIdAsync` is a snapshot followed by per-ID removal. It does not block concurrent creates for the same user.
- The repository has no locking across operations. Callers that need a consistent check-then-act sequence (for example, "create only if the user has no credential") must add their own synchronisation.

## Related

- `ITotpCredentialRepository` — the interface this class implements; declared in `src/Data/Repositories/TotpCredentialRepository.cs`.
- `TotpCredential` — the entity stored by this repository (`src/Domain/Entities/TotpCredential.cs`).
- `ClientRepository.md` — the sibling in-memory repository, for comparison of the shared conventions.
