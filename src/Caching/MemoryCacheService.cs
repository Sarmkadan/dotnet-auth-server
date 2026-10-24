#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Caching;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Linq; // Added for LINQ operations

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary.
/// Suitable for single-server deployments. For multi-server scenarios,
/// consider using a distributed cache like Redis via dependency injection.
/// Features automatic expiration checking and thread-safe operations.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Gets a cached value by key, returning null if it is missing, expired, or the key is blank.
    /// </summary>
    /// <typeparam name="T">Reference type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token (not observed; operation is synchronous).</param>
    /// <returns>The cached value, or null.</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult<T?>(null);

        if (_cache.TryGetValue(key, out var entry))
        {
            // Check if expired
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt <= DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult<T?>(null);
            }

            return Task.FromResult(entry.Value as T);
        }

        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Stores a value under the given key, optionally expiring after a time span.
    /// </summary>
    /// <typeparam name="T">Reference type of the value.</typeparam>
    /// <param name="key">Cache key. Blank keys are ignored.</param>
    /// <param name="value">Value to store.</param>
    /// <param name="expiration">Lifetime of the entry; null means no expiration.</param>
    /// <param name="cancellationToken">Cancellation token (not observed; operation is synchronous).</param>
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.CompletedTask;

        var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;
        var entry = new CacheEntry { Value = value, ExpiresAt = expiresAt };

        _cache.AddOrUpdate(key, entry, (_, __) => entry);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a single entry and its associated lock by key. Blank keys are ignored.
    /// </summary>
    /// <param name="key">Cache key to remove.</param>
    /// <param name="cancellationToken">Cancellation token (not observed; operation is synchronous).</param>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.CompletedTask;

        _cache.TryRemove(key, out _);
        _locks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all entries whose keys match a wildcard pattern (<c>*</c> and <c>?</c>), case-insensitively.
    /// An invalid pattern is silently ignored.
    /// </summary>
    /// <param name="pattern">Wildcard pattern to match keys against.</param>
    /// <param name="cancellationToken">Cancellation token (not observed; operation is synchronous).</param>
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return Task.CompletedTask;

        try
        {
            var regex = new Regex(WildcardToRegex(pattern), RegexOptions.IgnoreCase);
            var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
                _locks.TryRemove(key, out _);
            }
        }
        catch
        {
            // Invalid pattern, ignore
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all entries and locks from the cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token (not observed; operation is synchronous).</param>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        _locks.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the cached value for a key, or computes it with the factory, caches it, and returns it.
    /// Concurrent callers for the same key are serialized so the factory runs at most once per miss.
    /// </summary>
    /// <typeparam name="T">Reference type of the value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Delegate that produces the value on a cache miss; null results are not cached.</param>
    /// <param name="expiration">Lifetime of a newly cached entry; null means no expiration.</param>
    /// <param name="cancellationToken">Cancellation token passed to the factory and lock wait.</param>
    /// <returns>The cached or newly computed value.</returns>
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // Try to get from cache first
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        // Get or create a lock for this key to prevent thundering herd
        var @lock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await @lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check pattern: another thread may have populated the cache
            var cachedAgain = await GetAsync<T>(key, cancellationToken);
            if (cachedAgain is not null)
                return cachedAgain;

            // Call factory to compute value
            var value = await factory(cancellationToken);
            if (value is not null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
            }

            return value;
        }
        finally
        {
            @lock.Release();
        }
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }

    private class CacheEntry
    {
        public object? Value { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // Added ToString override for concise state representation
    /// <summary>
    /// Returns a string listing all cache entries with their values and expiration times.
    /// </summary>
    /// <returns>A string representation of the cache state.</returns>
    public override string ToString()
    {
        var entries = string.Join(
            ", ",
            _cache.Select(kv => $"{kv.Key}: {{ Value = {kv.Value.Value}, ExpiresAt = {kv.Value.ExpiresAt} }}"));
        return $"MemoryCacheService {{ {entries} }}";
    }
}
