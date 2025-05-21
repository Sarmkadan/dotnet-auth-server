# ScopeMetadataHandler

`ScopeMetadataHandler` manages the metadata associated with OAuth 2.0 scopes in the authorization server. It provides retrieval of individual and collective scope metadata, identification of scopes that require explicit user consent, and registration of custom application-defined scopes. The type serves as the central registry for scope definitions, exposing both built-in and custom scope information to the authorization and consent flows.

## API

### public ScopeMetadataHandler

Constructor. Initializes a new instance of the handler with the default set of known scopes and an empty custom scope registry. No parameters are required.

### public async Task<ScopeMetadata?> GetScopeMetadataAsync

Retrieves metadata for a single scope by its name.

- **Parameters**: `string name` — the scope identifier to look up.
- **Returns**: A `ScopeMetadata` instance if the scope is registered; `null` if the scope name is not found in either built-in or custom scopes.
- **Exceptions**: Throws `ArgumentException` when `name` is null or whitespace.

### public async Task<IEnumerable<ScopeMetadata>> GetScopesMetadataAsync

Retrieves metadata for a specific set of requested scope names.

- **Parameters**: `IEnumerable<string> scopeNames` — the collection of scope identifiers to resolve.
- **Returns**: A collection of `ScopeMetadata` objects corresponding to the requested names. Scopes that are not registered are silently omitted from the result.
- **Exceptions**: Throws `ArgumentNullException` when `scopeNames` is null.

### public async Task<IEnumerable<ScopeMetadata>> GetAllScopesAsync

Returns metadata for every registered scope, both built-in and custom.

- **Parameters**: None.
- **Returns**: A collection containing all `ScopeMetadata` instances currently known to the handler.
- **Exceptions**: None thrown under normal operation.

### public IEnumerable<ScopeMetadata> GetScopesRequiringConsent

Returns only those registered scopes where `RequiresConsent` is `true`.

- **Parameters**: None.
- **Returns**: A filtered collection of `ScopeMetadata` instances that must be presented to the user for explicit approval.
- **Exceptions**: None thrown.

### public void RegisterCustomScope

Adds a new custom scope definition to the handler at runtime.

- **Parameters**: `ScopeMetadata scopeMetadata` — the fully populated metadata object for the custom scope.
- **Returns**: Void.
- **Exceptions**: Throws `ArgumentNullException` when `scopeMetadata` is null. Throws `InvalidOperationException` when a scope with the same `Name` is already registered (built-in or custom).

### public string Name

Gets the unique identifier for the scope (e.g., `"openid"`, `"profile"`, `"read:orders"`). This is the value used in OAuth scope parameters and tokens.

### public string DisplayName

Gets the human-readable title for the scope, intended for display on consent screens (e.g., `"Read your profile information"`).

### public string Description

Gets the longer-form explanation of what access the scope grants, used in consent descriptions and documentation.

### public bool RequiresConsent

Gets whether the scope must be explicitly approved by the resource owner. Scopes marked `true` trigger a consent prompt; those marked `false` may be granted silently when policy allows.

### public string? Icon

Gets an optional URI or identifier for an icon associated with the scope, used in consent UI rendering. Returns `null` when no icon is defined.

### public List<string> RelatedScopes

Gets the list of other scope names that are semantically related to this scope. Used to suggest bundled scopes during authorization or to display context on consent screens.

## Usage

### Example 1: Resolving requested scopes and determining consent requirements

```csharp
var handler = new ScopeMetadataHandler();

// Scopes requested by a client application
var requestedScopes = new[] { "openid", "profile", "read:orders", "admin:users" };

// Resolve metadata for the requested scopes
var resolvedMetadata = await handler.GetScopesMetadataAsync(requestedScopes);

// Separate scopes requiring consent from those that can be granted silently
var consentRequired = resolvedMetadata
    .Where(m => m.RequiresConsent)
    .Select(m => m.Name);

foreach (var scopeName in consentRequired)
{
    Console.WriteLine($"Consent required for: {scopeName}");
}
```

### Example 2: Registering a custom scope and verifying registration

```csharp
var handler = new ScopeMetadataHandler();

var customScope = new ScopeMetadata
{
    Name = "export:data",
    DisplayName = "Export your data",
    Description = "Allows the application to export your account data in portable formats.",
    RequiresConsent = true,
    Icon = "https://example.com/icons/export.svg",
    RelatedScopes = new List<string> { "read:orders", "profile" }
};

// Register the custom scope
handler.RegisterCustomScope(customScope);

// Verify it appears in the full registry
var allScopes = await handler.GetAllScopesAsync();
var registered = allScopes.Any(s => s.Name == "export:data");
Console.WriteLine($"Custom scope registered: {registered}");
```

## Notes

- **Scope name uniqueness**: `RegisterCustomScope` enforces uniqueness across both built-in and custom scopes. Attempting to register a scope whose `Name` collides with an existing entry throws `InvalidOperationException`. Check for existence via `GetScopeMetadataAsync` before registering if collisions are possible.
- **Async resolution**: `GetScopeMetadataAsync` and `GetScopesMetadataAsync` are asynchronous to accommodate potential backend lookups (e.g., database-stored custom scopes). Implementations may involve I/O; callers should avoid synchronous blocking.
- **Missing scopes**: `GetScopesMetadataAsync` silently drops unknown scope names from its result set. Callers comparing requested scopes against resolved metadata should account for this by detecting discrepancies in count or presence.
- **Consent calculation**: `GetScopesRequiringConsent` returns a live-filtered view based on the current state of the registry. If custom scopes are registered after this method is called, subsequent calls will include them if `RequiresConsent` is `true`.
- **Thread safety**: The handler is not designed for concurrent mutation. `RegisterCustomScope` modifies internal collections without synchronization. In multi-threaded scenarios, scope registration should be performed during application startup before the handler is exposed to request-processing threads. Read operations (`GetScopeMetadataAsync`, `GetAllScopesAsync`, `GetScopesRequiringConsent`) are safe to call concurrently once registration is complete.
- **Null and whitespace handling**: `GetScopeMetadataAsync` rejects null or whitespace-only scope names with `ArgumentException`. Empty strings are treated as whitespace and rejected. Valid scope names must contain at least one non-whitespace character.
- **Icon property**: The `Icon` property is nullable. Consumers rendering consent UI must handle `null` gracefully, falling back to a default icon or omitting the icon entirely.
