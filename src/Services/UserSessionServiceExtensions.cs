#nullable enable

namespace DotnetAuthServer.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Extension methods for <see cref="UserSessionService"/> that provide additional convenience and functionality
/// for working with user sessions.
/// </summary>
public static class UserSessionServiceExtensions
{
    /// <summary>
    /// Gets all active sessions for a user and returns them as a dictionary keyed by session ID.
    /// Useful for scenarios where you need to quickly look up sessions by their ID.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="userId">The user ID to retrieve sessions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping session IDs to <see cref="UserSession"/> objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is null or whitespace.</exception>
    public static async Task<Dictionary<string, UserSession>> GetActiveSessionsDictionaryAsync(
        this UserSessionService service,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        ArgumentException.ThrowIfNullOrEmpty(userId);

        var sessions = await service.GetActiveSessionsAsync(userId, cancellationToken);
        return sessions.ToDictionary(s => s.SessionId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all sessions (including revoked and expired) for a user and returns them as a dictionary keyed by session ID.
    /// Useful for scenarios where you need to quickly look up any session by its ID.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="userId">The user ID to retrieve sessions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping session IDs to <see cref="UserSession"/> objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is null or whitespace.</exception>
    public static async Task<Dictionary<string, UserSession>> GetAllSessionsDictionaryAsync(
        this UserSessionService service,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        ArgumentException.ThrowIfNullOrEmpty(userId);

        var sessions = await service.GetAllSessionsAsync(userId, cancellationToken);
        return sessions.ToDictionary(s => s.SessionId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a user has any active sessions.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has at least one active session; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is null or whitespace.</exception>
    public static async Task<bool> HasActiveSessionsAsync(
        this UserSessionService service,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        ArgumentException.ThrowIfNullOrEmpty(userId);

        var activeSessions = await service.GetActiveSessionsAsync(userId, cancellationToken);
        return activeSessions.Any();
    }

    /// <summary>
    /// Gets the total number of active sessions across all users.
    /// Useful for monitoring and dashboard purposes.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active sessions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    public static async Task<int> GetTotalActiveSessionsCountAsync(
        this UserSessionService service,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        var activeSessions = await service.GetAllActiveSessionsAsync(cancellationToken);
        return activeSessions.Count();
    }

    /// <summary>
    /// Gets all sessions for a specific client ID.
    /// Useful for auditing which users are using specific OAuth clients.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="clientId">The client ID to filter sessions by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enumerable of sessions matching the client ID.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="clientId"/> is null or whitespace.</exception>
    public static async Task<IEnumerable<UserSession>> GetSessionsByClientIdAsync(
        this UserSessionService service,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        ArgumentException.ThrowIfNullOrEmpty(clientId);

        var allSessions = await service.GetAllActiveSessionsAsync(cancellationToken);
        return allSessions.Where(s => string.Equals(s.ClientId, clientId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all active sessions for a specific client ID.
    /// Useful for monitoring currently active sessions per client.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="clientId">The client ID to filter sessions by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enumerable of active sessions matching the client ID.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="clientId"/> is null or whitespace.</exception>
    public static async Task<IEnumerable<UserSession>> GetActiveSessionsByClientIdAsync(
        this UserSessionService service,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        ArgumentException.ThrowIfNullOrEmpty(clientId);

        var activeSessions = await service.GetAllActiveSessionsAsync(cancellationToken);
        return activeSessions
            .Where(s => string.Equals(s.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.IsActive());
    }

    /// <summary>
    /// Revokes all sessions for multiple users in a single operation.
    /// Useful for bulk user management scenarios like account deletion or password changes.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="userIds">Collection of user IDs whose sessions should be revoked.</param>
    /// <param name="reason">Optional reason for revoking the sessions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of sessions revoked across all users.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="userIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="userIds"/> contains null or whitespace entries.</exception>
    public static async Task<int> RevokeAllSessionsForUsersAsync(
        this UserSessionService service,
        IEnumerable<string> userIds,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(userIds);

        var userIdList = userIds.ToList();
        if (userIdList.Count == 0)
            return 0;

        if (userIdList.Any(static id => string.IsNullOrWhiteSpace(id)))
            throw new ArgumentException("User IDs cannot be null or whitespace", nameof(userIds));

        var totalRevoked = 0;
        foreach (var userId in userIdList)
        {
            var revokedCount = await service.RevokeAllUserSessionsAsync(userId, reason, cancellationToken);
            totalRevoked += revokedCount;
        }

        return totalRevoked;
    }

    /// <summary>
    /// Gets all sessions that are about to expire within the specified time window.
    /// Useful for proactive session management and renewal reminders.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="expiryThreshold">The time window before expiry to consider sessions as expiring soon.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enumerable of sessions that will expire within the threshold.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    public static async Task<IEnumerable<UserSession>> GetExpiringSessionsAsync(
        this UserSessionService service,
        TimeSpan expiryThreshold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        var thresholdDate = DateTime.UtcNow.Add(expiryThreshold);
        var allSessions = await service.GetAllActiveSessionsAsync(cancellationToken);

        return allSessions
            .Where(s => s.ExpiresAt <= thresholdDate)
            .Where(s => s.IsActive());
    }

    /// <summary>
    /// Gets session statistics with computed derived metrics.
    /// Provides additional convenience metrics beyond the basic session stats.
    /// </summary>
    /// <param name="service">The session service instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the basic stats and additional derived metrics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    public static async Task<(SessionStats Stats, int ExpiredSoonCount, double ActiveSessionRatio)> GetEnhancedStatsAsync(
        this UserSessionService service,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        var stats = await service.GetStatsAsync(cancellationToken);

        // Calculate sessions expiring soon (within 24 hours)
        var expiringSoonCount = stats.TotalSessions > 0
            ? (await service.GetAllActiveSessionsAsync(cancellationToken))
                .Count(s => s.ExpiresAt <= DateTime.UtcNow.AddHours(24))
            : 0;

        // Calculate active session ratio
        var activeSessionRatio = stats.TotalSessions > 0
            ? (double)stats.ActiveSessions / stats.TotalSessions
            : 0.0;

        stats.ComputedAt = DateTime.UtcNow;
        return (stats, expiringSoonCount, activeSessionRatio);
    }
}