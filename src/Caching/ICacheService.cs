// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Caching;

/// <summary>
/// Abstraction for caching layer to enable swapping implementations.
/// Supports both simple memory caching and distributed cache backends.
/// Essential for performance when validating frequently-accessed scopes,
/// clients, and authorization codes.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key.
    /// Returns null if key does not exist or has expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores a value in cache with optional expiration time.
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// Idempotent - does not throw if key does not exist.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values matching a pattern.
    /// Useful for invalidating related cache entries.
    /// Implementation depends on backend capabilities.
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached values.
    /// Use sparingly - should only be called on graceful shutdown or explicit flush.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from cache or executes a factory function if not cached.
    /// Atomic operation - factory is only called once even in concurrent scenarios.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class;
}
