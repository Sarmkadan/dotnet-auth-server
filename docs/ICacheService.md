# ICacheService

Abstraction for caching layer to enable swapping implementations.
Supports both simple memory caching and distributed cache backends.
Essential for performance when validating frequently-accessed scopes,
clients, and authorization codes.

## API

### `Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)`
where T : class

Retrieves a cached value by key.
Returns null if key does not exist or has expired.

#### Parameters
- `key`: The key identifying the cached value.
- `cancellationToken`: Optional token to cancel the operation.

#### Returns
The cached value as `T?` (nullable reference type) or `null` if not found or expired.

### `Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)`
where T : class

Stores a value in cache with optional expiration time.

#### Parameters
- `key`: The key under which to store the value.
- `value`: The value to cache (must be a reference type).
- `expiration`: Optional expiration time. If null, uses the cache's default expiration.
- `cancellationToken`: Optional token to cancel the operation.

#### Returns
A task representing the asynchronous operation.

### `Task RemoveAsync(string key, CancellationToken cancellationToken = default)`

Removes a cached value by key.
Idempotent - does not throw if key does not exist.

#### Parameters
- `key`: The key of the value to remove.
- `cancellationToken`: Optional token to cancel the operation.

#### Returns
A task representing the asynchronous operation.

### `Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)`

Removes all cached values matching a pattern.
Useful for invalidating related cache entries.
Implementation depends on backend capabilities.

#### Parameters
- `pattern`: The pattern to match keys against (implementation-specific).
- `cancellationToken`: Optional token to cancel the operation.

#### Returns
A task representing the asynchronous operation.

### `Task ClearAsync(CancellationToken cancellationToken = default)`

Clears all cached values.
Use sparingly - should only be called on graceful shutdown or explicit flush.

#### Parameters
- `cancellationToken`: Optional token to cancel the operation.

#### Returns
A task representing the asynchronous operation.

### `Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)`
where T : class

Gets a value from cache or executes a factory function if not cached.
Atomic operation - factory is only called once even in concurrent scenarios.

#### Parameters
- `key`: The key identifying the cached value.
- `factory`: A function that creates the value if not found in cache.
- `expiration`: Optional expiration time for the newly set value.
- `cancellationToken`: Optional token to cancel the operation.

#### Returns
The cached value as `T?` (nullable reference type) or `null` if factory returns null.

## Usage

This interface is implemented by concrete cache services such as `MemoryCacheService`.
It allows the application to depend on an abstraction rather than a specific caching implementation.

Example usage in a service:

```csharp
public class SomeService
{
    private readonly ICacheService _cache;

    public SomeService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<string> GetDataAsync(string key)
    {
        // Try to get from cache first
        var cached = await _cache.GetAsync<string>(key);
        if (cached != null)
        {
            return cached;
        }

        // If not in cache, fetch from source
        var data = await FetchDataFromSourceAsync(key);
        
        // Store in cache for future requests
        await _cache.SetAsync(key, data, TimeSpan.FromMinutes(10));
        
        return data;
    }
}
```