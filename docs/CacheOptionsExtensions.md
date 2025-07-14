# CacheOptionsExtensions

The `CacheOptionsExtensions` class provides a set of fluent extension methods for configuring `CacheOptions` instances within the `dotnet-auth-server` framework. These methods simplify the management of caching strategies, allowing developers to configure providers, expiration policies, and capacity limits in a structured and readable manner.

## API

### UseRedis
Configures the cache provider to use Redis as the underlying storage mechanism.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** None.

### UseMemory
Configures the cache provider to use an in-memory storage mechanism.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** None.

### SetDefaultExpiration
Sets the default duration after which a cache entry is considered expired.
* **Parameters:** A `TimeSpan` representing the default expiration duration.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** `ArgumentOutOfRangeException` if the provided duration is negative.

### SetMaxEntries
Sets the maximum number of entries allowed in the cache simultaneously.
* **Parameters:** An `int` representing the maximum entry count.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** `ArgumentOutOfRangeException` if the value is less than zero.

### SetExpirationScanInterval
Configures the frequency at which the background scanner clears expired items from the cache.
* **Parameters:** A `TimeSpan` representing the scan interval.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** `ArgumentOutOfRangeException` if the interval is zero or negative.

### GetExpirationSeconds
Retrieves the currently configured expiration time in seconds.
* **Returns:** An `int` representing the expiration duration in seconds.
* **Throws:** None.

### SetExpiration
Configures a specific expiration policy for cache entries.
* **Parameters:** A `TimeSpan` representing the desired expiration duration.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** `ArgumentOutOfRangeException` if the duration is negative.

### Disable
Disables the caching functionality globally for the associated `CacheOptions`.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** None.

### Enable
Enables the caching functionality for the associated `CacheOptions`.
* **Returns:** The modified `CacheOptions` instance.
* **Throws:** None.

## Usage

### Configuring a Redis Cache
```csharp
var options = new CacheOptions()
    .UseRedis()
    .SetDefaultExpiration(TimeSpan.FromMinutes(30))
    .SetMaxEntries(1000);
```

### Configuring an In-Memory Cache with Custom Scan Interval
```csharp
var options = new CacheOptions()
    .UseMemory()
    .SetExpiration(TimeSpan.FromHours(1))
    .SetExpirationScanInterval(TimeSpan.FromMinutes(5))
    .Enable();
```

## Notes

* **Thread Safety:** While `CacheOptions` instances are mutable during the configuration phase, they are typically configured once during application startup. After initialization, these options should be treated as read-only. Ensure configuration occurs before the cache service is registered or utilized to avoid concurrency issues.
* **Provider Conflicts:** The `UseRedis` and `UseMemory` methods are intended to be used exclusively. Calling both methods on the same `CacheOptions` instance will result in the last method call overwriting the previous provider configuration.
* **Fluent Interface:** All methods (excluding `GetExpirationSeconds`) return the `CacheOptions` instance, allowing for a fluent, chainable configuration style.
* **Validation:** While specific methods throw `ArgumentOutOfRangeException` for invalid parameters, ensure that input values are validated before invocation to maintain configuration integrity.
