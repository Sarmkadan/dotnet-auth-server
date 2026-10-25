# ClientRepository

`ClientRepository` is the in-memory implementation of `IClientRepository`. It stores `Client` entities in a `ConcurrentDictionary<string, Client>` keyed by `ClientId`, and it is registered in `Program.cs` as a singleton (`AddSingleton<IClientRepository, ClientRepository>()`). The class is `public sealed` and lives in `src/Data/Repositories/ClientRepository.cs`, namespace `DotnetAuthServer.Data.Repositories`.

The class implements the generic `IRepository<Client, string>` CRUD contract and the three client-specific members of `IClientRepository`: `GetActiveClientAsync`, `GetActiveClientsAsync`, and `SearchAsync`. The interface itself is documented in `IClientRepository.md`; this page describes what the in-memory class actually does.

## Storage characteristics

- **Key:** `Client.ClientId`. The dictionary uses `StringComparer.OrdinalIgnoreCase`, so `"ABC"` and `"abc"` address the same client. Stored `ClientId` values keep their original casing.
- **Identity:** the stored object is the same instance the caller passed in. `GetByIdAsync` and the other getters return live references, not copies. Mutating a returned `Client` changes the stored state immediately, even before `UpdateAsync` is called.
- **Snapshots:** collection-returning methods materialise a `List<Client>` at call time. Later additions or removals do not affect a list already returned.
- **Cancellation:** every method accepts a `CancellationToken`, but none of them observe it. The work is synchronous in-memory; async methods wrap the result with `Task.FromResult`.
- **Durability:** nothing is persisted. All data is lost on process restart.

## Constructor

### `public ClientRepository()`

Implicit parameterless constructor. It creates an empty dictionary. There are no options or dependencies.

## Members

### `GetByIdAsync`

```csharp
public Task<Client?> GetByIdAsync(string clientId, CancellationToken cancellationToken = default)
```

Gets a client by its client ID, or `null` if no such client is stored. The lookup is case-insensitive.

- **Throws:** `ArgumentException` (via `ArgumentException.ThrowIfNullOrEmpty`) when `clientId` is `null` or empty. Whitespace is not rejected. Because the method is `async`, the exception is delivered through the returned task rather than thrown synchronously.

### `GetAllAsync`

```csharp
public Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
```

Returns every stored client, active or not, as a new list. The list is empty when the repository is empty.

### `CreateAsync`

```csharp
public Task<Client> CreateAsync(Client entity, CancellationToken cancellationToken = default)
```

Adds `entity` under its `ClientId` and returns the same instance. `CreatedAt` and `UpdatedAt` are not changed by this method; they keep whatever values the entity already has.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- **Throws:** `InvalidOperationException` with the message `Client with ID {id} already exists` when a client with the same ID (case-insensitive) is already stored. The existing entry is not replaced.

### `UpdateAsync`

```csharp
public Task<Client> UpdateAsync(Client entity, CancellationToken cancellationToken = default)
```

Replaces the stored client that has the same `ClientId`. It sets `entity.UpdatedAt = DateTime.UtcNow` on the passed instance before storing it, then returns that instance. Because the store holds references, the stored object is the same instance and already reflects the change.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- **Throws:** `InvalidOperationException` with the message `Client with ID {id} not found` when no client with that ID exists. Nothing is added in this case.
- **Side effect:** `UpdatedAt` is overwritten. Callers cannot set it through this method.

### `DeleteAsync`

```csharp
public Task DeleteAsync(Client entity, CancellationToken cancellationToken = default)
```

Removes the client whose `ClientId` matches `entity`. It delegates to `DeleteByIdAsync(entity.ClientId)`.

- **Throws:** `ArgumentNullException` when `entity` is `null`.
- Deleting a client that is not stored is not an error.

### `DeleteByIdAsync`

```csharp
public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
```

Removes the client with the given ID, if present. The lookup is case-insensitive.

- **Throws:** `ArgumentException` when `id` is `null` or empty. Unlike the other async methods, this one is not `async`, so the exception is thrown synchronously when the method is called.
- A missing ID is silently ignored.

### `ExistsAsync`

```csharp
public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
```

Returns `true` when a client with the given ID is stored, `false` otherwise. The lookup is case-insensitive.

- **Throws:** `ArgumentException` when `id` is `null` or empty (delivered through the returned task).

### `GetActiveClientAsync`

```csharp
public Task<Client?> GetActiveClientAsync(string clientId, CancellationToken cancellationToken = default)
```

Returns the client with the given ID only if its `IsActive` flag is `true`. Returns `null` when the client does not exist or is inactive. Callers cannot tell those two cases apart from the return value.

- **Throws:** `ArgumentException` when `clientId` is `null` or empty (delivered through the returned task).

### `GetActiveClientsAsync`

```csharp
public Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
```

Returns a new list of every stored client whose `IsActive` is `true`. The list is empty when none are active.

### `SearchAsync`

```csharp
public Task<IEnumerable<Client>> SearchAsync(string query, CancellationToken cancellationToken = default)
```

Returns every stored client for which at least one of the following contains `query` as a substring:

- `ClientName`
- `ClientId`
- `Description` (skipped when `null`)

Matching is case-insensitive. The implementation lowercases both sides with `ToLower()`, which uses the current culture. Results are returned in dictionary enumeration order, which is not guaranteed.

- **Throws:** `ArgumentException` when `query` is `null` or empty (delivered through the returned task).
- **Note:** `ClientName` is declared non-nullable on `Client` (`= null!`) and is dereferenced without a null check. A client whose `ClientName` was set to `null` causes a `NullReferenceException` for any search.
- Active and inactive clients are both searched. Filter with `IsActive` if only active clients are wanted.

## Thread safety

The dictionary is a `ConcurrentDictionary`, so individual operations are safe to call concurrently. Compound operations are not atomic. For example, `ExistsAsync` followed by `CreateAsync` can race with another creator, and `UpdateAsync` does not check whether the stored object is the same instance the caller read. Callers that need atomic read-modify-write must add their own synchronisation.

## Related

- `IClientRepository.md`: the interface contract.
- `Client.md`: the entity stored by this repository.
- `ClientService.md`, `DynamicClientRegistrationService.md`, `ClientValidationService.md`, `TokenIssuer.md`: main consumers.
