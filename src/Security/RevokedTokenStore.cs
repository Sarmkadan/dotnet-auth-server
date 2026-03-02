#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.Collections.Concurrent;

/// <summary>
/// In-memory store for revoked JWT IDs (jti claims).
/// Enables individual access token revocation without invalidating all tokens for a user.
/// Entries are self-expiring: once the original token's expiry passes, the entry
/// is pruned on the next access so the store stays bounded.
/// </summary>
public sealed class RevokedTokenStore
{
    // jti → original token expiry (UTC)
    private readonly ConcurrentDictionary<string, DateTime> _revokedJtis =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a jti to the revocation list.
    /// <paramref name="tokenExpiresAt"/> is used to automatically expire the entry.
    /// </summary>
    public void Revoke(string jti, DateTime tokenExpiresAt)
        => _revokedJtis[jti] = tokenExpiresAt;

    /// <summary>
    /// Returns true if the given jti has been explicitly revoked and has not yet expired.
    /// </summary>
    public bool IsRevoked(string jti)
    {
        if (!_revokedJtis.TryGetValue(jti, out var expiresAt))
            return false;

        // The underlying token has expired naturally — clean up the entry
        if (DateTime.UtcNow > expiresAt)
        {
            _revokedJtis.TryRemove(jti, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes all entries whose original tokens have already expired.
    /// Called periodically by <see cref="BackgroundWorkers.TokenCleanupWorker"/>.
    /// </summary>
    public void PurgeExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _revokedJtis.Keys.ToList())
        {
            if (_revokedJtis.TryGetValue(key, out var exp) && now > exp)
                _revokedJtis.TryRemove(key, out _);
        }
    }
}
