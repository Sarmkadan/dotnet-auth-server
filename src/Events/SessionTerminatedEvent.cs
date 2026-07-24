#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when a user session is terminated, either through logout,
/// token expiration, or administrative action. Used for session management and
/// security monitoring.
/// </summary>
public sealed class SessionTerminatedEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string EventType => "session_terminated";

    /// <summary>
    /// The user whose session was terminated.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The client application associated with the session.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Reason for session termination (logout, token expiration, account lockout, etc.).
    /// </summary>
    public string Reason { get; set; } = "logout";

    /// <summary>
    /// Number of active sessions remaining for this user after termination.
    /// </summary>
    public int RemainingSessions { get; set; }

    /// <summary>
    /// Client IP address for audit/security purposes.
    /// </summary>
    public string? ClientIpAddress { get; set; }
}