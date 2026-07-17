#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using System.Globalization;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Extension methods for <see cref="SessionManagementTests"/> that provide additional test utilities
/// for session management scenarios.
/// </summary>
public static class SessionManagementTestsExtensions
{
    /// <summary>
    /// Creates a test session with the specified parameters and returns it.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="grantedScopes">The granted scopes as space-separated string.</param>
    /// <param name="ipAddress">The IP address for the session.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>The created <see cref="UserSession"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="userId"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="clientId"/> is null or empty.</exception>
    public static UserSession CreateTestSession(
        this SessionManagementTests test,
        string userId,
        string clientId,
        string grantedScopes = "openid profile email",
        string? ipAddress = null,
        string? userAgent = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentException.ThrowIfNullOrEmpty(clientId);

        var session = new UserSession
        {
            UserId = userId,
            ClientId = clientId,
            GrantedScopes = grantedScopes ?? "openid profile email",
            IpAddress = ipAddress ?? "127.0.0.1",
            UserAgent = userAgent ?? "TestAgent/1.0",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        return session;
    }

    /// <summary>
    /// Creates multiple test sessions for the same user.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="count">Number of sessions to create.</param>
    /// <param name="clientIdPrefix">Prefix for client identifiers (e.g., "client" becomes "client1", "client2").</param>
    /// <returns>Read-only list of created sessions.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="userId"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 1.</exception>
    public static IReadOnlyList<UserSession> CreateMultipleSessions(
        this SessionManagementTests test,
        string userId,
        int count,
        string clientIdPrefix = "client")
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var sessions = new List<UserSession>(count);
        for (var i = 0; i < count; i++)
        {
            sessions.Add(test.CreateTestSession(
                userId,
                $"{clientIdPrefix}{i + 1}",
                "openid profile",
                "192.168.1." + (i + 1),
                $"TestAgent/{i + 1}.0"
            ));
        }

        return sessions.AsReadOnly();
    }

    /// <summary>
    /// Verifies that a session collection contains exactly one active session for the specified user.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="sessions">The session collection to verify.</param>
    /// <param name="userId">The expected user identifier.</param>
    /// <returns>True if verification succeeds; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessions"/> or <paramref name="userId"/> is null.</exception>
    public static bool ShouldContainSingleActiveSessionForUser(
        this SessionManagementTests test,
        IEnumerable<UserSession> sessions,
        string userId)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(userId);

        var activeSessions = sessions.Where(s => s.IsActive()).ToList();
        return activeSessions.Count == 1
            && activeSessions[0].UserId.Equals(userId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the session with the specified session ID from the collection.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="sessions">The session collection.</param>
    /// <param name="sessionId">The session identifier to find.</param>
    /// <returns>The found session, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sessions"/> or <paramref name="sessionId"/> is null.</exception>
    public static UserSession? GetSessionById(
        this SessionManagementTests test,
        IEnumerable<UserSession> sessions,
        string sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(sessionId);

        return sessions.FirstOrDefault(s => s.SessionId.Equals(sessionId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Calculates the total duration in seconds for a session based on its creation and expiration times.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="session">The session to analyze.</param>
    /// <returns>The duration in seconds as a double, or 0 if session is null.</returns>
    public static double GetSessionDurationSeconds(
        this SessionManagementTests test,
        UserSession? session)
    {
        return session is null
            ? 0
            : (session.ExpiresAt - session.CreatedAt).TotalSeconds;
    }

    /// <summary>
    /// Creates a session that is about to expire (within the next minute).
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>A session that will expire shortly.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="userId"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="clientId"/> is null or empty.</exception>
    public static UserSession CreateExpiringSession(
        this SessionManagementTests test,
        string userId,
        string clientId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentException.ThrowIfNullOrEmpty(clientId);

        var session = new UserSession
        {
            UserId = userId,
            ClientId = clientId,
            GrantedScopes = "openid",
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent/1.0",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(30) // Expires in 30 seconds
        };

        return session;
    }

    /// <summary>
    /// Creates a session with custom expiration time specified in seconds.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="expirationSeconds">Expiration time in seconds from now.</param>
    /// <returns>A session with custom expiration.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="userId"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="expirationSeconds"/> is not positive.</exception>
    public static UserSession CreateSessionWithExpiration(
        this SessionManagementTests test,
        string userId,
        string clientId,
        int expirationSeconds)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expirationSeconds, 0);

        var session = new UserSession
        {
            UserId = userId,
            ClientId = clientId,
            GrantedScopes = "openid profile",
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent/1.0",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expirationSeconds)
        };

        return session;
    }
}