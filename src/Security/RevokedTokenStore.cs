#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Threading;

/// <summary>
/// In-memory store for revoked JWT IDs (jti claims).
/// Enables individual access token revocation without invalidating all tokens for a user.
/// Entries are self-expiring: once the original token's expiry passes, the entry
/// is pruned on the next access so the store stays bounded.
/// </summary>
public sealed class RevokedTokenStore
{
    // jti → original token expiry (UTC)
    private readonly ConcurrentDictionary<string, (DateTime tokenExpiresAt, object? value)> _revokedJtis =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly int _maxSize = 10000;
    private readonly Counter<long> _revokedTokenCount;
    private readonly Histogram<long> _revokedTokenLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevokedTokenStore"/> class.
    /// </summary>
    public RevokedTokenStore()
    {
        var meter = new Meter("DotnetAuthServer.Security.RevokedTokenStore");
        _revokedTokenCount = meter.CreateCounter<long>(
            name: "dotnet_auth_server.security.revoked_tokens.count",
            unit: "tokens",
            description: "Number of currently revoked tokens");
        _revokedTokenLifetime = meter.CreateHistogram<long>(
            name: "dotnet_auth_server.security.revoked_tokens.lifetime_seconds",
            unit: "s",
            description: "Lifetime of revoked tokens from issue to expiry");

        UpdateMetrics();
    }

    /// <summary>
    /// Adds a jti to the revocation list.
    /// <paramref name="tokenExpiresAt"/> is used to automatically expire the entry.
    /// </summary>
    /// <param name="jti">The JWT ID to revoke.</param>
    /// <param name="tokenExpiresAt">The UTC expiry time of the original token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> is null.</exception>
    public void Revoke(string jti, DateTime tokenExpiresAt)
    {
        ArgumentNullException.ThrowIfNull(jti);

        _semaphore.Wait();
        try
        {
            var lifetimeSeconds = (long)(tokenExpiresAt - DateTime.UtcNow).TotalSeconds;

            if (_revokedJtis.TryGetValue(jti, out var existing))
            {
                existing.tokenExpiresAt = tokenExpiresAt;
            }
            else
            {
                _revokedJtis[jti] = (tokenExpiresAt, null);
            }

            _revokedTokenLifetime.Record(lifetimeSeconds);
            UpdateMetrics();

            // Opportunistic cleanup
            if (_revokedJtis.Count > _maxSize)
            {
                PurgeExpired();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Returns true if the given jti has been explicitly revoked and has not yet expired.
    /// </summary>
    /// <param name="jti">The JWT ID to check.</param>
    /// <returns>True if the token is revoked and not expired; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> is null.</exception>
    public bool IsRevoked(string jti)
    {
        ArgumentNullException.ThrowIfNull(jti);

        _semaphore.Wait();
        try
        {
            if (!_revokedJtis.TryGetValue(jti, out var expiresAt))
            {
                return false;
            }

            // The underlying token has expired naturally — clean up the entry
            if (DateTime.UtcNow > expiresAt.tokenExpiresAt)
            {
                _revokedJtis.TryRemove(jti, out _);
                UpdateMetrics();
                return false;
            }

            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Removes all entries whose original tokens have already expired.
    /// Called periodically by <see cref="BackgroundWorkers.TokenCleanupWorker"/>.
    /// </summary>
    public void PurgeExpired()
    {
        var now = DateTime.UtcNow;
        var removedCount = 0;

        foreach (var key in _revokedJtis.Keys.ToList())
        {
            if (_revokedJtis.TryGetValue(key, out var exp) && now > exp.tokenExpiresAt)
            {
                if (_revokedJtis.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }
        }

        if (removedCount > 0)
        {
            UpdateMetrics();
        }
    }

    /// <summary>
    /// Removes all entries whose original tokens have already expired.
    /// </summary>
    /// <param name="now">The current UTC time to use for comparison.</param>
    public void RemoveExpired(DateTimeOffset now)
    {
        var removedCount = 0;

        foreach (var key in _revokedJtis.Keys.ToList())
        {
            if (_revokedJtis.TryGetValue(key, out var exp) && now > exp.tokenExpiresAt)
            {
                if (_revokedJtis.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }
        }

        if (removedCount > 0)
        {
            UpdateMetrics();
        }
    }

    /// <summary>
    /// Gets the current number of revoked tokens in the store.
    /// </summary>
    /// <returns>The count of revoked tokens.</returns>
    public int Count()
    {
        _semaphore.Wait();
        try
        {
            return _revokedJtis.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void UpdateMetrics()
    {
        _revokedTokenCount.Add(_revokedJtis.Count);
    }
}
