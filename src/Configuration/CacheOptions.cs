// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Configuration options for the caching layer.
/// Controls cache expiration, size limits, and backend selection.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Type of cache backend to use (Memory, Redis, etc.)
    /// </summary>
    public string Backend { get; set; } = "Memory";

    /// <summary>
    /// Default expiration time for cached entries (seconds).
    /// Individual entries can override this.
    /// </summary>
    public int DefaultExpirationSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Maximum number of entries in memory cache.
    /// Prevents unbounded memory growth.
    /// </summary>
    public int MaxEntries { get; set; } = 10000;

    /// <summary>
    /// How often to scan for and remove expired entries (seconds).
    /// Only applicable to memory cache.
    /// </summary>
    public int ExpirationScanIntervalSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Connection string for distributed cache (Redis, etc.)
    /// Only used if Backend is not "Memory".
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Cache expiration times for specific item types.
    /// </summary>
    public CacheItemExpirations ItemExpirations { get; set; } = new();
}

/// <summary>
/// Cache expiration times for different types of cached data.
/// </summary>
public class CacheItemExpirations
{
    /// <summary>
    /// How long to cache client information (seconds).
    /// </summary>
    public int ClientSeconds { get; set; } = 3600;

    /// <summary>
    /// How long to cache user information (seconds).
    /// </summary>
    public int UserSeconds { get; set; } = 1800;

    /// <summary>
    /// How long to cache scope definitions (seconds).
    /// </summary>
    public int ScopeSeconds { get; set; } = 7200;

    /// <summary>
    /// How long to cache authorization grant information (seconds).
    /// </summary>
    public int GrantSeconds { get; set; } = 300;

    /// <summary>
    /// How long to cache JWKS (public keys) (seconds).
    /// </summary>
    public int JwksSeconds { get; set; } = 86400; // 24 hours
}
