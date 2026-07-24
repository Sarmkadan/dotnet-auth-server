#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================

namespace DotnetAuthServer.Security;

using System.Collections.Concurrent;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Tracks TOTP verification attempts per user to prevent brute-force attacks.
/// TOTP codes are only 6 digits, making them vulnerable to brute-force attacks
/// within the 30-second validity window. This rate limiter enforces stricter limits
/// than password attempts to protect against this vulnerability.
/// </summary>
public sealed class TotpRateLimiter : IDisposable
{
    private readonly AuthServerOptions _options;
    private readonly ILogger<TotpRateLimiter> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly Timer _cleanupTimer;

    // Key (user ID) → list of attempt timestamps (UTC)
    private readonly ConcurrentDictionary<string, List<DateTime>> _attempts =
        new(StringComparer.OrdinalIgnoreCase);

    public TotpRateLimiter(AuthServerOptions options, ILogger<TotpRateLimiter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;

        // Start cleanup timer to remove stale buckets
        // Clean up every 30 seconds (half of the typical TOTP window)
        _cleanupInterval = TimeSpan.FromSeconds(Math.Max(10, _options.TotpRateLimitWindowSeconds / 2.0));
        _cleanupTimer = new Timer(CleanupStaleBuckets, null, _cleanupInterval, _cleanupInterval);
    }

    /// <summary>
    /// Throws <see cref="AuthServerException"/> (429) if the user has exceeded the TOTP attempt limit.
    /// </summary>
    /// <exception cref="AuthServerException">Thrown when the user has exceeded the TOTP attempt limit.</exception>
    public void ThrowIfBlocked(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var threshold = _options.TotpAttemptsPerWindow;
        var windowSeconds = _options.TotpRateLimitWindowSeconds;
        var cutoff = DateTime.UtcNow.AddSeconds(-windowSeconds);

        if (CountRecent(userId, cutoff) >= threshold)
        {
            var retryAfter = GetRetryAfterSeconds(userId, cutoff, windowSeconds);
            _logger.LogWarning("TOTP verification blocked for user {UserId} due to too many attempts. Retry after {RetryAfter} seconds",
                userId, retryAfter);
            throw new AuthServerException(
                "too_many_requests",
                GetTotpRateLimitMessage(retryAfter),
                429);
        }
    }

    /// <summary>
    /// Records a successful TOTP verification attempt.
    /// </summary>
    public void RecordSuccess(string userId)
    {
        // Success attempts don't count against the limit, but we still track them
        // for monitoring purposes
        if (!string.IsNullOrWhiteSpace(userId))
        {
            Append(userId, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Records a failed TOTP verification attempt.
    /// </summary>
    public void RecordFailure(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            Append(userId, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Disposes the cleanup timer.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    // -------------------------------------------------------------------------
    // Private methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets a rate limit message with retry-after information.
    /// </summary>
    private string GetTotpRateLimitMessage(int retryAfterSeconds)
    {
        return $"Too many TOTP attempts. Please try again in {retryAfterSeconds} seconds.";
    }

    /// <summary>
    /// Cleans up stale buckets that haven't had activity in a long time.
    /// </summary>
    private void CleanupStaleBuckets(object? state)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-_options.TotpRateLimitWindowSeconds * 2);
            var keys = _attempts.Keys.ToList();

            foreach (var key in keys)
            {
                if (_attempts.TryGetValue(key, out var list))
                {
                    lock (list)
                    {
                        // Remove timestamps older than cutoff
                        list.RemoveAll(t => t < cutoff);

                        // If bucket is empty, remove it entirely
                        if (list.Count == 0)
                        {
                            _attempts.TryRemove(key, out _);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of stale TOTP attempt buckets");
        }
    }

    private int CountRecent(string key, DateTime cutoff)
    {
        if (!_attempts.TryGetValue(key, out var list)) return 0;
        lock (list) { return list.Count(t => t >= cutoff); }
    }

    private int GetRetryAfterSeconds(string key, DateTime cutoff, int windowSeconds)
    {
        if (!_attempts.TryGetValue(key, out var list)) return windowSeconds;
        lock (list)
        {
            var oldest = list.Where(t => t >= cutoff).OrderBy(t => t).FirstOrDefault();
            if (oldest == default) return windowSeconds;
            var expiresAt = oldest.AddSeconds(windowSeconds);
            return Math.Max(1, (int)(expiresAt - DateTime.UtcNow).TotalSeconds);
        }
    }

    private void Append(string key, DateTime timestamp)
    {
        var list = _attempts.GetOrAdd(key, _ => new List<DateTime>());
        lock (list)
        {
            list.Add(timestamp);
            // Prune entries that are now outside the sliding window
            var cutoff = timestamp.AddSeconds(-_options.TotpRateLimitWindowSeconds);
            list.RemoveAll(t => t < cutoff);
        }
    }
}