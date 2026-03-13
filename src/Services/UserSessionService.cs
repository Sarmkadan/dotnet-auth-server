#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Manages the lifecycle of authenticated user sessions.
/// Sessions are created on successful token issuance and can be revoked
/// individually or in bulk (e.g. on password change or account deletion).
/// </summary>
public sealed class UserSessionService
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<UserSessionService> _logger;
    private readonly AuthServerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="UserSessionService"/>.
    /// </summary>
    public UserSessionService(
        IUserSessionRepository sessionRepository,
        ILogger<UserSessionService> logger,
        AuthServerOptions options)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Creates and persists a new user session after a successful token grant.
    /// </summary>
    /// <param name="userId">Authenticated user's ID.</param>
    /// <param name="clientId">OAuth2 client that obtained the tokens.</param>
    /// <param name="grantedScopes">Space-delimited scopes granted in this session.</param>
    /// <param name="ipAddress">Client IP address (optional).</param>
    /// <param name="userAgent">Client user-agent string (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="UserSession"/>.</returns>
    public async Task<UserSession> CreateSessionAsync(
        string userId,
        string clientId,
        string grantedScopes,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var session = new UserSession
        {
            UserId = userId,
            ClientId = clientId,
            GrantedScopes = grantedScopes,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_options.RefreshTokenLifetimeSeconds),
            LastActivityAt = DateTime.UtcNow
        };

        await _sessionRepository.CreateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session {SessionId} created for user {UserId} via client {ClientId}",
            session.SessionId, userId, clientId);

        return session;
    }

    /// <summary>
    /// Returns all active sessions for the specified user.
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(
        string userId, CancellationToken cancellationToken = default)
        => await _sessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);

    /// <summary>
    /// Returns all sessions (including revoked and expired) for the specified user.
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetAllSessionsAsync(
        string userId, CancellationToken cancellationToken = default)
        => await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);

    /// <summary>
    /// Returns a global view of all currently active sessions across all users.
    /// Intended for admin dashboards only.
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetAllActiveSessionsAsync(
        CancellationToken cancellationToken = default)
        => await _sessionRepository.GetAllActiveAsync(cancellationToken);

    /// <summary>
    /// Revokes a single session by ID.
    /// </summary>
    /// <exception cref="AuthServerException">Thrown when the session is not found.</exception>
    public async Task RevokeSessionAsync(
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"Session '{sessionId}' not found", 404);

        session.Revoke(reason);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Session {SessionId} revoked. Reason: {Reason}", sessionId, reason ?? "none");
    }

    /// <summary>
    /// Revokes all active sessions for a user. Returns the number of sessions revoked.
    /// </summary>
    public async Task<int> RevokeAllUserSessionsAsync(
        string userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var count = await _sessionRepository.RevokeAllUserSessionsAsync(userId, reason, cancellationToken);

        _logger.LogInformation(
            "Revoked {Count} session(s) for user {UserId}. Reason: {Reason}",
            count, userId, reason ?? "none");

        return count;
    }

    /// <summary>
    /// Records activity on a session (extends the last-activity timestamp).
    /// </summary>
    public async Task TouchSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || !session.IsActive()) return;

        session.Touch();
        await _sessionRepository.UpdateAsync(session, cancellationToken);
    }

    /// <summary>
    /// Returns a summary of session statistics across all users.
    /// </summary>
    public async Task<SessionStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _sessionRepository.GetAllAsync(cancellationToken);
        var list = all.ToList();

        return new SessionStats
        {
            TotalSessions = list.Count,
            ActiveSessions = list.Count(s => s.IsActive()),
            RevokedSessions = list.Count(s => s.IsRevoked),
            ExpiredSessions = list.Count(s => !s.IsRevoked && s.ExpiresAt <= DateTime.UtcNow),
            UniqueUsers = list.Select(s => s.UserId).Distinct(StringComparer.OrdinalIgnoreCase).Count()
        };
    }

    /// <summary>
    /// Removes sessions that have passed their expiry time.
    /// </summary>
    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var removed = await _sessionRepository.DeleteExpiredAsync(cancellationToken);
        if (removed > 0)
            _logger.LogInformation("Cleaned up {Count} expired session(s)", removed);
        return removed;
    }
}

/// <summary>
/// Aggregate statistics about the current session store.
/// </summary>
public sealed class SessionStats
{
    /// <summary>Total number of session records (all states).</summary>
    public int TotalSessions { get; set; }

    /// <summary>Sessions that are neither revoked nor expired.</summary>
    public int ActiveSessions { get; set; }

    /// <summary>Sessions that were explicitly revoked.</summary>
    public int RevokedSessions { get; set; }

    /// <summary>Sessions that expired without being explicitly revoked.</summary>
    public int ExpiredSessions { get; set; }

    /// <summary>Number of distinct user IDs with any session (active or otherwise).</summary>
    public int UniqueUsers { get; set; }

    /// <summary>Timestamp when these statistics were computed (UTC).</summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
