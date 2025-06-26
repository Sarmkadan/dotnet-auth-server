# IClientRepository

Defines the contract for persisting and retrieving OAuth 2.0 / OpenID Connect client registrations. Implementations encapsulate data-access logic for the `Client` entity, providing asynchronous CRUD operations alongside specialised lookups for active clients and free-text search.

## API

### GetByIdAsync
```csharp
Task<Client?> GetByIdAsync(string clientId);
```
Retrieves a single client by its unique identifier.  
**Parameters:** `clientId` – the client’s primary key.  
**Returns:** the matching `Client` instance, or `null` if no record exists.  
**Throws:** `ArgumentNullException` when `clientId` is null or whitespace.

### GetAllAsync
```csharp
Task<IEnumerable<Client>> GetAllAsync();
```
Returns every registered client regardless of status.  
**Returns:** a collection of all `Client` entities; the collection is empty when no clients exist.

### CreateAsync
```csharp
Task<Client> CreateAsync(Client client);
```
Persists a new client registration.  
**Parameters:** `client` – a fully populated `Client` object (the repository assigns the identifier).  
**Returns:** the created `Client` with its generated identity and any store-assigned timestamps.  
**Throws:** `ArgumentNullException` when `client` is null; `ValidationException` (or a domain-specific exception) when required fields such as `ClientId` or `Name` are missing; `DuplicateKeyException` when a client with the same logical key already exists.

### UpdateAsync
```csharp
Task<Client> UpdateAsync(Client client);
```
Replaces an existing client record with the supplied state.  
**Parameters:** `client` – the `Client` object containing updated values; its identifier must match a persisted record.  
**Returns:** the updated `Client` as stored.  
**Throws:** `ArgumentNullException` when `client` is null; `KeyNotFoundException` when no record with the given identifier exists; `ConcurrencyException` when an optimistic-concurrency token mismatch is detected.

### DeleteAsync
```csharp
Task DeleteAsync(Client client);
```
Removes a client record using the full entity.  
**Parameters:** `client` – the `Client` to delete; typically obtained from a prior query.  
**Throws:** `ArgumentNullException` when `client` is null; `KeyNotFoundException` when the entity is not tracked by the store.

### DeleteByIdAsync
```csharp
Task DeleteByIdAsync(string clientId);
```
Removes a client record by its identifier alone, avoiding a full fetch.  
**Parameters:** `clientId` – the primary key of the client to delete.  
**Throws:** `ArgumentNullException` when `clientId` is null or whitespace; `KeyNotFoundException` when no matching record exists.

### ExistsAsync
```csharp
Task<bool> ExistsAsync(string clientId);
```
Checks whether a client with the given identifier is registered, without loading the full entity.  
**Parameters:** `clientId` – the primary key to test.  
**Returns:** `true` if the client exists; otherwise `false`.  
**Throws:** `ArgumentNullException` when `clientId` is null or whitespace.

### GetActiveClientAsync
```csharp
Task<Client?> GetActiveClientAsync(string clientId);
```
Retrieves a client only when it is both registered and marked as active (e.g. not disabled, revoked, or expired).  
**Parameters:** `clientId` – the primary key.  
**Returns:** the `Client` if it exists and is active; `null` when the client is missing or inactive.  
**Throws:** `ArgumentNullException` when `clientId` is null or whitespace.

### GetActiveClientsAsync
```csharp
Task<IEnumerable<Client>> GetActiveClientsAsync();
```
Returns all clients currently in an active state.  
**Returns:** a collection of active `Client` entities; empty when no active clients exist.

### SearchAsync
```csharp
Task<IEnumerable<Client>> SearchAsync(string query);
```
Performs a free-text search across client metadata (typically `ClientName`, `Description`, or allowed URIs).  
**Parameters:** `query` – the search term; may be a partial string.  
**Returns:** clients whose indexed fields match the query, ordered by relevance or name.  
**Throws:** `ArgumentNullException` when `query` is null or whitespace.

## Usage

### Registering a new client and confirming existence
```csharp
var client = new Client
{
    ClientId = "web-app-prod",
    ClientName = "Production Web Application",
    GrantTypes = new[] { "authorization_code", "refresh_token" },
    IsActive = true
};

Client created = await repository.CreateAsync(client);

bool exists = await repository.ExistsAsync(created.ClientId);
// exists == true
```

### Retrieving active clients and performing a search
```csharp
IEnumerable<Client> activeClients = await repository.GetActiveClientsAsync();

foreach (var c in activeClients)
{
    Console.WriteLine($"Active: {c.ClientName}");
}

IEnumerable<Client> searchResults = await repository.SearchAsync("mobile");

foreach (var c in searchResults)
{
    Console.WriteLine($"Match: {c.ClientName} — {c.Description}");
}
```

## Notes

- **Null/whitespace guards:** Every method accepting a string identifier throws `ArgumentNullException` for null or whitespace input. Callers should validate arguments before invocation.
- **Active-state semantics:** `GetActiveClientAsync` and `GetActiveClientsAsync` rely on a boolean or status field whose interpretation is implementation-defined. A disabled client is treated identically to a non-existent one (both return `null` or are excluded from the collection).
- **Concurrency:** `UpdateAsync` may employ optimistic concurrency (e.g. a row version). Callers must be prepared to catch `ConcurrencyException` and re-fetch the entity before retrying.
- **Search behaviour:** `SearchAsync` is intended for human-facing lookup; it is not a strict filter. Implementations may use substring matching, tokenisation, or full-text indexes. Results should never include sensitive secrets.
- **Thread safety:** The interface itself is stateless; thread safety depends entirely on the underlying implementation. Callers should assume no built-in synchronisation unless the concrete repository documents otherwise.
- **Delete vs DeleteById:** `DeleteAsync` accepts the entity, which may carry a concurrency token for safe deletion; `DeleteByIdAsync` performs a blind delete and should be used only when concurrency checks are unnecessary.
