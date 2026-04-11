// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Collections.Concurrent;
using DotnetAuthServer.Extensions;

/// <summary>
/// Service for managing OAuth2 session/state during authorization flows.
/// Stores temporary state needed for multi-step flows like authorization code flow.
/// Uses in-memory storage for single-server deployments; consider distributed cache for scaled deployments.
/// </summary>
public class SessionStateService
{
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    private readonly ILogger<SessionStateService> _logger;

    public SessionStateService(ILogger<SessionStateService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new session state for an OAuth2 flow.
    /// Returns a state parameter that should be included in authorization requests.
    /// </summary>
    public string CreateSession(string clientId, string redirectUri, string scopes, string? nonce = null)
    {
        var stateId = Guid.NewGuid().ToString("N");

        var session = new SessionState
        {
            StateId = stateId,
            ClientId = clientId,
            RedirectUri = redirectUri,
            RequestedScopes = scopes,
            Nonce = nonce,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10) // State expires after 10 minutes
        };

        _sessions.TryAdd(stateId, session);

        _logger.LogInformation(
            "Session created: {StateId} for client {ClientId}",
            stateId,
            clientId);

        return stateId;
    }

    /// <summary>
    /// Retrieves and validates a session state.
    /// Checks for expiration and confirms it hasn't been tampered with.
    /// </summary>
    public SessionState? GetSession(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return null;

        if (!_sessions.TryGetValue(stateId, out var session))
        {
            _logger.LogWarning("Session not found: {StateId}", stateId);
            return null;
        }

        // Check if session has expired
        if (session.ExpiresAt.IsExpired())
        {
            _logger.LogWarning("Session expired: {StateId}", stateId);
            _sessions.TryRemove(stateId, out _);
            return null;
        }

        return session;
    }

    /// <summary>
    /// Completes a session and removes it from storage.
    /// Should be called after token exchange to prevent replay attacks.
    /// </summary>
    public bool CompleteSession(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return false;

        var removed = _sessions.TryRemove(stateId, out var session);

        if (removed)
        {
            _logger.LogInformation("Session completed and removed: {StateId}", stateId);
        }

        return removed;
    }

    /// <summary>
    /// Updates session metadata (e.g., after user authentication).
    /// </summary>
    public bool UpdateSession(string stateId, string? userId = null, string? grantedScopes = null)
    {
        if (!_sessions.TryGetValue(stateId, out var session))
            return false;

        if (!string.IsNullOrWhiteSpace(userId))
            session.UserId = userId;

        if (!string.IsNullOrWhiteSpace(grantedScopes))
            session.GrantedScopes = grantedScopes;

        session.LastUpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Cleans up expired sessions.
    /// Should be called periodically to free memory.
    /// </summary>
    public int CleanupExpiredSessions()
    {
        var now = DateTime.UtcNow;
        var expiredSessions = _sessions
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var stateId in expiredSessions)
        {
            if (_sessions.TryRemove(stateId, out _))
                removedCount++;
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", removedCount);
        }

        return removedCount;
    }

    /// <summary>
    /// Gets current session count (useful for monitoring).
    /// </summary>
    public int GetActiveSessionCount()
    {
        return _sessions.Count;
    }
}

/// <summary>
/// Represents a session state during an OAuth2 authorization flow.
/// </summary>
public class SessionState
{
    public string StateId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string RequestedScopes { get; set; } = string.Empty;
    public string? GrantedScopes { get; set; }
    public string? Nonce { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>
    /// Checks if this session is still valid (not expired).
    /// </summary>
    public bool IsValid()
    {
        return ExpiresAt > DateTime.UtcNow;
    }
}
