#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetAuthServer.Extensions;

/// <summary>
/// Extension methods for DateTime operations, particularly for JWT and OAuth2 token handling.
/// Ensures consistent Unix timestamp conversion and expiration checking throughout the system.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to Unix epoch time (seconds since January 1, 1970 UTC).
    /// Standard for JWT tokens and OAuth2 specifications.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert. If not UTC, it will be converted to UTC.</param>
    /// <returns>Unix epoch time in seconds.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dateTime"/> is <see langword="null"/>.</exception>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);

        return dateTime.Kind switch
        {
            DateTimeKind.Utc => (long)new DateTimeOffset(dateTime).ToUnixTimeSeconds(),
            _ => (long)new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds()
        };
    }

    /// <summary>
    /// Creates a DateTime from Unix epoch time (seconds since January 1, 1970 UTC).
    /// </summary>
    /// <param name="timestamp">Unix epoch time in seconds.</param>
    /// <returns>A DateTime representing the Unix timestamp in UTC.</returns>
    public static DateTime FromUnixTimestamp(long timestamp)
        => DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

    /// <summary>
    /// Checks if a token/grant has expired based on its expiration time.
    /// Includes a small buffer (5 seconds) to avoid race conditions.
    /// </summary>
    /// <param name="expiresAt">The expiration DateTime to check.</param>
    /// <returns><see langword="true"/> if the token/grant has expired; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expiresAt"/> is <see langword="null"/>.</exception>
    public static bool IsExpired(this DateTime expiresAt)
        => DateTime.UtcNow.AddSeconds(5) >= expiresAt;

    /// <summary>
    /// Checks if a token/grant is still valid (not yet expired).
    /// </summary>
    /// <param name="expiresAt">The expiration DateTime to check.</param>
    /// <returns><see langword="true"/> if the token/grant is still valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expiresAt"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this DateTime expiresAt)
        => !expiresAt.IsExpired();

    /// <summary>
    /// Calculates remaining lifetime in seconds until expiration.
    /// Returns 0 if already expired.
    /// </summary>
    /// <param name="expiresAt">The expiration DateTime to check.</param>
    /// <returns>Remaining lifetime in seconds, or 0 if already expired.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expiresAt"/> is <see langword="null"/>.</exception>
    public static long RemainingSeconds(this DateTime expiresAt)
    {
        var remaining = (long)(expiresAt - DateTime.UtcNow).TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Adds a configured lifetime (in seconds) to the current time.
    /// Ensures consistent expiration time calculation across the system.
    /// </summary>
    /// <param name="baseTime">The base DateTime to add lifetime to.</param>
    /// <param name="lifetimeSeconds">Lifetime in seconds to add. Negative values are treated as 0.</param>
    /// <returns>A new DateTime with the lifetime added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="baseTime"/> is <see langword="null"/>.</exception>
    public static DateTime AddLifetime(this DateTime baseTime, int lifetimeSeconds)
        => baseTime.AddSeconds(Math.Max(0, lifetimeSeconds));

    /// <summary>
    /// Formats a DateTime as an RFC 3339 string for OAuth2/OIDC responses.
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>RFC 3339 formatted string representation of the DateTime in UTC.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dateTime"/> is <see langword="null"/>.</exception>
    public static string ToRfc3339String(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);

        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime.ToString("o"),
            _ => dateTime.ToUniversalTime().ToString("o")
        };
    }
}
