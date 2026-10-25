# ScopeMetadataHandler

Scoped handler that resolves OAuth 2.0 / OpenID Connect scope metadata (display name, description, consent requirement) for consent screens and scope handling. It holds a registry of six built-in OIDC scopes and lets the application register its own scopes at runtime. Lookups go through `ICacheService` for the async methods.

The class is `public sealed` in namespace `DotnetAuthServer.Handlers` (`src/Handlers/ScopeMetadataHandler.cs`). `ScopeMetadata` is a `public sealed` class defined in the same file. It is registered as a scoped service in `Program.cs`.

## Behavior notes

- **Shared registry.** The built-in and custom scopes live in a `private static` dictionary. Every `ScopeMetadataHandler` instance in the process sees the same registry, and a registration made through one instance is visible through all of them.
- **Lookup is exact.** Scope names are matched with the dictionary's default string comparer, so lookups are case-sensitive and do not trim whitespace.
- **Caching.** `GetScopeMetadataAsync` checks `ICacheService` first, using the key `scope_metadata:{scopeName}`. On a miss, it looks up the registry and caches a found entry for 24 hours. Unknown names are not cached.
- **Stale cache after registration.** `RegisterCustomScope` does not invalidate cache entries. If a name was already resolved and cached, a later registration that replaces it is not seen until the cache entry expires (up to 24 hours).
- **Returned objects are shared.** The `ScopeMetadata` instances returned from the registry, and from the cache, are the same objects. Changes a caller makes to them, including to `RelatedScopes`, affect later lookups.
- **Logging.** `GetScopeMetadataAsync` writes an `Information` entry on every call and another on a cache hit. `RegisterCustomScope` writes `Information` on success and `Warning` when it skips a scope with no name.
- **Thread safety.** The registry is a plain `Dictionary<string, ScopeMetadata>`, not a concurrent collection, and `RegisterCustomScope` writes to it without a lock. Register all custom scopes during startup, before requests are served. The read methods are safe to call concurrently once registration is finished.

## Built-in scopes

| Name | Display name | Requires consent |
|---|---|---|
| `openid` | OpenID Connect | no |
| `profile` | Profile | yes |
| `email` | Email | yes |
| `phone` | Phone | yes |
| `address` | Address | yes |
| `offline_access` | Offline Access | yes |

Built-in entries set `Name`, `DisplayName`, `Description`, and `RequiresConsent`. They leave `Icon` as `null` and `RelatedScopes` empty.

## API

### Constructor

#### `ScopeMetadataHandler(ICacheService cacheService, ILogger<ScopeMetadataHandler> logger)`
Creates the handler. Throws `ArgumentNullException` if either argument is null.

### Methods

#### `Task<ScopeMetadata?> GetScopeMetadataAsync(string scopeName, CancellationToken cancellationToken = default)`
Returns the metadata for one scope, or `null` if the name is not registered.

- Throws `ArgumentNullException` when `scopeName` is null.
- Throws `ArgumentException` when `scopeName` is an empty string.
- Returns `null` without an error when `scopeName` is whitespace only.
- Checks the cache first, then the registry. A registry hit is cached for 24 hours.

#### `Task<IEnumerable<ScopeMetadata>> GetScopesMetadataAsync(IEnumerable<string> scopeNames, CancellationToken cancellationToken = default)`
Resolves each name with `GetScopeMetadataAsync` and returns the results as a `List<ScopeMetadata>`.

- Throws `ArgumentNullException` when `scopeNames` is null.
- Unknown and whitespace-only names are skipped without error. The result has no placeholder for them, so compare counts or names to detect what was dropped.
- Throws the same exceptions as `GetScopeMetadataAsync` for a null or empty element.
- The result keeps input order and includes duplicates.

#### `Task<IEnumerable<ScopeMetadata>> GetAllScopesAsync(CancellationToken cancellationToken = default)`
Returns metadata for every registered scope, built-in and custom, as a `List<ScopeMetadata>`. Each entry is resolved through `GetScopeMetadataAsync`, so the cache is used. The order is the registry's insertion order, which is not guaranteed by the dictionary contract.

#### `IEnumerable<ScopeMetadata> GetScopesRequiringConsent(IEnumerable<string> scopeNames)`
Filters the given names to those whose registry entry has `RequiresConsent == true`. This method is synchronous and does not use the cache.

- Throws `ArgumentNullException` immediately when `scopeNames` is null.
- The result is deferred. It is evaluated each time it is enumerated, against the current registry.
- Unknown names are skipped. A null element throws `ArgumentNullException` during enumeration.
- The result keeps input order and includes duplicates.

#### `void RegisterCustomScope(ScopeMetadata metadata)`
Adds or replaces a scope in the registry.

- Throws `ArgumentNullException` when `metadata` is null.
- When `metadata.Name` is null, empty, or whitespace, logs a `Warning` and returns without changing the registry.
- Otherwise stores `metadata` under `metadata.Name`. An existing entry with the same name is replaced without error. This includes built-in names, so registering `openid` replaces the built-in definition.
- Does not invalidate cached entries (see Behavior notes).

## `ScopeMetadata` properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | `string.Empty` | Scope identifier as sent in OAuth requests. Also the registry key. |
| `DisplayName` | `string` | `string.Empty` | Human-readable name shown to the user. |
| `Description` | `string` | `string.Empty` | Description of what the scope grants, shown to the user. |
| `RequiresConsent` | `bool` | `false` | Whether the user must explicitly consent to the scope. |
| `Icon` | `string?` | `null` | Optional icon identifier or URL. |
| `RelatedScopes` | `List<string>` | empty list | Names of other scopes related to this one. |

`RegisterCustomScope` stores the object you pass in, not a copy. Later changes to that object change the registry entry.

## Example

```csharp
// Resolved through the scoped instance from DI
var metadata = await scopeMetadataHandler.GetScopesMetadataAsync(["openid", "profile", "read:orders"]);

// Only scopes that need a consent prompt
var consentScopes = scopeMetadataHandler.GetScopesRequiringConsent(["openid", "profile", "read:orders"]);

// Custom scope, registered once at startup
scopeMetadataHandler.RegisterCustomScope(new ScopeMetadata
{
    Name = "read:orders",
    DisplayName = "Orders",
    Description = "Read your order history.",
    RequiresConsent = true
});
```
