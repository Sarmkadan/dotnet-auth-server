#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;

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
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be null or whitespace", nameof(clientId));

        if (string.IsNullOrWhiteSpace(grantedScopes))
            throw new ArgumentException("Granted scopes cannot be null or whitespace", nameof(grantedScopes));

        try
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
                session.SessionId,
                userId,
                clientId);

            return session;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error creating session for user {UserId} via client {ClientId}",
                userId,
                clientId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Session creation failed",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Returns all active sessions for the specified user.
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        try
        {
            return await _sessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving active sessions for user {UserId}", userId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Failed to retrieve active sessions",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Returns all sessions (including revoked and expired) for the specified user.
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetAllSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        try
        {
            return await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving all sessions for user {UserId}", userId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Failed to retrieve sessions",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Returns a global view of all currently active sessions across all users.
    /// Intended for admin dashboards only.
    /// </summary>
    public async Task<IEnumerable<UserSession>> GetAllActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _sessionRepository.GetAllActiveAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving all active sessions");
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Failed to retrieve active sessions",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Returns a list of active sessions for a user with device and activity information.
    /// </summary>
    /// <param name="userId">User ID to query sessions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions with device and activity metadata.</returns>
    public async Task<IEnumerable<UserSessionInfo>> GetActiveSessionsWithDetailsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        try
        {
            var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            return sessions.Select(s => new UserSessionInfo
            {
                SessionId = s.SessionId,
                UserId = s.UserId,
                ClientId = s.ClientId,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                ExpiresAt = s.ExpiresAt,
                IsRevoked = s.IsRevoked
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving active sessions with details for user {UserId}", userId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Failed to retrieve active sessions",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Revokes all sessions for a user except the specified session ID.
    /// Returns the number of sessions revoked.
    /// </summary>
    /// <param name="userId">User ID whose sessions to revoke.</param>
    /// <param name="keepSessionId">Session ID to keep active.</param>
    /// <param name="reason">Optional reason for revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sessions revoked.</returns>
    public async Task<int> RevokeAllOtherUserSessionsAsync(
        string userId,
        string keepSessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        if (string.IsNullOrWhiteSpace(keepSessionId))
            throw new ArgumentException("Session ID to keep cannot be null or whitespace", nameof(keepSessionId));

        try
        {
            var count = await _sessionRepository.RevokeAllOtherUserSessionsAsync(
                userId,
                keepSessionId,
                reason,
                cancellationToken);

            _logger.LogInformation(
                "Revoked {Count} other session(s) for user {UserId}. Kept session {KeepSessionId}. Reason: {Reason}",
                count,
                userId,
                keepSessionId,
                reason ?? "none");

            return count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error revoking all other sessions for user {UserId}", userId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Session revocation failed",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Revokes a single session by ID.
    /// </summary>
    /// <exception cref="AuthServerException">Thrown when the session is not found.</exception>
    public async Task RevokeSessionAsync(
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace", nameof(sessionId));

        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
                ?? throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    $"Session '{sessionId}' not found",
                    404);

            session.Revoke(reason);
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            _logger.LogInformation("Session {SessionId} revoked. Reason: {Reason}", sessionId, reason ?? "none");
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Session revocation failed",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Revokes all active sessions for a user. Returns the number of sessions revoked.
    /// </summary>
    public async Task<int> RevokeAllUserSessionsAsync(
        string userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or whitespace", nameof(userId));

        try
        {
            var count = await _sessionRepository.RevokeAllUserSessionsAsync(userId, reason, cancellationToken);

            _logger.LogInformation(
                "Revoked {Count} session(s) for user {UserId}. Reason: {Reason}",
                count,
                userId,
                reason ?? "none");

            return count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error revoking all sessions for user {UserId}", userId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Session revocation failed",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// DTO containing detailed information about a user session including device and activity metadata.
    /// </summary>
    public sealed class UserSessionInfo
    {
        /// <summary>Unique identifier for the session.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>User ID associated with this session.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>OAuth2 client that obtained the tokens.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Client IP address (optional).</summary>
        public string? IpAddress { get; set; }

        /// <summary>Client user-agent string (optional).</summary>
        public string? UserAgent { get; set; }

        /// <summary>When the session was created (UTC).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>When the session was last active (UTC).</summary>
        public DateTime? LastActivityAt { get; set; }

        /// <summary>When the session expires (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Whether the session has been explicitly revoked.</summary>
        public bool IsRevoked { get; set; }
    }

    /// <summary>
    /// Records activity on a session (extends the last-activity timestamp).
    /// </summary>
    public async Task TouchSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace", nameof(sessionId));

        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session is null || !session.IsActive())
                return;

            session.Touch();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error touching session {SessionId}", sessionId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Session touch failed",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Returns a summary of session statistics across all users.
    /// </summary>
    public async Task<SessionStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving session statistics");
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Failed to retrieve session statistics",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Removes sessions that have passed their expiry time.
    /// </summary>
    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var removed = await _sessionRepository.DeleteExpiredAsync(cancellationToken);
            if (removed > 0)
                _logger.LogInformation("Cleaned up {Count} expired session(s)", removed);
            return removed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Session cleanup failed",
                500,
                null,
                null,
                ex);
        }
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
