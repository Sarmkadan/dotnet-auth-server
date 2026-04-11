// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
            dateTime = dateTime.ToUniversalTime();

        return (long)new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Creates a DateTime from Unix epoch time (seconds since January 1, 1970 UTC).
    /// </summary>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    /// <summary>
    /// Checks if a token/grant has expired based on its expiration time.
    /// Includes a small buffer (5 seconds) to avoid race conditions.
    /// </summary>
    public static bool IsExpired(this DateTime expiresAt)
    {
        return DateTime.UtcNow.AddSeconds(5) >= expiresAt;
    }

    /// <summary>
    /// Checks if a token/grant is still valid (not yet expired).
    /// </summary>
    public static bool IsValid(this DateTime expiresAt)
    {
        return !expiresAt.IsExpired();
    }

    /// <summary>
    /// Calculates remaining lifetime in seconds until expiration.
    /// Returns 0 if already expired.
    /// </summary>
    public static long RemainingSeconds(this DateTime expiresAt)
    {
        var remaining = (long)(expiresAt - DateTime.UtcNow).TotalSeconds;
        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Adds a configured lifetime (in seconds) to the current time.
    /// Ensures consistent expiration time calculation across the system.
    /// </summary>
    public static DateTime AddLifetime(this DateTime baseTime, int lifetimeSeconds)
    {
        return baseTime.AddSeconds(Math.Max(0, lifetimeSeconds));
    }

    /// <summary>
    /// Formats a DateTime as an RFC 3339 string for OAuth2/OIDC responses.
    /// </summary>
    public static string ToRfc3339String(this DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
            dateTime = dateTime.ToUniversalTime();

        return dateTime.ToString("o");
    }
}
