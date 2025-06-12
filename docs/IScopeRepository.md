# IScopeRepository

`IScopeRepository` defines the contract for managing OAuth 2.0 / OpenID Connect scopes within the `dotnet-auth-server` project. It provides asynchronous operations for CRUD lifecycle management, querying by identifier, retrieving active scopes, searching, validating scope strings, and associating claims and roles with scopes. Implementations are expected to persist scope definitions to a backing data store.

## API

### ScopeService

```csharp
ScopeService ScopeService { get; }
```

Exposes the underlying `ScopeService` instance that this repository delegates to for domain logic such as scope creation, claim assignment, role assignment, and scope validation. The property provides direct access when callers need to invoke service-level methods not surfaced directly on the repository interface.

---

### GetByIdAsync

```csharp
Task<Scope?> GetByIdAsync(string id);
```

Retrieves a scope by its internal persistent identifier.

**Parameters:**
- `id` — The internal unique identifier of the scope record.

**Returns:** The matching `Scope` instance, or `null` if no scope with the given internal identifier exists.

**Throws:** `ArgumentNullException` when `id` is null or empty.

---

### GetAllAsync

```csharp
Task<IEnumerable<Scope>> GetAllAsync();
```

Returns all scopes stored in the repository, regardless of their active/inactive status.

**Returns:** An enumerable collection of all `Scope` records.

---

### CreateAsync

```csharp
Task<Scope> CreateAsync(Scope scope);
```

Persists a new scope definition.

**Parameters:**
- `scope` — The fully constructed `Scope` object to persist.

**Returns:** The created `Scope` instance, typically with its internal identifier populated after persistence.

**Throws:** `ArgumentNullException` when `scope` is null. May throw a duplicate-key exception if a scope with the same `ScopeId` already exists.

---

### UpdateAsync

```csharp
Task<Scope> UpdateAsync(Scope scope);
```

Updates an existing scope definition.

**Parameters:**
- `scope` — The `Scope` object containing modified values. Must have a valid internal identifier matching an existing record.

**Returns:** The updated `Scope` instance as persisted.

**Throws:** `ArgumentNullException` when `scope` is null. May throw a concurrency or not-found exception if the record does not exist or has been modified concurrently.

---

### DeleteAsync

```csharp
Task DeleteAsync(Scope scope);
```

Deletes a scope by its full entity object.

**Parameters:**
- `scope` — The `Scope` entity to remove.

**Throws:** `ArgumentNullException` when `scope` is null. May throw if the scope is referenced by existing grants or tokens depending on implementation.

---

### DeleteByIdAsync

```csharp
Task DeleteByIdAsync(string id);
```

Deletes a scope using its internal persistent identifier.

**Parameters:**
- `id` — The internal unique identifier of the scope to remove.

**Throws:** `ArgumentNullException` when `id` is null or empty. May throw if no scope with the given identifier exists.

---

### ExistsAsync

```csharp
Task<bool> ExistsAsync(string scopeId);
```

Checks whether a scope with the given logical `ScopeId` value already exists.

**Parameters:**
- `scopeId` — The logical scope identifier string (e.g., `"openid"`, `"profile"`, `"api:read"`).

**Returns:** `true` if a scope with that logical identifier exists; otherwise `false`.

**Throws:** `ArgumentNullException` when `scopeId` is null or empty.

---

### GetByScopeIdAsync

```csharp
Task<Scope?> GetByScopeIdAsync(string scopeId);
```

Retrieves a scope by its logical `ScopeId` value rather than its internal identifier.

**Parameters:**
- `scopeId` — The logical scope identifier string.

**Returns:** The matching `Scope` instance, or `null` if no scope with that logical identifier exists.

**Throws:** `ArgumentNullException` when `scopeId` is null or empty.

---

### GetActiveScopesAsync

```csharp
Task<IEnumerable<Scope>> GetActiveScopesAsync();
```

Returns only scopes that are currently marked as active and available for use in authorization requests.

**Returns:** An enumerable collection of active `Scope` records.

---

### SearchAsync

```csharp
Task<IEnumerable<Scope>> SearchAsync(string query);
```

Performs a text search across scope definitions, typically matching against display names, descriptions, or scope identifiers.

**Parameters:**
- `query` — The search string to match against scope properties.

**Returns:** An enumerable collection of `Scope` records whose properties contain the query string.

**Throws:** `ArgumentNullException` when `query` is null.

---

### CreateScopeAsync

```csharp
Task<Scope> CreateScopeAsync(string scopeId, string displayName, string description, bool isOpenIdScope);
```

Creates a new scope definition from its constituent parts, delegating to the `ScopeService` for construction and validation before persistence.

**Parameters:**
- `scopeId` — The logical scope identifier string.
- `displayName` — The human-readable display name for the scope.
- `description` — A description of what access the scope grants.
- `isOpenIdScope` — `true` if this scope is an OpenID Connect scope (e.g., `"openid"`, `"profile"`); `false` for resource scopes.

**Returns:** The newly created and persisted `Scope` instance.

**Throws:** `ArgumentNullException` when `scopeId`, `displayName`, or `description` is null or empty. May throw if a scope with the given `scopeId` already exists.

---

### AddClaimToScopeAsync

```csharp
Task AddClaimToScopeAsync(string scopeId, string claimType);
```

Associates a claim with an existing scope so that tokens issued with that scope include the specified claim.

**Parameters:**
- `scopeId` — The logical scope identifier to which the claim should be added.
- `claimType` — The claim type string (e.g., `"email"`, `"role"`, `"custom:permission"`).

**Throws:** `ArgumentNullException` when either parameter is null or empty. May throw if the scope does not exist.

