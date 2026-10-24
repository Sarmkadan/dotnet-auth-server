#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when authentication fails due to invalid credentials, locked account,
/// or other authentication-related issues. Feeds rate limiters and SIEM systems.
/// </summary>
public sealed class AuthenticationFailedEvent : IDomainEvent
{
    /// <summary>
    /// Unique identifier of this event.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// UTC timestamp at which the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier of the HTTP request that produced the event, if available.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Machine-readable event type name.
    /// </summary>
    public string EventType => "authentication_failed";

    /// <summary>
    /// The user who attempted authentication.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Username used for authentication attempt.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The client application for which authentication was attempted.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client IP address for security/anomaly detection and rate limiting.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Authentication method used (password, SAML, OIDC, etc.).
    /// </summary>
    public string AuthenticationMethod { get; set; } = "password";

    /// <summary>
    /// Failure reason code for programmatic handling.
    /// </summary>
    public string FailureReason { get; set; } = "invalid_credentials";

    /// <summary>
    /// Detailed error message for logging purposes.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}