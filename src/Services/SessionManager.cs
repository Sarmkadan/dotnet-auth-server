#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using Microsoft.Extensions.Options;

/// <summary>
/// Manages user sessions: creation, validation, activity tracking, and revocation.
/// Each session is bound to a user-client pair and tracks metadata for audit purposes.
/// </summary>
public sealed class SessionManager
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<SessionManager> _logger;
    private readonly AuthServerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SessionManager"/>.
    /// </summary>
    /// <param name="sessionRepository">Repository for session persistence.</param>
    /// <param name="options">Server configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public SessionManager(
        IUserSessionRepository sessionRepository,
        IOptions<AuthServerOptions> options,
        ILogger<SessionManager> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new session for an authenticated user.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier.</param>
    /// <param name="clientId">The OAuth2 client that initiated authentication.</param>
    /// <param name="scopes">Space-separated granted scopes.</param>
    /// <param name="ipAddress">Client IP address, if available.</param>
    /// <param name="userAgent">Client User-Agent header, if available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session entity.</returns>
    public async Task<UserSession> CreateSessionAsync(
        string userId,
        string clientId,
        string scopes,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = userId,
            ClientId = clientId,
            GrantedScopes = scopes,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_options.RefreshTokenLifetimeSeconds),
            LastActivityAt = DateTime.UtcNow
        };

        await _sessionRepository.CreateAsync(session, cancellationToken);
        _logger.LogInformation("Created session {SessionId} for user {UserId}, client {ClientId}",
            session.SessionId, userId, clientId);

        return session;
    }

    /// <summary>
    /// Validates that a session exists, is not revoked, and has not expired.
    /// Updates the last activity timestamp on success.
    /// </summary>
    /// <param name="sessionId">The session identifier to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The valid session entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is invalid, expired or revoked.</exception>
    public async Task<UserSession> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new InvalidOperationException($"Session '{sessionId}' not found.");
        }

        if (session.IsRevoked)
        {
            _logger.LogWarning("Attempt to use revoked session {SessionId}", sessionId);
            throw new InvalidOperationException("Session has been revoked.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogInformation("Session {SessionId} has expired", sessionId);
            throw new InvalidOperationException("Session has expired.");
        }

        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return session;
    }

    /// <summary>
    /// Revokes a specific session by its identifier.
    /// </summary>
    /// <param name="sessionId">The session to revoke.</param>
    /// <param name="reason">Human-readable reason for revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RevokeSessionAsync(string sessionId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Attempted to revoke non-existent session {SessionId}", sessionId);
            return;
        }

        session.IsRevoked = true;
        session.RevocationReason = reason;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Revoked session {SessionId} for user {UserId}: {Reason}",
            sessionId, session.UserId, reason);
    }

    /// <summary>
    /// Revokes all active sessions for a user, typically used on password change or account lockout.
    /// </summary>
    /// <param name="userId">The user whose sessions should be revoked.</param>
    /// <param name="reason">Human-readable reason for revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions revoked.</returns>
    public async Task<int> RevokeAllUserSessionsAsync(
        string userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);
        var count = 0;

        foreach (var session in sessions.Where(s => !s.IsRevoked))
        {
            session.IsRevoked = true;
            session.RevocationReason = reason;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            count++;
        }

        _logger.LogInformation("Revoked {Count} sessions for user {UserId}: {Reason}", count, userId, reason);
        return count;
    }
}
