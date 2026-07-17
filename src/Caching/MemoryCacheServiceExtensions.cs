#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Caching;

using System.Collections.Concurrent;
using System.Globalization;

/// <summary>
/// Extension methods for <see cref="MemoryCacheService"/> providing additional
/// convenience methods for common caching scenarios.
/// </summary>
public static class MemoryCacheServiceExtensions
{
    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to get.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The value associated with the specified key, or null if the key is not present.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static Task<T?> TryGetValueAsync<T>(this MemoryCacheService cache, string key, CancellationToken cancellationToken = default)
    where T : class
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return cache.GetAsync<T>(key, cancellationToken);
    }

    /// <summary>
    /// Sets a value in the cache with the specified key and optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to set.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The key used to reference the item.</param>
    /// <param name="value">The value to store in the cache.</param>
    /// <param name="expiration">The expiration time for the cache entry. If null, the item does not expire.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static Task SetValueAsync<T>(
        this MemoryCacheService cache,
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    where T : class
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return cache.SetAsync(key, value, expiration, cancellationToken);
    }

    /// <summary>
    /// Sets multiple values in the cache with the specified keys and optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the values to set.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="items">A dictionary of key-value pairs to store in the cache.</param>
    /// <param name="expiration">The expiration time for the cache entries. If null, items do not expire.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="items"/> is null.</exception>
    public static Task SetMultipleAsync<T>(
        this MemoryCacheService cache,
        IReadOnlyDictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    where T : class
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(items);

        var tasks = items.Select(kvp => cache.SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Gets values for multiple keys from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the values to get.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="keys">The keys of the values to get.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A dictionary mapping keys to their cached values (null if not found).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="keys"/> is null.</exception>
    public static async Task<IReadOnlyDictionary<string, T?>> GetMultipleAsync<T>(
        this MemoryCacheService cache,
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    where T : class
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(keys);

        var keyList = keys.ToList();
        var result = new Dictionary<string, T?>();

        foreach (var key in keyList)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                result[key] = null;
                continue;
            }

            var value = await cache.GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Gets a value from the cache or computes it if not present, with support for multiple keys.
    /// </summary>
    /// <typeparam name="T">The type of the value to get or compute.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="keys">The keys to check for cached values.</param>
    /// <param name="factory">A factory function to compute the value if not in cache.</param>
    /// <param name="expiration">The expiration time for the cache entry. If null, the item does not expire.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The cached or computed value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/>, <paramref name="keys"/>, or <paramref name="factory"/> is null.</exception>
    public static async Task<T?> GetOrSetMultipleAsync<T>(
        this MemoryCacheService cache,
        IEnumerable<string> keys,
        Func<IReadOnlyList<string>, CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    where T : class
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(factory);

        var keyList = keys.ToList();

        // Check if any key has a cached value
        var cachedValues = await cache.GetMultipleAsync<T>(keyList, cancellationToken);
        if (cachedValues.Values.Any(v => v is not null))
        {
            // Return the first non-null value found
            return cachedValues.Values.FirstOrDefault(v => v is not null);
        }

        // All keys are missing, compute the value
        var value = await factory(keyList, cancellationToken);

        if (value is not null)
        {
            // Store with the first key
            await cache.SetAsync(keyList[0], value, expiration, cancellationToken);
        }

        return value;
    }

    /// <summary>
    /// Gets the expiration time for a cached value.
    /// </summary>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The key of the cached value.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The expiration time if the key exists and has an expiration, otherwise null.
    /// Note: This method returns null because the MemoryCacheService does not expose expiration times through its public API.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static async Task<DateTime?> GetExpirationAsync(
        this MemoryCacheService cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Get the cached entry to check expiration
        var cachedValue = await cache.GetAsync<object>(key, cancellationToken);
        if (cachedValue is null)
        {
            return null;
        }

        // Since MemoryCacheService doesn't expose expiration directly, we cannot determine the exact expiration time
        // This method returns null to indicate that expiration information is not available through the public API
        return null;
    }

    /// <summary>
    /// Removes all cache entries that match any of the specified patterns.
    /// </summary>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="patterns">The patterns to match against cache keys.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="patterns"/> is null.</exception>
    public static Task RemoveByPatternsAsync(
        this MemoryCacheService cache,
        IEnumerable<string> patterns,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(patterns);

        var tasks = patterns.Select(pattern => cache.RemoveByPatternAsync(pattern, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Gets all cache keys that match the specified pattern.
    /// </summary>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="pattern">The pattern to match against cache keys (supports * and ? wildcards).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of cache keys matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern"/> is null or whitespace.</exception>
    public static async Task<IReadOnlyList<string>> GetKeysByPatternAsync(
        this MemoryCacheService cache,
        string pattern,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        // Use reflection to access the internal _cache field
        var field = typeof(MemoryCacheService).GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field?.GetValue(cache) is ConcurrentDictionary<string, object> internalCache)
        {
            // Manually convert wildcard pattern to regex
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var regex = new System.Text.RegularExpressions.Regex(
                regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var matchingKeys = internalCache.Keys.Where(k => regex.IsMatch(k)).ToList();
            return matchingKeys.AsReadOnly();
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets statistics about the cache contents.
    /// </summary>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A cache statistics object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is null.</exception>
    public static async Task<CacheStatistics> GetStatisticsAsync(
        this MemoryCacheService cache,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        // Use reflection to access internal fields
        var cacheField = typeof(MemoryCacheService).GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var locksField = typeof(MemoryCacheService).GetField("_locks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var cacheCount = cacheField?.GetValue(cache) is ConcurrentDictionary<string, object> cacheDict ? cacheDict.Count : 0;
        var locksCount = locksField?.GetValue(cache) is ConcurrentDictionary<string, SemaphoreSlim> locksDict ? locksDict.Count : 0;

        // Calculate total size of all cached values more accurately
        var totalSizeBytes = 0L;
        if (cacheField?.GetValue(cache) is ConcurrentDictionary<string, object> internalCache)
        {
            foreach (var entry in internalCache.Values)
            {
                if (entry is not null)
                {
                    // Use MemorySizeOf for more accurate size calculation
                    // For primitive types and common objects, use approximate size
                    totalSizeBytes += GetApproximateSize(entry);
                }
            }
        }

        return new CacheStatistics
        {
            EntryCount = cacheCount,
            LockCount = locksCount,
            TotalSizeBytes = totalSizeBytes,
            ApproximateSize = FormatSize(totalSizeBytes)
        };
    }

    /// <summary>
    /// Gets an approximate size of an object in bytes.
    /// </summary>
    /// <param name="obj">The object to measure.</param>
    /// <returns>Approximate size in bytes.</returns>
    private static long GetApproximateSize(object? obj)
    {
        if (obj is null)
        {
            return 0;
        }

        // Handle common types
        return obj switch
        {
            string s => System.Text.Encoding.Unicode.GetByteCount(s),
            byte[] b => b.Length,
            Array a => a.Length * (a.GetType().GetElementType()?.IsValueType == true ? System.Runtime.InteropServices.Marshal.SizeOf(a.GetType().GetElementType()) : IntPtr.Size),
            _ => IntPtr.Size // Default size for reference types
        };
    }

    /// <summary>
    /// Formats a byte size into a human-readable string.
    /// </summary>
    /// <param name="bytes">The number of bytes to format.</param>
    /// <returns>Human-readable size string.</returns>
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        var len = (double)bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", len, sizes[order]);
    }
}

/// <summary>
/// Represents statistics about the cache contents.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets the number of cache entries.
    /// </summary>
    public int EntryCount { get; init; }

    /// <summary>
    /// Gets the number of active locks.
    /// </summary>
    public int LockCount { get; init; }

    /// <summary>
    /// Gets the total size of all cached values in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Gets a human-readable representation of the cache size.
    /// </summary>
    public string ApproximateSize { get; init; } = string.Empty;
}