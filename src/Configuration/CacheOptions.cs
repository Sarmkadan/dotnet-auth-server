#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Configuration options for the caching layer.
/// Controls cache expiration, size limits, and backend selection.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    [Required]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Type of cache backend to use (Memory, Redis, etc.)
    /// </summary>
    [Required]
    public string Backend { get; set; } = "Memory";

    /// <summary>
    /// Default expiration time for cached entries (seconds).
    /// Individual entries can override this.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int DefaultExpirationSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Maximum number of entries in memory cache.
    /// Prevents unbounded memory growth.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxEntries { get; set; } = 10000;

    /// <summary>
    /// How often to scan for and remove expired entries (seconds).
    /// Only applicable to memory cache.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExpirationScanIntervalSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Connection string for distributed cache (Redis, etc.)
    /// Only used if Backend is not "Memory".
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Cache expiration times for specific item types.
    /// </summary>
    [Required]
    public CacheItemExpirations ItemExpirations { get; set; } = new();
}

/// <summary>
/// Cache expiration times for different types of cached data.
/// </summary>
public sealed class CacheItemExpirations
{
    /// <summary>
    /// How long to cache client information (seconds).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ClientSeconds { get; set; } = 3600;

    /// <summary>
    /// How long to cache user information (seconds).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int UserSeconds { get; set; } = 1800;

    /// <summary>
    /// How long to cache scope definitions (seconds).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ScopeSeconds { get; set; } = 7200;

    /// <summary>
    /// How long to cache authorization grant information (seconds).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int GrantSeconds { get; set; } = 300;

    /// <summary>
    /// How long to cache JWKS (public keys) (seconds).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int JwksSeconds { get; set; } = 86400; // 24 hours
}
