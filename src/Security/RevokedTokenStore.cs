#nullable enable

// =============================================================================
// Author: Vladyslav Zaiatz | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

/// <summary>
/// In‑memory store for revoked JWT IDs (jti claims) and refresh‑token families.
/// Enables individual access‑token revocation without invalidating all tokens for a user,
/// and supports refresh‑token rotation with reuse detection as defined in OAuth 2.1.
/// Entries are self‑expiring: once the original token's expiry passes, the entry
/// is pruned on the next access so the store stays bounded.
/// </summary>
public sealed class RevokedTokenStore
{
    // jti → original token expiry (UTC) for revoked tokens
    private readonly ConcurrentDictionary<string, (DateTime tokenExpiresAt, object? value)> _revokedJtis =
        new(StringComparer.OrdinalIgnoreCase);

    // All known tokens (including active ones) with their family identifier
    private readonly ConcurrentDictionary<string, (DateTime expiresAt, string familyId)> _tokenInfo =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _compoundOperationLock = new();
    private readonly int _maxSize = 10_000;
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

    #region Registration & Revocation

    /// <summary>
    /// Registers a newly‑issued token (access or refresh) with its family identifier.
    /// The token is not revoked; it is merely tracked so that a family‑wide revocation
    /// can be performed later if a reused refresh token is detected.
    /// </summary>
    /// <param name="jti">The JWT ID of the token.</param>
    /// <param name="expiresAt">The UTC expiry time of the token.</param>
    /// <param name="familyId">The identifier that groups related refresh tokens.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> or <paramref name="familyId"/> is <c>null</c>.</exception>
    public void RegisterToken(string jti, DateTime expiresAt, string familyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(jti);
        ArgumentException.ThrowIfNullOrEmpty(familyId);

        _tokenInfo[jti] = (expiresAt, familyId);
    }

    /// <summary>
    /// Revokes a token identified by its <c>jti</c>. The token is added to the revoked set
    /// and its family identifier is retained for possible family‑wide revocation.
    /// </summary>
    /// <param name="jti">The JWT ID to revoke.</param>
    /// <param name="tokenExpiresAt">The UTC expiry time of the original token.</param>
    /// <param name="familyId">The family identifier of the token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> or <paramref name="familyId"/> is <c>null</c>.</exception>
    public void Revoke(string jti, DateTime tokenExpiresAt, string familyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(jti);
        ArgumentException.ThrowIfNullOrEmpty(familyId);

        // Ensure the token is known; if not, register it so family information is retained.
        _tokenInfo.TryAdd(jti, (tokenExpiresAt, familyId));

        var lifetimeSeconds = (long)(tokenExpiresAt - DateTime.UtcNow).TotalSeconds;
        _revokedJtis[jti] = (tokenExpiresAt, null);
        _revokedTokenLifetime.Record(lifetimeSeconds);
        UpdateMetrics();

        if (_revokedJtis.Count > _maxSize)
        {
            // Coordinate the count recheck and full sweep so concurrent threshold crossings
            // do not start overlapping size-bound pruning passes.
            lock (_compoundOperationLock)
            {
                if (_revokedJtis.Count > _maxSize)
                {
                    PurgeExpired();
                }
            }
        }
    }

    /// <summary>
    /// Revokes a token without specifying a family identifier.
    /// This overload preserves existing behaviour and simply forwards to the
    /// three‑parameter overload using an empty family identifier.
    /// </summary>
    /// <param name="jti">The JWT ID to revoke.</param>
    /// <param name="tokenExpiresAt">The UTC expiry time of the original token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> is <c>null</c>.</exception>
    public void Revoke(string jti, DateTime tokenExpiresAt)
        => Revoke(jti, tokenExpiresAt, string.Empty);

    /// <summary>
    /// Revokes every token that belongs to the specified family.
    /// This is used when a refresh token is presented that has already been revoked,
    /// indicating a possible theft; the whole family is then invalidated.
    /// </summary>
    /// <param name="familyId">The family identifier to revoke.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="familyId"/> is <c>null</c>.</exception>
    public void RevokeFamily(string familyId)
    {
        ArgumentNullException.ThrowIfNull(familyId);

        // Keep the family snapshot and its revocation sweep together so other
        // family-wide sweeps cannot interleave and double-record revocations.
        lock (_compoundOperationLock)
        {
            var now = DateTime.UtcNow;
            var tokensInFamily = _tokenInfo
                .Where(kvp => kvp.Value.familyId == familyId)
                .Select(kvp => (jti: kvp.Key, expiresAt: kvp.Value.expiresAt))
                .ToList();

            foreach (var (jti, expiresAt) in tokensInFamily)
            {
                // If already revoked, skip; otherwise add to revoked set.
                if (!_revokedJtis.ContainsKey(jti))
                {
                    var lifetimeSeconds = (long)(expiresAt - now).TotalSeconds;
                    _revokedJtis[jti] = (expiresAt, null);
                    _revokedTokenLifetime.Record(lifetimeSeconds);
                }
            }

            UpdateMetrics();
        }
    }

    /// <summary>
    /// Determines whether a token is revoked and, if so, returns its family identifier.
    /// </summary>
    /// <param name="jti">The JWT ID to check.</param>
    /// <param name="familyId">
    /// When the method returns <c>true</c>, receives the family identifier associated
    /// with the revoked token; otherwise receives <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the token is revoked and not yet expired; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> is <c>null</c>.</exception>
    public bool IsRevoked(string jti, out string? familyId)
    {
        ArgumentNullException.ThrowIfNull(jti);

        if (!_revokedJtis.TryGetValue(jti, out var expiresAt))
        {
            familyId = null;
            return false;
        }

        // The underlying token has expired naturally — clean up the entry
        if (DateTime.UtcNow > expiresAt.tokenExpiresAt)
        {
            _revokedJtis.TryRemove(jti, out _);
            UpdateMetrics();
            familyId = null;
            return false;
        }

        // Retrieve family identifier from the token‑info dictionary if available.
        familyId = _tokenInfo.TryGetValue(jti, out var info) ? info.familyId : null;
        return true;
    }

    /// <summary>
    /// Determines whether a token is revoked.
    /// This overload preserves existing callers that do not need the family identifier.
    /// </summary>
    /// <param name="jti">The JWT ID to check.</param>
    /// <returns><c>true</c> if the token is revoked and not yet expired; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jti"/> is <c>null</c>.</exception>
    public bool IsRevoked(string jti) => IsRevoked(jti, out _);

    #endregion

    #region Cleanup & Metrics

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

        // Also clean up the token‑info dictionary.
        foreach (var key in _tokenInfo.Keys.ToList())
        {
            if (_tokenInfo.TryGetValue(key, out var info) && now > info.expiresAt)
            {
                if (_tokenInfo.TryRemove(key, out _))
                {
                    // No metric for active tokens; just remove.
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

        foreach (var key in _tokenInfo.Keys.ToList())
        {
            if (_tokenInfo.TryGetValue(key, out var info) && now > info.expiresAt)
            {
                if (_tokenInfo.TryRemove(key, out _))
                {
                    // No metric for active tokens.
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
        return _revokedJtis.Count;
    }

    private void UpdateMetrics()
    {
        _revokedTokenCount.Add(_revokedJtis.Count);
    }

    #endregion
}
