#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;

/// <summary>
/// REST API for the session management dashboard.
/// Exposes endpoints for listing active sessions, revoking individual or all sessions
/// for a user, and viewing aggregate statistics. All routes are under <c>/api/sessions</c>.
/// </summary>
[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
public sealed class SessionManagementController : ControllerBase
{
    private readonly UserSessionService _sessionService;
    private readonly ILogger<SessionManagementController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SessionManagementController"/>.
    /// </summary>
    public SessionManagementController(
        UserSessionService sessionService,
        ILogger<SessionManagementController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Global session views
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all currently active sessions across all users.
    /// Intended for admin dashboards and security monitoring.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SessionSummary>>), 200)]
    public async Task<IActionResult> GetAllActiveSessionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _sessionService.GetAllActiveSessionsAsync(cancellationToken);
            var summaries = sessions.Select(MapToSummary);
            return Ok(ApiResponse<IEnumerable<SessionSummary>>.SuccessResponse(summaries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing active sessions");
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    /// <summary>
    /// Returns aggregate session statistics (total, active, revoked, expired, unique users).
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<SessionStats>), 200)]
    public async Task<IActionResult> GetStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _sessionService.GetStatsAsync(cancellationToken);
            return Ok(ApiResponse<SessionStats>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session stats");
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Per-user session views
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all active sessions for the specified user.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SessionSummary>>), 200)]
    public async Task<IActionResult> GetUserSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _sessionService.GetActiveSessionsAsync(userId, cancellationToken);
            var summaries = sessions.Select(MapToSummary);
            return Ok(ApiResponse<IEnumerable<SessionSummary>>.SuccessResponse(summaries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing sessions for user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Revocation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Revokes a single session by its ID.
    /// </summary>
    /// <param name="sessionId">The session identifier to revoke.</param>
    /// <param name="reason">Optional human-readable reason for the revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RevokeSessionAsync(
        string sessionId,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sessionService.RevokeSessionAsync(sessionId, reason, cancellationToken);
            return Ok(ApiResponse.SuccessResponse($"Session '{sessionId}' revoked"));
        }
        catch (AuthServerException ex) when (ex.StatusCode == 404)
        {
            return NotFound(ApiResponse.ErrorResponse("session_not_found", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session {SessionId}", sessionId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    /// <summary>
    /// Revokes all active sessions for the specified user.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="reason">Optional human-readable reason for the revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> RevokeAllUserSessionsAsync(
        string userId,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await _sessionService.RevokeAllUserSessionsAsync(userId, reason, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(
                new { revokedCount = count },
                $"Revoked {count} session(s) for user '{userId}'"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all sessions for user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    /// <summary>
    /// Removes all expired sessions from storage to free memory.
    /// </summary>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        try
        {
            var count = await _sessionService.CleanupExpiredSessionsAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(
                new { removedCount = count },
                $"Removed {count} expired session(s)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SessionSummary MapToSummary(Domain.Entities.UserSession s) => new SessionSummary
    {
        SessionId = s.SessionId,
        UserId = s.UserId,
        ClientId = s.ClientId,
        IpAddress = s.IpAddress,
        UserAgent = s.UserAgent,
        GrantedScopes = s.GrantedScopes,
        CreatedAt = s.CreatedAt,
        ExpiresAt = s.ExpiresAt,
        LastActivityAt = s.LastActivityAt,
        IsRevoked = s.IsRevoked,
        RevocationReason = s.RevocationReason
    };
}

/// <summary>
/// Read-only projection of a <see cref="Domain.Entities.UserSession"/> for API responses.
/// </summary>
public sealed class SessionSummary
{
    /// <summary>Unique session identifier.</summary>
    public string SessionId { get; set; } = null!;

    /// <summary>Subject that owns this session.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>OAuth2 client that opened the session.</summary>
    public string ClientId { get; set; } = null!;

    /// <summary>Client IP address at session creation time.</summary>
    public string? IpAddress { get; set; }

    /// <summary>User-Agent header at session creation time.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Space-separated list of granted scopes.</summary>
    public string GrantedScopes { get; set; } = null!;

    /// <summary>Session creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Session expiry timestamp (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Last activity timestamp (UTC).</summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>Whether this session has been revoked.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Reason for revocation, if set.</summary>
    public string? RevocationReason { get; set; }
}
