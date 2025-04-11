#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.Collections.Concurrent;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Tracks failed login attempts per username and per IP address using a sliding-window
/// counter and temporarily blocks callers that exceed the configured threshold.
/// This mitigates brute-force and credential-stuffing attacks on the password grant endpoint.
/// </summary>
public sealed class LoginRateLimiter
{
    private readonly AuthServerOptions _options;
    private readonly ILogger<LoginRateLimiter> _logger;

    // Key (username or IP) → ordered list of failed attempt timestamps (UTC)
    private readonly ConcurrentDictionary<string, List<DateTime>> _attempts =
        new(StringComparer.OrdinalIgnoreCase);

    public LoginRateLimiter(AuthServerOptions options, ILogger<LoginRateLimiter> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Throws <see cref="AuthServerException"/> (429) if the username or IP is currently
    /// blocked due to too many recent failures.
    /// </summary>
    public void ThrowIfBlocked(string? username, string? ipAddress)
    {
        var threshold = _options.FailedLoginAttemptThreshold;
        var windowMinutes = _options.AccountLockoutDurationMinutes;
        var cutoff = DateTime.UtcNow.AddMinutes(-windowMinutes);

        if (!string.IsNullOrWhiteSpace(username) && CountRecent(username, cutoff) >= threshold)
        {
            _logger.LogWarning("Login blocked for username {Username} due to repeated failures", username);
            throw new AuthServerException(
                "too_many_requests",
                $"Too many failed login attempts. Try again in {GetRetryAfterSeconds(username, cutoff, windowMinutes)} seconds.",
                429);
        }

        if (!string.IsNullOrWhiteSpace(ipAddress) && CountRecent(ipAddress, cutoff) >= threshold)
        {
            _logger.LogWarning("Login blocked for IP {IpAddress} due to repeated failures", ipAddress);
            throw new AuthServerException(
                "too_many_requests",
                $"Too many failed login attempts. Try again in {GetRetryAfterSeconds(ipAddress, cutoff, windowMinutes)} seconds.",
                429);
        }
    }

    /// <summary>
    /// Records a failed login attempt for the username and/or IP.
    /// </summary>
    public void RecordFailure(string? username, string? ipAddress)
    {
        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(username))
            Append(username, now);
        if (!string.IsNullOrWhiteSpace(ipAddress))
            Append(ipAddress, now);
    }

    /// <summary>
    /// Clears the failed-attempt counter for a username after a successful login.
    /// </summary>
    public void RecordSuccess(string? username)
    {
        if (!string.IsNullOrWhiteSpace(username))
            _attempts.TryRemove(username, out _);
    }

    // -------------------------------------------------------------------------
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
