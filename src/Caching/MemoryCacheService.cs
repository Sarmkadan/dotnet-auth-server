// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Caching;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary.
/// Suitable for single-server deployments. For multi-server scenarios,
/// consider using a distributed cache like Redis via dependency injection.
/// Features automatic expiration checking and thread-safe operations.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

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

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.CompletedTask;

        _cache.TryRemove(key, out _);
        _locks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

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

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        _locks.Clear();
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        // Try to get from cache first
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
            return cached;

        // Get or create a lock for this key to prevent thundering herd
        var @lock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await @lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check pattern: another thread may have populated the cache
            var cachedAgain = await GetAsync<T>(key, cancellationToken);
            if (cachedAgain != null)
                return cachedAgain;

            // Call factory to compute value
            var value = await factory(cancellationToken);
            if (value != null)
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
}
