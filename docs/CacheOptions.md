# CacheOptions

The `CacheOptions` class defines the configuration settings for the caching layer within the authentication server, controlling enablement, storage backend selection, global expiration policies, and specific time-to-live (TTL) values for distinct entity types such as clients, users, scopes, grants, and JSON Web Key Sets (JWKS).

## API

### Enabled
Gets or sets a boolean value indicating whether the caching mechanism is active. When set to `false`, the server bypasses the cache and retrieves data directly from the underlying data source for every request.

### Backend
Gets or sets the string identifier specifying the caching backend implementation to use (e.g., "Memory", "Redis", "Distributed"). The valid values depend on the registered cache providers within the application startup configuration.

### DefaultExpirationSeconds
Gets or sets the default time-to-live, in seconds, applied to cached items that do not have a specific expiration rule defined in the `ItemExpirations` configuration.

### MaxEntries
Gets or sets the maximum number of entries allowed in the cache when using an in-memory or size-constrained backend. Once this limit is reached, the cache eviction policy determines which items are removed to accommodate new entries.

### ExpirationScanIntervalSeconds
Gets or sets the interval, in seconds, at which the cache system scans for and removes expired entries. This is particularly relevant for in-memory caches that do not rely on external expiration events.

### ConnectionString
Gets or sets the optional connection string required to connect to an external caching provider (such as Redis). This property is null when using an in-memory backend.

### ItemExpirations
Gets or sets an instance of `CacheItemExpirations` that holds granular expiration configurations for specific categories of cached data, allowing overrides to the default expiration settings.

### ClientSeconds
Gets or sets the specific expiration time, in seconds, for cached client configuration data.

### UserSeconds
Gets or sets the specific expiration time, in seconds, for cached user profile and claim data.

### ScopeSeconds
Gets or sets the specific expiration time, in seconds, for cached scope definitions.

### GrantSeconds
Gets or sets the specific expiration time, in seconds, for cached authorization grant data.

### JwksSeconds
Gets or sets the specific expiration time, in seconds, for cached JSON Web Key Sets (JWKS) used for token validation.

## Usage

The following example demonstrates configuring `CacheOptions` for a high-performance environment using Redis, with specific TTLs for user and client data to balance freshness and load.

```csharp
var cacheOptions = new CacheOptions
{
    Enabled = true,
    Backend = "Redis",
    ConnectionString = "localhost:6379,abortConnect=false",
    DefaultExpirationSeconds = 300,
    MaxEntries = 10000,
    ExpirationScanIntervalSeconds = 60,
    ClientSeconds = 600,
    UserSeconds = 120,
    ScopeSeconds = 900,
    GrantSeconds = 300,
    JwksSeconds = 3600
};

// Assign to service configuration
services.Configure<CacheOptions>(options =>
{
    options.Enabled = cacheOptions.Enabled;
    options.Backend = cacheOptions.Backend;
    options.ConnectionString = cacheOptions.ConnectionString;
    options.ClientSeconds = cacheOptions.ClientSeconds;
    options.UserSeconds = cacheOptions.UserSeconds;
    // ... assign remaining properties
});
```

The following example shows a minimal configuration for a development environment using the default in-memory backend with short expiration times to facilitate rapid testing of configuration changes.

```csharp
var devCacheOptions = new CacheOptions
{
    Enabled = true,
    Backend = "Memory",
    DefaultExpirationSeconds = 60,
    MaxEntries = 500,
    ExpirationScanIntervalSeconds = 10,
    ClientSeconds = 60,
    UserSeconds = 30,
    ScopeSeconds = 60,
    GrantSeconds = 60,
    JwksSeconds = 120
};

// Direct assignment in startup logic
services.AddSingleton(devCacheOptions);
```

## Notes

*   **Backend Compatibility**: The `ConnectionString` property is ignored if the `Backend` is set to an in-memory provider. Conversely, external backends like Redis will likely throw a runtime connection exception if `ConnectionString` is null or malformed when that backend is selected.
*   **Precedence**: Specific duration properties (e.g., `UserSeconds`, `ClientSeconds`) typically take precedence over `DefaultExpirationSeconds` for their respective entity types. If a specific property is set to zero or a negative value, the behavior may default to the global setting or indicate non-expiring entries, depending on the underlying cache implementation.
*   **Thread Safety**: The `CacheOptions` class itself is a Plain Old CLR Object (POCO) and is not thread-safe for modification. It should be configured during application startup and treated as immutable during runtime operation. The underlying cache implementations referenced by these options are expected to handle concurrent access internally.
*   **Eviction Policy**: When `MaxEntries` is exceeded, the specific eviction strategy (e.g., Least Recently Used) is determined by the selected `Backend` implementation, not by this configuration class.
*   **Scan Interval**: Setting `ExpirationScanIntervalSeconds` to a very low value may increase CPU usage due to frequent scanning threads, while setting it too high may result in stale data remaining in the cache longer than the defined expiration times.
