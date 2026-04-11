// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when a user successfully authenticates during authorization flow.
/// Used for audit logging and tracking authentication patterns.
/// </summary>
public class UserAuthenticatedEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string EventType => "user_authenticated";

    /// <summary>
    /// User who was authenticated.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username used for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The client application for which authentication occurred.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address for security/anomaly detection.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Authentication method used (password, SAML, OIDC, etc.)
    /// </summary>
    public string AuthenticationMethod { get; set; } = "password";

    /// <summary>
    /// User's email address (if available).
    /// </summary>
    public string? Email { get; set; }
}
