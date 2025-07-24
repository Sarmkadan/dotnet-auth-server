#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using DotnetAuthServer.Configuration;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Extension methods for <see cref="CacheOptions"/> that provide convenient
/// ways to configure and work with cache settings.
/// </summary>
public static class CacheOptionsExtensions
{
    /// <summary>
    /// Sets the cache backend to Redis with the specified connection string.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static CacheOptions UseRedis(this CacheOptions options, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        options.Backend = "Redis";
        options.ConnectionString = connectionString;
        return options;
    }

    /// <summary>
    /// Sets the cache backend to Memory.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static CacheOptions UseMemory(this CacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Backend = "Memory";
        options.ConnectionString = null;
        return options;
    }

    /// <summary>
    /// Sets the default expiration time for cached entries.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <param name="expirationSeconds">Expiration time in seconds.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expirationSeconds"/> is less than 1.</exception>
    public static CacheOptions SetDefaultExpiration(this CacheOptions options, int expirationSeconds)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (expirationSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(expirationSeconds), expirationSeconds, "Expiration must be at least 1 second.");
        }

        options.DefaultExpirationSeconds = expirationSeconds;
        return options;
    }

    /// <summary>
    /// Sets the maximum number of entries allowed in the cache.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <param name="maxEntries">Maximum number of entries.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxEntries"/> is less than 1.</exception>
    public static CacheOptions SetMaxEntries(this CacheOptions options, int maxEntries)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (maxEntries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Max entries must be at least 1.");
        }

        options.MaxEntries = maxEntries;
        return options;
    }

    /// <summary>
    /// Sets the expiration scan interval for memory cache.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <param name="intervalSeconds">Scan interval in seconds.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="intervalSeconds"/> is less than 1.</exception>
    public static CacheOptions SetExpirationScanInterval(this CacheOptions options, int intervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (intervalSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds), intervalSeconds, "Interval must be at least 1 second.");
        }

        options.ExpirationScanIntervalSeconds = intervalSeconds;
        return options;
    }

    /// <summary>
    /// Gets the effective expiration time for a specific cache item type.
    /// </summary>
    /// <param name="options">The cache options.</param>
    /// <param name="itemType">Type of cache item to get expiration for.</param>
    /// <returns>Expiration time in seconds for the specified item type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static int GetExpirationSeconds(this CacheOptions options, CacheItemType itemType)
    {
        ArgumentNullException.ThrowIfNull(options);

        return itemType switch
        {
            CacheItemType.Client => options.ItemExpirations.ClientSeconds,
            CacheItemType.User => options.ItemExpirations.UserSeconds,
            CacheItemType.Scope => options.ItemExpirations.ScopeSeconds,
            CacheItemType.Grant => options.ItemExpirations.GrantSeconds,
            CacheItemType.Jwks => options.ItemExpirations.JwksSeconds,
            _ => options.DefaultExpirationSeconds
        };
    }

    /// <summary>
    /// Sets the expiration time for a specific cache item type.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <param name="itemType">Type of cache item to configure.</param>
    /// <param name="seconds">Expiration time in seconds.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is less than 1 or <paramref name="itemType"/> is unknown.</exception>
    public static CacheOptions SetExpiration(this CacheOptions options, CacheItemType itemType, int seconds)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (seconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Expiration must be at least 1 second.");
        }

        switch (itemType)
        {
            case CacheItemType.Client:
                options.ItemExpirations.ClientSeconds = seconds;
                break;
            case CacheItemType.User:
                options.ItemExpirations.UserSeconds = seconds;
                break;
            case CacheItemType.Scope:
                options.ItemExpirations.ScopeSeconds = seconds;
                break;
            case CacheItemType.Grant:
                options.ItemExpirations.GrantSeconds = seconds;
                break;
            case CacheItemType.Jwks:
                options.ItemExpirations.JwksSeconds = seconds;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(itemType), itemType, "Unknown cache item type.");
        }

        return options;
    }

    /// <summary>
    /// Disables caching by setting Enabled to false.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static CacheOptions Disable(this CacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Enabled = false;
        return options;
    }

    /// <summary>
    /// Enables caching by setting Enabled to true.
    /// </summary>
    /// <param name="options">The cache options to configure.</param>
    /// <returns>The configured <see cref="CacheOptions"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static CacheOptions Enable(this CacheOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Enabled = true;
        return options;
    }
}

/// <summary>
/// Types of cacheable items for expiration configuration.
/// </summary>
public enum CacheItemType
{
    /// <summary>Client information.</summary>
    Client,

    /// <summary>User information.</summary>
    User,

    /// <summary>Scope definitions.</summary>
    Scope,

    /// <summary>Authorization grant information.</summary>
    Grant,

    /// <summary>JWKS (public keys).</summary>
    Jwks
}