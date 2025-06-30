# MemoryCacheService

`MemoryCacheService` is a lightweight, thread-safe in-memory cache that stores a single value per type `T`. It provides asynchronous methods for retrieving, setting, removing, and clearing cached entries, with optional expiration. The service is designed for scenarios where a small, typed cache is sufficient and where consistency and simplicity are preferred over a full-featured distributed cache.

## API

### `Task<T?> GetAsync<T>()`
Retrieves the cached value of type `T`, if present and not expired.  
- **Returns**: A `Task<T?>` that resolves to the cached value, or `default(T)` if the entry does not exist or has expired.  
- **Throws**: `InvalidCastException` if the cached object cannot be cast to `T`.

### `Task SetAsync<T>(T value, TimeSpan? expiration = null)`
Stores a value of type `T` in the cache, replacing any existing entry for that type.  
- **Parameters**:  
  - `value` – The object to cache.  
  - `expiration` – Optional absolute or sliding expiration duration. If `null`, the entry never expires.  
- **Returns**: A `Task` representing the asynchronous operation.  
- **Throws**: `ArgumentNullException` if `value` is `null`.

### `Task RemoveAsync()`
Removes the cached entry for the type `T` (inferred from the calling context).  
- **Returns**: A `Task` representing the asynchronous operation.  
- **Throws**: None.

### `Task RemoveByPatternAsync(string pattern)`
Removes all cached entries whose type name matches the specified glob pattern.  
- **Parameters**:  
  - `pattern` – A string pattern (e.g., `"MyApp.*"`) used to match type names.  
- **Returns**: A `Task` representing the asynchronous operation.  
- **Throws**: `ArgumentException` if `pattern` is `null` or empty.

### `Task ClearAsync()`
Removes all cached entries regardless of type.  
- **Returns**: A `Task` representing the asynchronous operation.  
- **Throws**: None.

### `async Task<T?> GetOrSetAsync<T>(Func<Task<T>> factory, TimeSpan? expiration = null)`
Atomically retrieves the cached value of type `T` or, if missing or expired, creates and caches a new value using the provided factory.  
- **Parameters**:  
  - `factory` – An asynchronous delegate that produces the value to cache.  
  - `expiration` – Optional expiration for the newly created entry.  
- **Returns**: A `Task<T?>` that resolves to the cached or newly created value.  
- **Throws**: `ArgumentNullException` if `factory` is `null`.

### `object? Value`
Gets the raw cached object for the most recently accessed type. This property is not type-safe and is intended for diagnostic or inspection purposes.  
- **Value**: The cached object, or `null` if no entry exists.

### `DateTime? ExpiresAt`
Gets the absolute expiration time of the currently cached entry (for the most recently accessed type), or `null` if the entry never expires.  
- **Value**: A `DateTime?` representing the expiration time in UTC, or `null`.

## Usage

### Example 1: Basic caching with expiration
```csharp
public class UserProfileService
{
    private readonly MemoryCacheService _cache = new();

    public async Task<UserProfile> GetProfileAsync(int userId)
    {
        // Cache the profile for 5 minutes
        return await _cache.GetOrSetAsync<UserProfile>(async () =>
        {
            return await FetchProfileFromDatabase(userId);
        }, TimeSpan.FromMinutes(5));
    }

    private async Task<UserProfile> FetchProfileFromDatabase(int userId)
    {
        // Simulate database call
        await Task.Delay(100);
        return new UserProfile { Id = userId, Name = "John Doe" };
    }
}
```

### Example 2: Manual set, get, and removal
```csharp
public class ConfigurationCache
{
    private readonly MemoryCacheService _cache = new();

    public async Task LoadConfigurationAsync()
    {
        var config = await LoadFromFileAsync("appsettings.json");
        await _cache.SetAsync(config, TimeSpan.FromHours(1));
    }

    public async Task<AppConfig?> GetConfigurationAsync()
    {
        return await _cache.GetAsync<AppConfig>();
    }

    public async Task InvalidateAsync()
    {
        await _cache.RemoveAsync(); // Removes the AppConfig entry
    }
}
```

## Notes

- **Thread safety**: All public methods are thread-safe. Concurrent calls to `GetOrSetAsync` will only execute the factory once; subsequent callers receive the same cached value.
- **Expiration**: Expiration times are evaluated on every `GetAsync` or `GetOrSetAsync` call. Expired entries are lazily evicted and will not be returned.
- **Type isolation**: Each type `T` is treated as a separate cache slot. Storing a value for `string` does not affect a value for `int`.
- **`Value` and `ExpiresAt`**: These properties reflect the state of the most recently accessed type (via any method). They are not reliable for concurrent scenarios where multiple types are accessed simultaneously.
- **`RemoveByPatternAsync`**: Pattern matching uses simple wildcard syntax (`*` matches any sequence of characters). The pattern is compared against the full type name (e.g., `"MyApp.Models.User"`).
- **Null values**: Setting a `null` value is not permitted and will throw `ArgumentNullException`. The cache treats missing entries and expired entries identically (both return `default(T)`).
- **Memory pressure**: The cache holds strong references to objects. For large or numerous entries, consider using a more scalable solution or periodically calling `ClearAsync`.