---

### AssignRoleAsync

```csharp
Task AssignRoleAsync(string scopeId, string roleName);
```

Associates a role with an existing scope, enabling role-based access control semantics through scope assignment.

**Parameters:**
- `scopeId` — The logical scope identifier to which the role should be assigned.
- `roleName` — The name of the role to associate.

**Throws:** `ArgumentNullException` when either parameter is null or empty. May throw if the scope or role does not exist.

---

### GetScopesWithClaimsAsync

```csharp
Task<IEnumerable<ScopeSummary>> GetScopesWithClaimsAsync();
```

Returns a summary view of all scopes along with their associated claims, suitable for discovery endpoints or administrative dashboards.

**Returns:** An enumerable collection of `ScopeSummary` objects, each containing scope metadata and its mapped claims.

---

### ValidateAndFilterScopesAsync

```csharp
Task<IEnumerable<string>> ValidateAndFilterScopesAsync(IEnumerable<string> requestedScopes);
```

Validates a collection of requested scope strings against the repository, filtering out any that are unknown, inactive, or otherwise unavailable.

**Parameters:**
- `requestedScopes` — The collection of scope strings requested by a client.

**Returns:** An enumerable collection containing only the valid and available scope strings from the input set.

**Throws:** `ArgumentNullException` when `requestedScopes` is null.

---

### ScopeId

```csharp
string ScopeId { get; }
```

The logical scope identifier string (e.g., `"openid"`, `"api:admin"`). This is the value used in authorization requests and token responses.

---

### DisplayName

```csharp
string DisplayName { get; }
```

The human-readable display name for the scope, intended for consent screens and administrative interfaces.

---

### Description

```csharp
string Description { get; }
```

A description of the access or claims this scope represents, shown to end-users during consent and to administrators during configuration.

---

### IsOpenIdScope

```csharp
bool IsOpenIdScope { get; }
```

Indicates whether this scope is an OpenID Connect scope. When `true`, the scope relates to identity and authentication; when `false`, it represents an API or resource access scope.

## Usage

### Example 1: Creating and Configuring a New API Scope

```csharp
// Obtain the repository via dependency injection
IScopeRepository scopeRepo = serviceProvider.GetRequiredService<IScopeRepository>();

// Create a new resource scope for API access
Scope apiScope = await scopeRepo.CreateScopeAsync(
    scopeId: "api:documents:read",
    displayName: "Read Documents",
    description: "Grants read-only access to user documents",
    isOpenIdScope: false
);

// Associate claims with the scope
await scopeRepo.AddClaimToScopeAsync("api:documents:read", "document:read");
await scopeRepo.AddClaimToScopeAsync("api:documents:read", "document:metadata");

// Assign a role for coarse-grained access control
await scopeRepo.AssignRoleAsync("api:documents:read", "DocumentViewer");

// Verify the scope exists
bool exists = await scopeRepo.ExistsAsync("api:documents:read");
// exists == true
```

### Example 2: Validating Requested Scopes During Token Issuance

```csharp
IScopeRepository scopeRepo = serviceProvider.GetRequiredService<IScopeRepository>();

// Scopes requested by a client application
IEnumerable<string> requestedScopes = new[]
{
    "openid",
    "profile",
    "api:documents:read",
    "api:admin:destroy"  // This scope may not exist or be inactive
};

// Filter to only valid, active scopes
IEnumerable<string> validScopes = await scopeRepo.ValidateAndFilterScopesAsync(requestedScopes);

// validScopes might contain: ["openid", "profile", "api:documents:read"]
// "api:admin:destroy" is silently dropped

// Retrieve full metadata for the valid scopes
foreach (string scopeId in validScopes)
{
    Scope? scope = await scopeRepo.GetByScopeIdAsync(scopeId);
    if (scope is not null)
    {
        Console.WriteLine($"Issuing scope: {scope.DisplayName} (OpenID: {scope.IsOpenIdScope})");
    }
}
```

## Notes

- **Thread safety:** The interface itself defines no synchronization guarantees. Implementations are expected to be registered as scoped or singleton services and must be thread-safe if shared across concurrent requests. Callers should not rely on atomicity across multiple method calls (e.g., checking `ExistsAsync` then calling `CreateAsync`) without external coordination.
- **Duplicate detection:** `CreateAsync` and `CreateScopeAsync` may throw when a scope with the same `ScopeId` already exists. Callers should either check `ExistsAsync` first (accepting the race condition) or catch the persistence-layer exception.
- **Soft deletion:** The interface does not distinguish between hard and soft deletion. `DeleteAsync` and `DeleteByIdAsync` may physically remove the record or mark it inactive depending on the implementation. Callers should not assume deleted scopes can be recovered.
- **Scope validation behavior:** `ValidateAndFilterScopesAsync` silently drops unknown or inactive scopes rather than throwing. Callers comparing input and output counts can detect mismatches to log or reject requests with entirely invalid scope sets.
- **OpenID scope semantics:** The `IsOpenIdScope` flag influences token issuance behavior. Scopes marked as OpenID scopes typically trigger ID token generation and identity claim inclusion, while resource scopes govern access token claims.
- **`ScopeService` access:** The `ScopeService` property exposes domain logic directly. Direct invocation bypasses any repository-level interceptors, caching, or decorators that may wrap the interface methods. Prefer the repository methods unless service-level functionality is explicitly required.
- **Null and empty guards:** All methods accepting string parameters throw `ArgumentNullException` (or equivalent) for null or empty arguments. Implementations are expected to validate inputs eagerly rather than deferring to the persistence layer.
