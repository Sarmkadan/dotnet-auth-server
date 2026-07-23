#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.Collections.Concurrent;
using System.Diagnostics;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Tracks failed login attempts per username and per IP address using a sliding-window
/// counter and temporarily blocks callers that exceed the configured threshold.
/// This mitigates brute-force and credential-stuffing attacks on the password grant endpoint.
/// </summary>
public sealed class LoginRateLimiter : IDisposable
{
    private readonly AuthServerOptions _options;
    private readonly ILogger<LoginRateLimiter> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly Timer _cleanupTimer;

    // Key (username or IP) → ordered list of failed attempt timestamps (UTC)
    // Uses StringComparer.OrdinalIgnoreCase to prevent case-based key proliferation
    private readonly ConcurrentDictionary<string, List<DateTime>> _attempts =
        new(StringComparer.OrdinalIgnoreCase);

    // Global failure tracking for distributed attacks
    private readonly ConcurrentDictionary<string, int> _globalAttempts = new();
    private readonly object _globalLock = new();

    // Global circuit breaker state
    private int _globalFailureCount;
    private DateTime _lastGlobalReset = DateTime.UtcNow;

    public LoginRateLimiter(AuthServerOptions options, ILogger<LoginRateLimiter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;

        // Start cleanup timer to remove stale buckets
        _cleanupInterval = TimeSpan.FromMinutes(Math.Max(1, _options.AccountLockoutDurationMinutes / 2.0));
        _cleanupTimer = new Timer(CleanupStaleBuckets, null, _cleanupInterval, _cleanupInterval);
    }

    /// <summary>
    /// Throws <see cref="AuthServerException"/> (429) if the username or IP is currently
    /// blocked due to too many recent failures.
    /// </summary>
    /// <exception cref="AuthServerException">Thrown when the username or IP address is blocked.</exception>
    public void ThrowIfBlocked(string? username, string? ipAddress)
    {
        // Check global circuit breaker first - protects against distributed attacks
        CheckGlobalCircuitBreaker();

        var threshold = _options.FailedLoginAttemptThreshold;
        var windowMinutes = _options.AccountLockoutDurationMinutes;
        var cutoff = DateTime.UtcNow.AddMinutes(-windowMinutes);

        // Check username block (use indistinguishable error message)
        if (!string.IsNullOrWhiteSpace(username) && CountRecent(username, cutoff) >= threshold)
        {
            _logger.LogWarning("Login blocked for username {Username} due to repeated failures", username);
            throw new AuthServerException(
                "too_many_requests",
                GetGenericTooManyAttemptsMessage(),
                429);
        }

        // Check IP address block
        if (!string.IsNullOrWhiteSpace(ipAddress) && CountRecent(ipAddress, cutoff) >= threshold)
        {
            _logger.LogWarning("Login blocked for IP {IpAddress} due to repeated failures", ipAddress);
            throw new AuthServerException(
                "too_many_requests",
                GetGenericTooManyAttemptsMessage(),
                429);
        }
    }

    /// <summary>
    /// Records a failed login attempt for the username and/or IP.
    /// </summary>
    public void RecordFailure(string? username, string? ipAddress)
    {
        var now = DateTime.UtcNow;

        // Update global failure tracking
        UpdateGlobalAttempts(username, ipAddress);

        // Record individual attempts
        if (!string.IsNullOrWhiteSpace(username))
        {
            Append(username, now);
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            Append(ipAddress, now);
        }
    }

    /// <summary>
    /// Clears the failed-attempt counter for a username after a successful login.
    /// </summary>
    public void RecordSuccess(string? username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            _attempts.TryRemove(username, out _);
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
    /// <summary>
    /// Gets a generic error message that doesn't reveal whether username exists.
    /// </summary>
    private string GetGenericTooManyAttemptsMessage()
    {
        return "Too many failed login attempts. Please try again later.";
    }

    /// <summary>
    /// Checks the global circuit breaker and raises friction if aggregate failure rate is high.
    /// </summary>
    private void CheckGlobalCircuitBreaker()
    {
        const int globalThresholdMultiplier = 10;
        var globalThreshold = _options.FailedLoginAttemptThreshold * globalThresholdMultiplier;
        var globalWindowMinutes = _options.AccountLockoutDurationMinutes;
        var globalCutoff = DateTime.UtcNow.AddMinutes(-globalWindowMinutes);

        lock (_globalLock)
        {
            // Reset counter if window has passed
            if (DateTime.UtcNow - _lastGlobalReset > TimeSpan.FromMinutes(globalWindowMinutes))
            {
                _globalFailureCount = 0;
                _lastGlobalReset = DateTime.UtcNow;
                return;
            }

            // Check if global threshold exceeded
            if (_globalFailureCount >= globalThreshold)
            {
                _logger.LogWarning("Global login rate limit triggered. Current failures: {Count} in last {Minutes} minutes",
                    _globalFailureCount, globalWindowMinutes);
                throw new AuthServerException(
                    "too_many_requests",
                    GetGenericTooManyAttemptsMessage(),
                    429);
            }
        }
    }

    /// <summary>
    /// Updates global attempt tracking for distributed attack detection.
    /// </summary>
    private void UpdateGlobalAttempts(string? username, string? ipAddress)
    {
        lock (_globalLock)
        {
            // Track by username if available
            if (!string.IsNullOrWhiteSpace(username))
            {
                var key = $"user:{username}";
                if (_globalAttempts.TryGetValue(key, out var count))
                {
                    _globalAttempts[key] = count + 1;
                }
                else
                {
                    _globalAttempts[key] = 1;
                }
            }

            // Track by IP if available
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                var key = $"ip:{ipAddress}";
                if (_globalAttempts.TryGetValue(key, out var count))
                {
                    _globalAttempts[key] = count + 1;
                }
                else
                {
                    _globalAttempts[key] = 1;
                }
            }

            _globalFailureCount++;
        }

        // Periodically clean up old global attempts
        if (_globalFailureCount % 100 == 0)
        {
            CleanupOldGlobalAttempts();
        }
    }

    /// <summary>
    /// Cleans up old global attempt tracking entries.
    /// </summary>
    private void CleanupOldGlobalAttempts()
    {
        lock (_globalLock)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-_options.AccountLockoutDurationMinutes);
            var keysToRemove = _globalAttempts.Keys
                .Where(key => key.StartsWith("user:", StringComparison.Ordinal) || key.StartsWith("ip:", StringComparison.Ordinal))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _globalAttempts.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Cleans up stale buckets that haven't had activity in a long time.
    /// </summary>
    private void CleanupStaleBuckets(object? state)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-_options.AccountLockoutDurationMinutes * 2);
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

            // Also clean up global attempts periodically
            CleanupOldGlobalAttempts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of stale login attempt buckets");
        }
    }

    private int CountRecent(string key, DateTime cutoff)
    {
        if (!_attempts.TryGetValue(key, out var list)) return 0;
        lock (list) { return list.Count(t => t >= cutoff); }
    }

    private int GetRetryAfterSeconds(string key, DateTime cutoff, int windowMinutes)
    {
        if (!_attempts.TryGetValue(key, out var list)) return windowMinutes * 60;
        lock (list)
        {
            var oldest = list.Where(t => t >= cutoff).OrderBy(t => t).FirstOrDefault();
            if (oldest == default) return windowMinutes * 60;
            var expiresAt = oldest.AddMinutes(windowMinutes);
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
            var cutoff = timestamp.AddMinutes(-_options.AccountLockoutDurationMinutes);
            list.RemoveAll(t => t < cutoff);
        }
    }
}